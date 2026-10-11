// <copyright file="JewelShopCurrencyExchange.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>
/// Moves jewel payments (Bless, Soul, Chaos, Creation, Life) from buyer to seller
/// by moving the existing item rows, like a trade does. Nothing is created or deleted.
/// Single jewels count one each. Packed bundles (e.g. from Lahap) count their piece
/// count, but only whole rows are ever consumed: a bundle pays only when the purchase
/// consumes it entirely, otherwise the buyer must unstack it first. A consumed bundle
/// stays a bundle at the seller; the seller needs one free inventory slot per moved row.
/// Only plain jewels qualify: tradable, level zero singles without options, and bundles
/// known to the jewel mixes. Anything else is skipped, fail-closed.
/// The transfer is credit-first under the seller's store lock: the seller is credited
/// before the buyer is debited, so a failed credit leaves the buyer untouched.
/// </summary>
public class JewelShopCurrencyExchange : IShopCurrencyExchange
{
    /// <summary>
    /// The number of bits of the bundle selection mask. Bundles beyond this
    /// are ignored, fail-closed to singles only.
    /// </summary>
    private const int MaxBundleBits = 32;

    /// <summary>
    /// Initializes a new instance of the <see cref="JewelShopCurrencyExchange"/> class.
    /// </summary>
    /// <param name="currency">The jewel currency.</param>
    public JewelShopCurrencyExchange(PlayerShopCurrency currency)
    {
        this.Currency = currency;
        this.Jewel = ShopCurrencies.TryGet(currency, out var descriptor) && descriptor is { JewelIdentifier: { } identifier }
            ? identifier
            : throw new ArgumentOutOfRangeException(nameof(currency), currency, "Not a jewel currency.");
    }

    /// <inheritdoc />
    public PlayerShopCurrency Currency { get; }

    /// <summary>
    /// Gets the jewel identifier (number, group).
    /// </summary>
    public ItemIdentifier Jewel { get; }

    /// <inheritdoc />
    public bool CanCover(Player player, int amount)
    {
        return this.SelectPayment(player, amount) is not null;
    }

    /// <inheritdoc />
    public async ValueTask<(ShopPaymentResult Result, IReadOnlyList<Item> MovedRows)> TryTransferAsync(Player buyer, Player seller, int amount)
    {
        if (buyer.Inventory is null || seller.Inventory is null)
        {
            return (ShopPaymentResult.InsufficientFunds, Array.Empty<Item>());
        }

        if (this.SelectPayment(buyer, amount) is not { } selection)
        {
            return (ShopPaymentResult.InsufficientFunds, Array.Empty<Item>());
        }

        if (seller.Inventory.FreeSlots.Take(selection.Count).Count() < selection.Count)
        {
            return (ShopPaymentResult.ReceiverCannotHold, Array.Empty<Item>());
        }

        var credited = new List<Item>(selection.Count);
        var buyerSlots = selection.Select(row => row.ItemSlot).ToList();
        foreach (var row in selection)
        {
            if (!await seller.Inventory.AddItemAsync(row).ConfigureAwait(false))
            {
                await this.RollbackCreditAsync(seller, credited).ConfigureAwait(false);
                return (ShopPaymentResult.ReceiverCannotHold, Array.Empty<Item>());
            }

            await seller.InvokeViewPlugInAsync<IItemAppearPlugIn>(p => p.ItemAppearAsync(row)).ConfigureAwait(false);
            credited.Add(row);
        }

        for (var i = 0; i < selection.Count; i++)
        {
            await buyer.Inventory.RemoveItemAsync(selection[i]).ConfigureAwait(false);
            var buyerSlot = buyerSlots[i];
            await buyer.InvokeViewPlugInAsync<IItemRemovedPlugIn>(p => p.RemoveItemAsync(buyerSlot)).ConfigureAwait(false);
        }

        return (ShopPaymentResult.Success, selection);
    }

