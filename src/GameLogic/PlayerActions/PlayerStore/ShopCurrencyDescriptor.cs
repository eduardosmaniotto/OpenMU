// <copyright file="ShopCurrencyDescriptor.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

using MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// Describes one shop currency: how players name it, what kind it is,
/// which jewel it maps to, and which configuration flag enables it.
/// </summary>
/// <param name="Currency">The currency.</param>
/// <param name="Kind">The kind which decides payment and display.</param>
/// <param name="Aliases">The lowercase chat names, the first one is the canonical name.</param>
/// <param name="JewelIdentifier">The jewel identifier; <c>null</c> for non-jewels.</param>
/// <param name="IsEnabled">Reads the enabling configuration flag.</param>
public sealed record ShopCurrencyDescriptor(
    PlayerShopCurrency Currency,
    ShopCurrencyKind Kind,
    string[] Aliases,
    ItemIdentifier? JewelIdentifier,
    Func<MultiCurrencyPlayerShopConfiguration, bool> IsEnabled);
