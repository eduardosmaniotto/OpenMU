// <copyright file="MultiCurrencyPlayerShopConfiguration.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

/// <summary>
/// The configuration of the <see cref="MultiCurrencyPlayerShopFeaturePlugIn"/>.
/// </summary>
/// <remarks>
/// Each currency is a plain boolean, so the admin panel renders it as a checkbox.
/// A list of currencies would fall back to a text field, because the admin panel
/// has no editor for collections of enums.
/// Zen is always allowed and therefore has no flag.
/// </remarks>
public class MultiCurrencyPlayerShopConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether prices in Jewel of Bless are allowed.
    /// </summary>
    public bool AllowJewelOfBless { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether prices in Jewel of Soul are allowed.
    /// </summary>
    public bool AllowJewelOfSoul { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether prices in Jewel of Chaos are allowed.
    /// </summary>
    public bool AllowJewelOfChaos { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether prices in Jewel of Creation are allowed.
    /// </summary>
    public bool AllowJewelOfCreation { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether prices in Jewel of Life are allowed.
    /// </summary>
    public bool AllowJewelOfLife { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether prices in WCoin (C) are allowed.
    /// </summary>
    public bool AllowWCoinC { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether prices in WCoin (P) are allowed.
    /// </summary>
    public bool AllowWCoinP { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether prices in Goblin Points are allowed.
    /// </summary>
    public bool AllowGoblinPoints { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum price amount, regardless of currency.
    /// It prevents overflow and absurd listings.
    /// The client sends the price as 32-bit integer.
    /// </summary>
    public int MaximumPriceAmount { get; set; } = 999999999;

    /// <summary>
    /// Gets or sets the maximum price amount for jewel currencies.
    /// It must stay low enough that a buyer can actually own that many
    /// jewels and a seller has the inventory space to receive them.
    /// </summary>
    public int MaximumJewelAmount { get; set; } = 999;

    /// <summary>
    /// Gets or sets the maximum price amount for account coin currencies.
    /// </summary>
    public int MaximumCoinAmount { get; set; } = 999999999;
}
