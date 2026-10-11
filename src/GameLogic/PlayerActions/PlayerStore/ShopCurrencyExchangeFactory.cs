// <copyright file="ShopCurrencyExchangeFactory.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

using MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// Selects the <see cref="IShopCurrencyExchange"/> for a currency.
/// The single switch lives here, so the buy flow stays polymorphic.
/// </summary>
public static class ShopCurrencyExchangeFactory
{
    /// <summary>
    /// Gets the exchange for the currency.
    /// </summary>
    /// <param name="currency">The currency.</param>
    /// <returns>The exchange.</returns>
    public static IShopCurrencyExchange GetExchange(PlayerShopCurrency currency)
    {
        if (!ShopCurrencies.TryGet(currency, out var descriptor) || descriptor is null)
        {
            throw new ArgumentOutOfRangeException(nameof(currency), currency, "Unknown shop currency.");
        }

        return descriptor.Kind switch
        {
            ShopCurrencyKind.Zen => new ZenShopCurrencyExchange(),
            ShopCurrencyKind.Jewel => new JewelShopCurrencyExchange(currency),
            ShopCurrencyKind.AccountCoin => new AccountCoinShopCurrencyExchange(currency),
            _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, "Unknown shop currency kind."),
        };
    }
}