    /// <inheritdoc />
    public ValueTask NotifyBalanceChangedAsync(Player player)
    {
        // Jewel payments notify per row during the transfer; there is no balance display to refresh.
        return ValueTask.CompletedTask;
    }

    private bool IsPayableSingle(Item row)
    {
        return row is { Level: 0, Definition: { } definition }
            && definition.Number == this.Jewel.Number
            && definition.Group == this.Jewel.Group
            && definition.IsTradable
            && !row.ItemOptions.Any();
    }

    private bool TryGetBundle(Player player, Item row, out (Item Row, int Pieces) bundle)
    {
        bundle = default;
        if (row.Definition is not { } definition || row.ItemOptions.Any() || !definition.IsTradable)
        {
            return false;
        }

        var mix = player.GameContext.Configuration.JewelMixes?.FirstOrDefault(m =>
            m.MixedJewel is { } mixed && mixed.Group == definition.Group && mixed.Number == definition.Number
            && m.SingleJewel is { } single && single.Group == this.Jewel.Group && single.Number == this.Jewel.Number);
        if (mix is null)
        {
            return false;
        }

        if (!JewelBundleHelper.TryGetPieceCount(row.Level, out var pieces))
        {
            return false;
        }

        bundle = (row, pieces);
        return true;
    }

    private IReadOnlyList<Item>? SelectPayment(Player buyer, int amount)
    {
        if (amount <= 0 || buyer.Inventory is null)
        {
            return null;
        }

        var singles = new List<Item>();
        var bundles = new List<(Item Row, int Pieces)>();
        foreach (var item in buyer.Inventory.Items)
        {
            if (this.IsPayableSingle(item))
            {
                singles.Add(item);
            }
            else if (this.TryGetBundle(buyer, item, out var bundle))
            {
                bundles.Add(bundle);
            }
        }

        // The empty bundle subset is covered by mask 0, so sufficient singles keep the previous behavior.
        if (!this.FindCover(bundles, singles.Count, amount, out var bundleMask, out var singlesNeeded))
        {
            return null;
        }

        var rows = new List<Item>(singlesNeeded + bundles.Count);
        for (var i = 0; i < bundles.Count && i < MaxBundleBits; i++)
        {
            if ((bundleMask & (1 << i)) != 0)
            {
                rows.Add(bundles[i].Row);
            }
        }

        rows.AddRange(singles.Take(singlesNeeded));
        return rows;
    }

    private bool FindCover(
        List<(Item Row, int Pieces)> bundles,
        int singleCount,
        int amount,
        out int bundleMask,
        out int singlesNeeded)
    {
        bundleMask = 0;
        singlesNeeded = 0;
        var reachable = new Dictionary<int, int> { [0] = 0 };
        for (var i = 0; i < bundles.Count && i < MaxBundleBits; i++)
        {
            foreach (var sum in reachable.Keys.ToList())
            {
                reachable.TryAdd(sum + bundles[i].Pieces, reachable[sum] | (1 << i));
            }
        }

        // Sums are tried ascending, so the empty subset (singles only) wins whenever
        // it covers the amount; bundles are spent only as needed, biggest shortfall first.
        foreach (var sum in reachable.Keys.OrderBy(s => s))
        {
            if (sum <= amount && amount - sum <= singleCount)
            {
                bundleMask = reachable[sum];
                singlesNeeded = amount - sum;
                return true;
            }
        }

        return false;
    }

    private async ValueTask RollbackCreditAsync(Player seller, List<Item> credited)
    {
        foreach (var paid in credited)
        {
            await seller.Inventory!.RemoveItemAsync(paid).ConfigureAwait(false);
            await seller.InvokeViewPlugInAsync<IItemRemovedPlugIn>(p => p.RemoveItemAsync(paid.ItemSlot)).ConfigureAwait(false);
        }

        if (credited.Count > 0)
        {
            seller.Logger.LogWarning("Rolled back {count} credited jewel payment rows of player {player} after a failed credit.", credited.Count, seller);
        }
    }
}
