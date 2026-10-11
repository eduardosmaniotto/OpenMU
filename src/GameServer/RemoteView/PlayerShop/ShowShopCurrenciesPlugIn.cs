// <copyright file="ShowShopCurrenciesPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer.RemoteView.PlayerShop;

using System.Runtime.InteropServices;
using MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;
using MUnique.OpenMU.GameLogic.Views.PlayerShop;
using MUnique.OpenMU.Network;
using MUnique.OpenMU.Network.Packets.ServerToClient;
using MUnique.OpenMU.Network.PlugIns;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The extended implementation of the <see cref="IShowShopCurrenciesPlugIn"/> which informs
/// the client about the enabled shop currencies. Only extended clients receive it;
/// vanilla clients only trade in Zen and need no currency list.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.ShowShopCurrenciesPlugIn_Name), Description = nameof(PlugInResources.ShowShopCurrenciesPlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("394D33BF-87DC-4FFF-80ED-CCD829F12F1E")]
[MinimumClient(ExtendedShopClient.Season, ExtendedShopClient.Episode, ClientLanguage.Invariant)]
public class ShowShopCurrenciesPlugIn : IShowShopCurrenciesPlugIn
{
    private readonly RemotePlayer _player;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowShopCurrenciesPlugIn"/> class.
    /// </summary>
    /// <param name="player">The player.</param>
    public ShowShopCurrenciesPlugIn(RemotePlayer player) => this._player = player;

    /// <inheritdoc/>
    public async ValueTask ShowShopCurrenciesAsync()
    {
        var connection = this._player.Connection;
        if (connection is null)
        {
            return;
        }

        var settings = MultiCurrencyPlayerShopFeaturePlugIn.GetSettings(this._player.GameContext);
        await connection.SendPlayerShopCurrenciesExtendedAsync(MultiCurrencyPlayerShopFeaturePlugIn.GetCurrencyMask(settings)).ConfigureAwait(false);
    }
}
