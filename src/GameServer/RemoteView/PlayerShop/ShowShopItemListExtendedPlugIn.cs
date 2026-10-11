// <copyright file="ShowShopItemListExtendedPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.PlayerShop;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;
using MUnique.OpenMU.GameLogic.Views;
using MUnique.OpenMU.GameLogic.Views.PlayerShop;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The extended implementation of the <see cref="IShowShopItemListPlugIn"/> which is forwarding everything to the game client with specific data packets.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.ShowShopItemListExtendedPlugIn_Name), Description = nameof(PlugInResources.ShowShopItemListExtendedPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("D64E9027-4801-46EE-9FD0-2FC66C33FE32")]
[MinimumClient(ExtendedShopClient.Season, ExtendedShopClient.Episode, ClientLanguage.Invariant)]
public class ShowShopItemListExtendedPlugIn : IShowShopItemListPlugIn
{
    /// <summary>
    /// The <c>PriceItemType</c> value for WCoin (C) prices; the amount is in <c>MoneyPrice</c>.
    /// </summary>
    private const ushort WCoinCPriceItemType = 0xFFF0;

    /// <summary>
    /// The <c>PriceItemType</c> value for WCoin (P) prices; the amount is in <c>MoneyPrice</c>.
    /// </summary>
    private const ushort WCoinPPriceItemType = 0xFFF1;

    /// <summary>
    /// The <c>PriceItemType</c> value for Goblin Points prices; the amount is in <c>MoneyPrice</c>.
    /// </summary>
    private const ushort GoblinPointsPriceItemType = 0xFFF2;

    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowShopItemListExtendedPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public ShowShopItemListExtendedPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc/>
    /// <remarks>
    /// Maybe cache the result, because a lot of players could request the same list. However, this isn't critical.
    /// </remarks>
    public async ValueTask ShowShopItemListAsync(Player requestedPlayer, bool isUpdate)
    {
        var connection = this._player.Connection;
        if (connection is null || requestedPlayer.ShopStorage is null || requestedPlayer.SelectedCharacter is null)
        {
            return;
        }

        var itemSerializer = this._player.ItemSerializer;
        var playerId = requestedPlayer.GetId(this._player);

        // Items priced in a meanwhile-disabled currency are hidden as well;
        // they cannot be bought until they are repriced.
        var items = requestedPlayer.ShopStorage.Items
            .Where(item => item.StorePrice.HasValue
                && MultiCurrencyPlayerShopFeaturePlugIn.IsCurrencyAllowed(this._player.GameContext, item.StorePriceCurrency))
            .ToList();
        int Write()
        {
            var size = PlayerShopItemListExtendedRef.GetRequiredSize(items.Count, PlayerShopItemExtendedRef.GetRequiredSize(itemSerializer.NeededSpace));
            var span = connection.Output.GetSpan(size)[..size];
            _ = new PlayerShopItemListExtendedRef(span)
            {
                Action = isUpdate
                    ? PlayerShopItemListExtended.ActionKind.UpdateAfterItemChange
                    : PlayerShopItemListExtended.ActionKind.ByRequest,
                ItemCount = (byte)items.Count,
                PlayerId = playerId,
                PlayerName = requestedPlayer.SelectedCharacter.Name,
                ShopName = requestedPlayer.SelectedCharacter.StoreName,
            };

            int headerSize = PlayerShopItemListExtendedRef.GetRequiredSize(0, 0);
            int actualSize = headerSize;
            foreach (var item in items)
            {
                var itemBlock = new PlayerShopItemExtendedRef(span[actualSize..]);
                itemBlock.ItemSlot = item.ItemSlot;
                this.WritePrice(itemBlock, item.StorePriceCurrency, (uint)(item.StorePrice ?? 0));

                var itemSize = itemSerializer.SerializeItem(itemBlock.ItemData, item);
                actualSize += PlayerShopItemExtendedRef.GetRequiredSize(itemSize);
            }

            span.Slice(0, actualSize).SetPacketSize();
            return actualSize;
        }

        await connection.SendAsync(Write).ConfigureAwait(false);
    }

    /// <summary>
    /// Encodes a jewel identifier as the <c>PriceItemType</c> value: the item group
    /// in the highest 4 bits, the item number in the remaining ones.
    /// </summary>
    /// <param name="jewel">The jewel identifier.</param>
    /// <returns>The encoded item type.</returns>
    private static ushort EncodeJewel(ItemIdentifier jewel)
    {
        return (ushort)(((ushort)(jewel.Group << 12)) | ((ushort)(jewel.Number ?? 0)));
    }

    /// <summary>
    /// Writes the price of an item into the packet block.
    /// Zen uses <c>MoneyPrice</c> with an empty item type; jewels use <c>PriceItemType</c>
    /// (high 4 bits group, low 12 bits number) plus <c>RequiredItemAmount</c>; account coins
    /// have no item definition, so they use a reserved <c>PriceItemType</c> sentinel with the
    /// amount in <c>MoneyPrice</c>. Amounts always fit by construction: the set-price action
    /// rejects anything above the configured per-currency maximum.
    /// </summary>
    /// <param name="itemBlock">The packet block.</param>
    /// <param name="currency">The price currency.</param>
    /// <param name="amount">The price amount.</param>
    private void WritePrice(PlayerShopItemExtendedRef itemBlock, PlayerShopCurrency currency, uint amount)
    {
        switch (currency)
        {
            case PlayerShopCurrency.Zen:
                itemBlock.MoneyPrice = amount;
                itemBlock.PriceItemType = 0;
                itemBlock.RequiredItemAmount = 0;
                break;
            case PlayerShopCurrency.JewelOfBless:
            case PlayerShopCurrency.JewelOfSoul:
            case PlayerShopCurrency.JewelOfChaos:
            case PlayerShopCurrency.JewelOfCreation:
            case PlayerShopCurrency.JewelOfLife:
                itemBlock.MoneyPrice = 0;
                itemBlock.PriceItemType = EncodeJewel(ShopCurrencies.GetJewelIdentifier(currency));
                itemBlock.RequiredItemAmount = (ushort)amount;
                break;
            case PlayerShopCurrency.WCoinC:
                itemBlock.MoneyPrice = amount;
                itemBlock.PriceItemType = WCoinCPriceItemType;
                itemBlock.RequiredItemAmount = 0;
                break;
            case PlayerShopCurrency.WCoinP:
                itemBlock.MoneyPrice = amount;
                itemBlock.PriceItemType = WCoinPPriceItemType;
                itemBlock.RequiredItemAmount = 0;
                break;
            case PlayerShopCurrency.GoblinPoints:
                itemBlock.MoneyPrice = amount;
                itemBlock.PriceItemType = GoblinPointsPriceItemType;
                itemBlock.RequiredItemAmount = 0;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(currency), currency, "Unknown shop currency.");
        }
    }
}