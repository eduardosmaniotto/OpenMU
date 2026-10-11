// <copyright file="PlayerShopCurrency.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// The currency in which a personal store item price is expressed.
/// Persisted per <see cref="Item"/>; values are a contract, append only.
/// </summary>
public enum PlayerShopCurrency
{
    /// <summary>
    /// Zen (game money). Default for all existing rows.
    /// </summary>
    Zen = 0,

    /// <summary>
    /// Jewel of Bless (physical inventory item).
    /// </summary>
    JewelOfBless = 1,

    /// <summary>
    /// Jewel of Soul (physical inventory item).
    /// </summary>
    JewelOfSoul = 2,

    /// <summary>
    /// Jewel of Chaos (physical inventory item).
    /// </summary>
    JewelOfChaos = 3,

    /// <summary>
    /// Jewel of Creation (physical inventory item).
    /// </summary>
    JewelOfCreation = 4,

    /// <summary>
    /// Jewel of Life (physical inventory item).
    /// </summary>
    JewelOfLife = 5,

    /// <summary>
    /// WCoin (C) account balance.
    /// </summary>
    WCoinC = 6,

    /// <summary>
    /// WCoin (P) account balance.
    /// </summary>
    WCoinP = 7,

    /// <summary>
    /// Goblin Points account balance.
    /// </summary>
    GoblinPoints = 8,
}
