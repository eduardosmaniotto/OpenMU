// <copyright file="BuyRequestAction.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.PlugIns;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.Inventory;
using MUnique.OpenMU.Interfaces;

/// <summary>
/// Action to buy an item from another player shop.
/// </summary>
public class BuyRequestAction
{
    private readonly CloseStoreAction _closeStoreAction = new();

    /// <summary>
    /// Buys the item from another player shop.
    /// </summary>
    /// <param name="player">The player.</param>
    /// <param name="requestedPlayer">The requested player.</param>
    /// <param name="slot">The slot.</param>
    public async ValueTask BuyItemAsync(Player player, Player requestedPlayer, byte slot)
    {
        using var loggerScope = player.Logger.BeginScope(this.GetType());
        if (requestedPlayer.IsTemplatePlayer || player.IsTemplatePlayer)
        {
            await player.InvokeViewPlugInAsync<IPlayerShopBuyRequestResultPlugIn>(p => p.ShowResultAsync(requestedPlayer, ItemBuyResult.ItemBlock, null)).ConfigureAwait(false);
            return;
        }

        if (!(requestedPlayer.ShopStorage?.StoreOpen ?? false))
        {
            player.Logger.LogDebug("Store not open, Character {0}", requestedPlayer.SelectedCharacter?.Name);
            await player.InvokeViewPlugInAsync<IPlayerShopBuyRequestResultPlugIn>(p => p.ShowResultAsync(requestedPlayer, ItemBuyResult.ShopNotOpened, null)).ConfigureAwait(false);
            return;
        }

        if (slot < InventoryConstants.FirstStoreItemSlotIndex)
        {
            player.Logger.LogWarning("Store Slot too low: {0}, possible hacker", slot);
            await player.InvokeViewPlugInAsync<IPlayerShopBuyRequestResultPlugIn>(p => p.ShowResultAsync(requestedPlayer, ItemBuyResult.InvalidShopSlot, null)).ConfigureAwait(false);
            return;
        }

        var item = requestedPlayer.ShopStorage.GetItem(slot);
        var price = GetShopPrice(item);
        if (item is null || price is null)
        {
            player.Logger.LogDebug("Item unavailable, Slot {0}", slot);
            await player.InvokeViewPlugInAsync<IPlayerShopBuyRequestResultPlugIn>(p => p.ShowResultAsync(requestedPlayer, ItemBuyResult.NameMismatchOrPriceMissing, null)).ConfigureAwait(false);
            return;
        }

        var requestedPrice = price.Value;
        if (!MultiCurrencyPlayerShopFeaturePlugIn.IsCurrencyAllowed(player.GameContext, requestedPrice.Currency))
        {
            player.Logger.LogDebug("Currency {0} of slot {1} is not allowed", requestedPrice.Currency, slot);
            await player.InvokeViewPlugInAsync<IPlayerShopBuyRequestResultPlugIn>(p => p.ShowResultAsync(requestedPlayer, ItemBuyResult.NameMismatchOrPriceMissing, null)).ConfigureAwait(false);
            return;
        }

        if (requestedPrice.RequiresExtendedClient && !player.SupportsMultiCurrencyShop)
        {
            player.Logger.LogWarning("Player {0} tried to buy non-Zen item from slot {1} with a vanilla client, possible hacker", player, slot);
            await player.InvokeViewPlugInAsync<IPlayerShopBuyRequestResultPlugIn>(p => p.ShowResultAsync(requestedPlayer, ItemBuyResult.ItemBlock, null)).ConfigureAwait(false);
            return;
        }

        // Checked here, and not only when the item is moved into the store: the store may
        // contain it since before the item rules existed.
        if (item.Definition is { IsPersonalStoreSellable: false })
        {
            player.Logger.LogWarning(
                "Player {0} tried to buy {1} from the store of {2}, which its item definition doesn't allow to sell in a personal store.",
                player,
                item,
                requestedPlayer);
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.ItemCannotBeSoldInPersonalStore)).ConfigureAwait(false);
            await player.InvokeViewPlugInAsync<IPlayerShopBuyRequestResultPlugIn>(p => p.ShowResultAsync(requestedPlayer, ItemBuyResult.ItemBlock, null)).ConfigureAwait(false);
            return;
        }

        var exchange = ShopCurrencyExchangeFactory.GetExchange(requestedPrice.Currency);
        if (!exchange.CanCover(player, requestedPrice.Amount))
        {
            await this.NotifyInsufficientFundsAsync(player, requestedPlayer, requestedPrice).ConfigureAwait(false);
            return;
        }

        // Check Inv Space
        var freeslot = player.Inventory?.CheckInvSpace(item);
        if (freeslot is null)
        {
            await player.InvokeViewPlugInAsync<IPlayerShopBuyRequestResultPlugIn>(p => p.ShowResultAsync(requestedPlayer, ItemBuyResult.MoneyOverflowOrNotEnoughSpace, null)).ConfigureAwait(false);
            await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.InventoryNotEnoughSpace)).ConfigureAwait(false);
            return;
        }

        bool itemSold = false;
        using (await requestedPlayer.ShopStorage.StoreLock.LockAsync().ConfigureAwait(false))
        {
            if (!requestedPlayer.ShopStorage.StoreOpen)
            {
                player.Logger.LogDebug("Store not open anymore, Character {0}", requestedPlayer.SelectedCharacter?.Name);
                await player.InvokeViewPlugInAsync<IPlayerShopBuyRequestResultPlugIn>(p => p.ShowResultAsync(requestedPlayer, ItemBuyResult.ShopNotOpened, null)).ConfigureAwait(false);
                return;
            }

            item = requestedPlayer.ShopStorage.GetItem(slot);
            var lockedPrice = GetShopPrice(item);
            if (item is null || lockedPrice is null)
            {
                await player.InvokeViewPlugInAsync<IPlayerShopBuyRequestResultPlugIn>(p => p.ShowResultAsync(requestedPlayer, ItemBuyResult.InvalidShopSlot, null)).ConfigureAwait(false);
                return;
            }

            if (lockedPrice.Value.Currency != requestedPrice.Currency || lockedPrice.Value.Amount != requestedPrice.Amount)
            {
                await player.InvokeViewPlugInAsync<IPlayerShopBuyRequestResultPlugIn>(p => p.ShowResultAsync(requestedPlayer, ItemBuyResult.NameMismatchOrPriceMissing, null)).ConfigureAwait(false);
                return;
            }

            player.Logger.LogDebug("BuyRequest, Item Price: {0} {1}", lockedPrice.Value.Amount, lockedPrice.Value.Currency);
            var payment = await exchange.TryTransferAsync(player, requestedPlayer, lockedPrice.Value.Amount).ConfigureAwait(false);
            switch (payment.Result)
            {
                case ShopPaymentResult.Success:
                    await this.MoveSoldItemAsync(player, requestedPlayer, item, slot, (byte)freeslot, payment.MovedRows, exchange).ConfigureAwait(false);
                    itemSold = true;
                    break;
                case ShopPaymentResult.InsufficientFunds:
                    await this.NotifyInsufficientFundsAsync(player, requestedPlayer, lockedPrice.Value).ConfigureAwait(false);
                    break;
                case ShopPaymentResult.ReceiverCannotHold:
                    await player.InvokeViewPlugInAsync<IPlayerShopBuyRequestResultPlugIn>(p => p.ShowResultAsync(requestedPlayer, ItemBuyResult.MoneyOverflowOrNotEnoughSpace, null)).ConfigureAwait(false);
                    await player.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.SellerInventoryFull)).ConfigureAwait(false);
                    break;
                default:
                    player.Logger.LogWarning("Shop payment of player {0} for slot {1} could not be saved.", player, slot);
                    await player.InvokeViewPlugInAsync<IPlayerShopBuyRequestResultPlugIn>(p => p.ShowResultAsync(requestedPlayer, ItemBuyResult.Undefined, null)).ConfigureAwait(false);
                    break;
            }
        }

        if (itemSold)
        {
            if (requestedPlayer.ShopStorage.Items.Any())
            {
                // this update may be sent to other players as well which are currently looking at the store
                await player.InvokeViewPlugInAsync<Views.PlayerShop.IShowShopItemListPlugIn>(p => p.ShowShopItemListAsync(requestedPlayer, true)).ConfigureAwait(false);
            }
            else
            {
                await this._closeStoreAction.CloseStoreAsync(requestedPlayer).ConfigureAwait(false);
            }
        }
    }

    private static ShopPrice? GetShopPrice(Item? item)
    {
        return ShopPrice.FromItem(item?.StorePriceCurrency ?? PlayerShopCurrency.Zen, item?.StorePrice);
    }

    private async ValueTask NotifyInsufficientFundsAsync(Player buyer, Player seller, ShopPrice price)
    {
        await buyer.InvokeViewPlugInAsync<IPlayerShopBuyRequestResultPlugIn>(p => p.ShowResultAsync(seller, ItemBuyResult.LackOfMoney, null)).ConfigureAwait(false);
        if (!price.IsZen)
        {
            // The client renders LackOfMoney with its own Zen-specific text;
            // name the actual currency so non-Zen prices are not misleading.
            await buyer.ShowLocalizedBlueMessageAsync(nameof(PlayerMessage.NotEnoughShopCurrency), price.Amount, ShopCurrencies.CanonicalNameOf(price.Currency)).ConfigureAwait(false);
        }
    }

    private async ValueTask MoveSoldItemAsync(
        Player buyer,
        Player seller,
        Item item,
        byte slot,
        byte freeslot,
        IReadOnlyList<Item> paymentRows,
        IShopCurrencyExchange exchange)
    {
        using var itemContext = seller.GameContext.PersistenceContextProvider.CreateNewTradeContext();
        foreach (var row in paymentRows)
        {
            itemContext.Attach(row);
        }

        itemContext.Attach(item);
        await seller.ShopStorage!.RemoveItemAsync(item).ConfigureAwait(false);
        await seller.InvokeViewPlugInAsync<IItemSoldByPlayerShopPlugIn>(p => p.ItemSoldByPlayerShopAsync(slot, buyer)).ConfigureAwait(false);
        await seller.InvokeViewPlugInAsync<IItemRemovedPlugIn>(p => p.RemoveItemAsync(slot)).ConfigureAwait(false);
        item.ItemSlot = (byte)freeslot;
        item.StorePrice = null;
        item.StorePriceCurrency = PlayerShopCurrency.Zen;
        await buyer.Inventory!.AddItemAsync(item).ConfigureAwait(false);
        seller.PersistenceContext.Detach(item);
        foreach (var row in paymentRows)
        {
            buyer.PersistenceContext.Detach(row);
        }

        await itemContext.SaveChangesAsync().ConfigureAwait(false);
        buyer.PersistenceContext.Attach(item);
        foreach (var row in paymentRows)
        {
            seller.PersistenceContext.Attach(row);
        }

        await buyer.InvokeViewPlugInAsync<IPlayerShopBuyRequestResultPlugIn>(p => p.ShowResultAsync(seller, ItemBuyResult.Success, item)).ConfigureAwait(false);
        await exchange.NotifyBalanceChangedAsync(seller).ConfigureAwait(false);
        await exchange.NotifyBalanceChangedAsync(buyer).ConfigureAwait(false);

        buyer.GameContext.PlugInManager.GetPlugInPoint<IItemSoldToOtherPlayerPlugIn>()?.ItemSold(seller, item, buyer);
    }
}
