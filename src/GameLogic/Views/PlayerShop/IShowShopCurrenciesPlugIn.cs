// <copyright file="IShowShopCurrenciesPlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.Views.PlayerShop;

/// <summary>
/// Interface of a view which informs the client about the shop currencies
/// it may offer, so the client only shows valid ones.
/// </summary>
public interface IShowShopCurrenciesPlugIn : IViewPlugIn
{
    /// <summary>
    /// Shows the enabled shop currencies of the multi-currency shop feature.
    /// </summary>
    ValueTask ShowShopCurrenciesAsync();
}
