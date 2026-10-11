// <copyright file="ShopCurrencyKind.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

/// <summary>
/// The kind of a shop currency. It decides how a price is paid and displayed.
/// </summary>
public enum ShopCurrencyKind
{
    /// <summary>
    /// Zen (game money), paid from the inventory balance.
    /// </summary>
    Zen,

    /// <summary>
    /// A jewel item, paid with inventory items.
    /// </summary>
    Jewel,

    /// <summary>
    /// An account balance (WCoin, Goblin Points).
    /// </summary>
    AccountCoin,
}
