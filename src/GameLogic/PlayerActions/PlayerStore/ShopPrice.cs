// <copyright file="ShopPrice.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

using MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// The price of a personal store item, expressed in a specific currency.
/// </summary>
/// <param name="Currency">The currency.</param>
/// <param name="Amount">The amount in the currency.</param>
public readonly record struct ShopPrice(PlayerShopCurrency Currency, int Amount)
{
    /// <summary>
    /// Gets a value indicating whether the price is a Zen price.
    /// </summary>
    public bool IsZen => this.Kind == ShopCurrencyKind.Zen;

    /// <summary>
    /// Gets a value indicating whether the price is paid with account coins.
    /// </summary>
    public bool IsAccountCoin => this.Kind == ShopCurrencyKind.AccountCoin;

    /// <summary>
    /// Gets a value indicating whether the price is paid with jewel items.
    /// </summary>
    public bool IsJewel => this.Kind == ShopCurrencyKind.Jewel;

    /// <summary>
    /// Gets a value indicating whether the price requires an extended client to display.
    /// </summary>
    public bool RequiresExtendedClient => !this.IsZen;

    private ShopCurrencyKind Kind => ShopCurrencies.TryGet(this.Currency, out var descriptor) && descriptor is not null
        ? descriptor.Kind
        : ShopCurrencyKind.Zen;

    /// <summary>
    /// Creates a <see cref="ShopPrice"/> from persisted item values.
    /// A <c>null</c> or non-positive amount means the item has no price,
    /// so a zero amount can never make an item free.
    /// </summary>
    /// <param name="currency">The persisted currency.</param>
    /// <param name="amount">The persisted amount.</param>
    /// <returns>The price, or <c>null</c> when the item is not priced.</returns>
    public static ShopPrice? FromItem(PlayerShopCurrency currency, int? amount)
    {
        if (amount is null || amount <= 0)
        {
            return null;
        }

        return new ShopPrice(currency, amount.Value);
    }
}
