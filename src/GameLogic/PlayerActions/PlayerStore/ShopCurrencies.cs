// <copyright file="ShopCurrencies.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

using MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// The single table of all shop currencies. Adding a currency means adding one row
/// here, one configuration flag, and (for jewels) nothing else in <c>GameLogic</c>;
/// the compiler forces the remaining spots (exhaustive switches) by itself.
/// </summary>
public static class ShopCurrencies
{
    private static readonly IReadOnlyList<ShopCurrencyDescriptor> Descriptors = new[]
    {
        new ShopCurrencyDescriptor(PlayerShopCurrency.Zen, ShopCurrencyKind.Zen, new[] { "zen" }, null, _ => true),
        new ShopCurrencyDescriptor(PlayerShopCurrency.JewelOfBless, ShopCurrencyKind.Jewel, new[] { "bless", "jewelofbless", "jewel-of-bless" }, ItemConstants.JewelOfBless, c => c.AllowJewelOfBless),
        new ShopCurrencyDescriptor(PlayerShopCurrency.JewelOfSoul, ShopCurrencyKind.Jewel, new[] { "soul", "jewelofsoul", "jewel-of-soul" }, ItemConstants.JewelOfSoul, c => c.AllowJewelOfSoul),
        new ShopCurrencyDescriptor(PlayerShopCurrency.JewelOfChaos, ShopCurrencyKind.Jewel, new[] { "chaos", "jewelofchaos", "jewel-of-chaos" }, ItemConstants.JewelOfChaos, c => c.AllowJewelOfChaos),
        new ShopCurrencyDescriptor(PlayerShopCurrency.JewelOfCreation, ShopCurrencyKind.Jewel, new[] { "creation", "jewelofcreation", "jewel-of-creation" }, ItemConstants.JewelOfCreation, c => c.AllowJewelOfCreation),
        new ShopCurrencyDescriptor(PlayerShopCurrency.JewelOfLife, ShopCurrencyKind.Jewel, new[] { "life", "jeweloflife", "jewel-of-life" }, ItemConstants.JewelOfLife, c => c.AllowJewelOfLife),
        new ShopCurrencyDescriptor(PlayerShopCurrency.WCoinC, ShopCurrencyKind.AccountCoin, new[] { "wcoinc", "wcoin-c" }, null, c => c.AllowWCoinC),
        new ShopCurrencyDescriptor(PlayerShopCurrency.WCoinP, ShopCurrencyKind.AccountCoin, new[] { "wcoinp", "wcoin-p" }, null, c => c.AllowWCoinP),
        new ShopCurrencyDescriptor(PlayerShopCurrency.GoblinPoints, ShopCurrencyKind.AccountCoin, new[] { "goblin", "goblinpoints", "goblin-points" }, null, c => c.AllowGoblinPoints),
    };

    /// <summary>
    /// Gets the canonical chat names of all currencies, for user-facing messages.
    /// </summary>
    public static string CanonicalNames => string.Join(", ", Descriptors.Select(d => d.Aliases[0]));

    /// <summary>
    /// Gets the canonical chat name of a currency.
    /// </summary>
    /// <param name="currency">The currency.</param>
    /// <returns>The canonical name; "zen" for unknown values.</returns>
    public static string CanonicalNameOf(PlayerShopCurrency currency)
    {
        return TryGet(currency, out var descriptor) && descriptor is not null
            ? descriptor.Aliases[0]
            : PlayerShopCurrency.Zen.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// Gets the jewel identifier of a jewel currency.
    /// </summary>
    /// <param name="currency">The currency.</param>
    /// <returns>The jewel identifier.</returns>
    public static ItemIdentifier GetJewelIdentifier(PlayerShopCurrency currency)
    {
        if (TryGet(currency, out var descriptor)
            && descriptor is { Kind: ShopCurrencyKind.Jewel, JewelIdentifier: { } identifier })
        {
            return identifier;
        }

        throw new ArgumentOutOfRangeException(nameof(currency), currency, "Not a jewel currency.");
    }

    /// <summary>
    /// Tries to get the descriptor of a currency.
    /// </summary>
    /// <param name="currency">The currency.</param>
    /// <param name="descriptor">The descriptor, if known.</param>
    /// <returns><c>true</c>, if the currency is known.</returns>
    public static bool TryGet(PlayerShopCurrency currency, out ShopCurrencyDescriptor? descriptor)
    {
        foreach (var candidate in Descriptors)
        {
            if (candidate.Currency == currency)
            {
                descriptor = candidate;
                return true;
            }
        }

        descriptor = null;
        return false;
    }

    /// <summary>
    /// Tries to parse a chat name into a currency.
    /// </summary>
    /// <param name="text">The normalized (trimmed, lowercase) chat name.</param>
    /// <param name="currency">The currency, if known.</param>
    /// <returns><c>true</c>, if the name is known.</returns>
    public static bool TryParseAlias(string text, out PlayerShopCurrency currency)
    {
        foreach (var descriptor in Descriptors)
        {
            foreach (var alias in descriptor.Aliases)
            {
                if (alias == text)
                {
                    currency = descriptor.Currency;
                    return true;
                }
            }
        }

        currency = PlayerShopCurrency.Zen;
        return false;
    }
}
