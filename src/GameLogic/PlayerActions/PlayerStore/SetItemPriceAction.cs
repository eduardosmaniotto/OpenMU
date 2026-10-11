// <copyright file="SetItemPriceAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.GameLogic.Views.PlayerShop;

/// <summary>
/// Action to set the price of an item of the player store.
/// </summary>
public class SetItemPriceAction
{
    /// <summary>
    /// Sets the price of an item in Zen.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="slot">The absolute inventory slot of the item; store items live in slots 204 to 235.</param>
    /// <param name="price">The price in Zen.</param>
    public async ValueTask SetPriceAsync(Player player, byte slot, int price)
    {
        await this.SetPriceAsync(player, slot, new ShopPrice(PlayerShopCurrency.Zen, price)).ConfigureAwait(false);
    }

    /// <summary>
    /// Sets the price of an item in the given currency.
    /// Non-Zen currencies require the multi-currency feature to be active.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="slot">The absolute inventory slot of the item; store items live in slots 204 to 235.</param>
    /// <param name="price">The price.</param>
    public async ValueTask SetPriceAsync(Player player, byte slot, ShopPrice price)
    {
        var result = this.Validate(player, price);
        if (result == ItemPriceResult.Success)
        {
            result = this.ApplyPrice(player, slot, price);
        }

        await player.InvokeViewPlugInAsync<IItemPriceSetResponsePlugIn>(p => p.ItemPriceSetResponseAsync(slot, result)).ConfigureAwait(false);
    }

    private ItemPriceResult Validate(Player player, ShopPrice price)
    {
        if (player.ShopStorage?.StoreOpen ?? false)
        {
            return ItemPriceResult.Failed;
        }

        if (player.Level < 6)
        {
            return ItemPriceResult.CharacterLevelTooLow;
        }

        if (price.Amount < 0)
        {
            return ItemPriceResult.PriceNegative;
        }

        var settings = MultiCurrencyPlayerShopFeaturePlugIn.GetSettings(player.GameContext);
        var maxAmount = settings?.MaximumPriceAmount ?? int.MaxValue;
        if (price.Amount > maxAmount)
        {
            return ItemPriceResult.Failed;
        }

        // The kind cap keeps display and charge identical: jewel amounts travel in a
        // 16-bit wire field, so they can never exceed it, whatever is configured.
        var kindCap = int.MaxValue;
        if (price.IsJewel)
        {
            kindCap = Math.Min(settings?.MaximumJewelAmount ?? int.MaxValue, ushort.MaxValue);
        }
        else if (price.IsAccountCoin)
        {
            kindCap = settings?.MaximumCoinAmount ?? int.MaxValue;
        }

        if (price.Amount > kindCap)
        {
            return ItemPriceResult.Failed;
        }

        if (!MultiCurrencyPlayerShopFeaturePlugIn.IsCurrencyAllowed(settings, price.Currency))
        {
            return ItemPriceResult.Failed;
        }

        return ItemPriceResult.Success;
    }

    private ItemPriceResult ApplyPrice(Player player, byte slot, ShopPrice price)
    {
        var item = player.SelectedCharacter?.Inventory?.Items?.FirstOrDefault(i => i.ItemSlot == slot);
        if (item is null)
        {
            return ItemPriceResult.ItemNotFound;
        }

        if (price.Amount > 0)
        {
            item.StorePrice = price.Amount;
            item.StorePriceCurrency = price.Currency;
        }
        else
        {
            item.StorePrice = null;
            item.StorePriceCurrency = PlayerShopCurrency.Zen;
        }

        return ItemPriceResult.Success;
    }
}
