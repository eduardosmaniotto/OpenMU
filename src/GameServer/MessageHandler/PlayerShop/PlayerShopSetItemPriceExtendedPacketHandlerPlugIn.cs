// <copyright file="PlayerShopSetItemPriceExtendedPacketHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.PlayerShop;

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;
using MUnique.OpenMU.Network.Packets.ClientToServer;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Packet handler which sets prices with currency for an item in the player shop (3F 09).
/// Only the extended client sends this packet; the currency is validated server-side.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.PlayerShopSetItemPriceExtendedPacketHandlerPlugIn_Name), Description = nameof(PlugInResources.PlayerShopSetItemPriceExtendedPacketHandlerPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("3674F77E-3DDF-4CC7-B436-FB479D2921C2")]
[BelongsToGroup(StoreHandlerGroupPlugIn.GroupKey)]
internal class PlayerShopSetItemPriceExtendedPacketHandlerPlugIn : ISubPacketHandlerPlugIn
{
    private readonly SetItemPriceAction _setPriceAction = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => true;

    /// <inheritdoc/>
    public byte Key => PlayerShopSetItemPriceExtended.SubCode;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        if (packet.Length < PlayerShopSetItemPriceExtended.Length)
        {
            player.Logger.LogWarning("Too short extended shop price packet: {length}, possible hacker", packet.Length);
            return;
        }

        PlayerShopSetItemPriceExtended message = packet;
        if (!Enum.IsDefined(message.Currency))
        {
            player.Logger.LogWarning("Unknown shop currency: {currency}, possible hacker", message.Currency);
            return;
        }

        var currency = (PlayerShopCurrency)message.Currency;
        player.Logger.LogDebug("Player [{0}] sets price of slot {1} to {2} {3}", player.SelectedCharacter?.Name, message.ItemSlot, message.Price, currency);
        await this._setPriceAction.SetPriceAsync(player, message.ItemSlot, new ShopPrice(currency, (int)message.Price)).ConfigureAwait(false);
    }
}
