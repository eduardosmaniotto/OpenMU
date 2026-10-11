// <copyright file="AccountCoinShopCurrencyExchange.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.CashShop;
using MUnique.OpenMU.GameLogic.Views.CashShop;

/// <summary>
/// Moves WCoin (C), WCoin (P) and Goblin Points payments between accounts.
/// Balances live on the <see cref="Account"/>, so no inventory slots are touched.
/// Only the buyer's progress is saved immediately (his own persistence lock, like
/// <c>CashShopActions.PurchaseAsync</c> does); the seller's balance persists with his
/// next regular save, exactly like Zen money does. The seller's lock is never acquired
/// here, see the lock-order invariant on <c>PlayerPersistence</c>.
/// </summary>
public class AccountCoinShopCurrencyExchange : IShopCurrencyExchange
{
    private static readonly IReadOnlyList<Item> NoRows = Array.Empty<Item>();

    private readonly CashShopCoinType _coinType;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountCoinShopCurrencyExchange"/> class.
    /// </summary>
    /// <param name="currency">The shop currency.</param>
    public AccountCoinShopCurrencyExchange(PlayerShopCurrency currency)
    {
        this.Currency = currency;
        this._coinType = currency switch
        {
            PlayerShopCurrency.WCoinC => CashShopCoinType.WCoinC,
            PlayerShopCurrency.WCoinP => CashShopCoinType.WCoinP,
            PlayerShopCurrency.GoblinPoints => CashShopCoinType.GoblinPoints,
            _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, "Not an account coin currency."),
        };
    }

    /// <inheritdoc />
    public PlayerShopCurrency Currency { get; }

    /// <inheritdoc />
    public bool CanCover(Player player, int amount) => this.GetBalance(player) >= amount;

    /// <inheritdoc />
    public async ValueTask<(ShopPaymentResult Result, IReadOnlyList<Item> MovedRows)> TryTransferAsync(Player buyer, Player seller, int amount)
    {
        if (buyer.Account is not { } buyerAccount || seller.Account is not { } sellerAccount)
        {
            return (ShopPaymentResult.InsufficientFunds, NoRows);
        }

        await buyer.ApplyPendingCashShopCoinGrantsAsync().ConfigureAwait(false);
        var buyerBalance = buyerAccount.GetCashShopCoins(this._coinType);
        var sellerBalance = sellerAccount.GetCashShopCoins(this._coinType);
        if (amount <= 0 || buyerBalance < amount)
        {
            return (ShopPaymentResult.InsufficientFunds, NoRows);
        }

        if ((long)sellerBalance + amount > int.MaxValue)
        {
            return (ShopPaymentResult.ReceiverCannotHold, NoRows);
        }

        buyerAccount.SetCashShopCoins(this._coinType, buyerBalance - amount);
        sellerAccount.SetCashShopCoins(this._coinType, sellerBalance + amount);
        if (!await this.TrySaveAsync(buyer).ConfigureAwait(false))
        {
            buyerAccount.SetCashShopCoins(this._coinType, buyerBalance);
            sellerAccount.SetCashShopCoins(this._coinType, sellerBalance);
            return (ShopPaymentResult.SaveFailed, NoRows);
        }

        return (ShopPaymentResult.Success, NoRows);
    }

    /// <inheritdoc />
    public async ValueTask NotifyBalanceChangedAsync(Player player)
    {
        if (player.Account is { } account)
        {
            await player.InvokeViewPlugInAsync<ICashShopViewPlugIn>(p => p.ShowPointsAsync(account)).ConfigureAwait(false);
        }
    }

    private int GetBalance(Player player)
    {
        if (player.Account is not { } account)
        {
            return 0;
        }

        return account.GetCashShopCoins(this._coinType);
    }

    private async ValueTask<bool> TrySaveAsync(Player buyer)
    {
        try
        {
            return await buyer.SaveProgressAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            buyer.Logger.LogError(ex, "Couldn't save the {coin} shop payment of player {player}.", this._coinType, buyer);
            return false;
        }
    }
}
