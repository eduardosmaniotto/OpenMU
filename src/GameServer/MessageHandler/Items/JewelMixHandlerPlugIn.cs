// <copyright file="JewelMixHandlerPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.MessageHandler.Items;

using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.PlayerActions.Items;
using MUnique.OpenMU.Network.Packets.ClientToServer;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// Handler for jewel mix packets.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.JewelMixHandlerPlugIn_Name), Description = nameof(PlugInResources.JewelMixHandlerPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("d6067475-a910-488d-8450-9310ae394c47")]
internal class JewelMixHandlerPlugIn : IPacketHandlerPlugIn
{
    private readonly ItemStackAction _mixAction = new();

    /// <inheritdoc/>
    public bool IsEncryptionExpected => LahapJewelMixRequest.HeaderType >= 0xC3;

    /// <inheritdoc/>
    public byte Key => LahapJewelMixRequest.Code;

    /// <inheritdoc/>
    public async ValueTask HandlePacketAsync(Player player, Memory<byte> packet)
    {
        LahapJewelMixRequest message = packet;
        if (packet.Length < 6)
        {
            return;
        }

        switch (message.Operation)
        {
            case LahapJewelMixRequest.MixType.Mix:
                if (TryGetStackSize(message.MixingStackSize, out var stackSize))
                {
                    await this._mixAction.StackItemsAsync(player, (byte)message.Item, stackSize).ConfigureAwait(false);
                }
                else
                {
                    player.Logger.LogWarning("Unknown jewel mix stack size: {stackSize}, possible hacker", message.MixingStackSize);
                }

                break;
            case LahapJewelMixRequest.MixType.Unmix:
                await this._mixAction.UnstackItemsAsync(player, (byte)message.Item, message.UnmixingSourceSlot).ConfigureAwait(false);
                break;
            default:
                player.Logger.LogWarning("Unknown jewel mix operation: {operation}, possible hacker", message.Operation);
                break;
        }
    }

    private static bool TryGetStackSize(LahapJewelMixRequest.StackSize stackSize, out byte size)
    {
        switch (stackSize)
        {
            case LahapJewelMixRequest.StackSize.Ten:
                size = 10;
                return true;
            case LahapJewelMixRequest.StackSize.Twenty:
                size = 20;
                return true;
            case LahapJewelMixRequest.StackSize.Thirty:
                size = 30;
                return true;
            default:
                size = 0;
                return false;
        }
    }
}