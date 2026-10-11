// <copyright file="ShopPriceChatCommandPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// A chat command which lists the personal store items and sets their prices.
/// It's the input path for vanilla clients which can't send the extended price packet.
/// Slots are the store positions (1-32) the player sees in the shop window;
/// absolute slots (204-235) are accepted as well.
/// </summary>
[Guid("28B85B2D-AF73-4F5E-B9FF-E67141EF7941")]
[PlugIn]
[Display(Name = nameof(PlugInResources.ShopPriceChatCommandPlugIn_Name), Description = nameof(PlugInResources.ShopPriceChatCommandPlugIn_Description), ResourceType = typeof(PlugInResources))]
[ChatCommandHelp(Command, typeof(Arguments), MinimumStatus)]
public class ShopPriceChatCommandPlugIn : ChatCommandPlugInBase<ShopPriceChatCommandPlugIn.Arguments>
{
    private const string Command = "/shopprice";
    private const CharacterStatus MinimumStatus = CharacterStatus.Normal;

    private readonly SetItemPriceAction _setPriceAction = new();

    /// <inheritdoc />
    public override string Key => Command;

    /// <inheritdoc />
    public override CharacterStatus MinCharacterStatusRequirement => MinimumStatus;

    /// <inheritdoc />
    protected override async ValueTask DoHandleCommandAsync(Player player, Arguments arguments)
    {
        if (arguments is null || arguments.Slot < 0)
        {
            await this.ListStoreItemsAsync(player).ConfigureAwait(false);
            return;
        }

        if (arguments.Amount < 0
            || !TryResolveSlot(arguments.Slot, out var storeSlot))
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.InvalidShopPriceArguments)).ConfigureAwait(false);
            return;
        }

        if (!TryParseCurrency(arguments.Currency, out var currency))
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.UnknownShopCurrency), arguments.Currency, ShopCurrencies.CanonicalNames).ConfigureAwait(false);
            return;
        }

        await this._setPriceAction.SetPriceAsync(player, storeSlot, new ShopPrice(currency, arguments.Amount)).ConfigureAwait(false);
    }

    private static bool TryParseCurrency(string? text, out PlayerShopCurrency currency)
    {
        currency = PlayerShopCurrency.Zen;
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }

        return ShopCurrencies.TryParseAlias(text.Trim().ToLowerInvariant(), out currency);
    }

    private static bool TryResolveSlot(int slot, out byte storeSlot)
    {
        if (slot >= 1 && slot <= InventoryConstants.StoreSize)
        {
            storeSlot = (byte)(InventoryConstants.FirstStoreItemSlotIndex + slot - 1);
            return true;
        }

        if (slot >= InventoryConstants.FirstStoreItemSlotIndex
            && slot < InventoryConstants.FirstStoreItemSlotIndex + InventoryConstants.StoreSize)
        {
            storeSlot = (byte)slot;
            return true;
        }

        storeSlot = 0;
        return false;
    }

    private async ValueTask ListStoreItemsAsync(Player player)
    {
        var items = player.SelectedCharacter?.Inventory?.Items
            .Where(i => i.ItemSlot >= InventoryConstants.FirstStoreItemSlotIndex
                && i.ItemSlot < InventoryConstants.FirstStoreItemSlotIndex + InventoryConstants.StoreSize)
            .OrderBy(i => i.ItemSlot)
            .ToList();
        if (items is null || items.Count == 0)
        {
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.InvalidShopPriceArguments)).ConfigureAwait(false);
            return;
        }

        foreach (var item in items)
        {
            var number = item.ItemSlot - InventoryConstants.FirstStoreItemSlotIndex + 1;
            var priceText = item.StorePrice is { } price
                ? $"{price} {ShopCurrencies.CanonicalNameOf(item.StorePriceCurrency)}"
                : player.GetLocalizedMessage(nameof(PlayerMessage.ShopPriceNoPrice));
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.ShopPriceListEntry), number, item, priceText).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Arguments for the shop price chat command.
    /// Missing values keep their negative defaults, so the command can tell
    /// omitted arguments apart without nullable types (which the argument
    /// parser cannot convert).
    /// </summary>
    public class Arguments : ArgumentsBase
    {
        /// <summary>
        /// Gets or sets the store position (1-32) or absolute slot of the item.
        /// Negative lists the store items.
        /// </summary>
        public int Slot { get; set; } = -1;

        /// <summary>
        /// Gets or sets the price amount. 0 removes the price.
        /// </summary>
        public int Amount { get; set; } = -1;

        /// <summary>
        /// Gets or sets the currency name. Empty means Zen.
        /// </summary>
        public string? Currency { get; set; }
    }
}
