// <copyright file="IShopCurrencyExchange.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

using MUnique.OpenMU.DataModel.Entities;

/// <summary>
/// Moves the payment of a personal store purchase from buyer to seller.
/// Implementations cover one currency kind each; callers select them
/// through <see cref="ShopCurrencyExchangeFactory"/> instead of branching.
/// </summary>
public interface IShopCurrencyExchange
{
    /// <summary>
    /// Gets the currency this exchange handles.
    /// </summary>
    PlayerShopCurrency Currency { get; }

    /// <summary>
    /// Determines whether the buyer can currently pay the amount with exact tender:
    /// whole rows only, nothing is split and no change is made.
    /// </summary>
    /// <param name="player">The buying player.</param>
    /// <param name="amount">The amount.</param>
    /// <returns><c>true</c>, if the amount can be paid exactly.</returns>
    bool CanCover(Player player, int amount);

    /// <summary>
    /// Moves <paramref name="amount"/> from buyer to seller.
    /// Must only be called while holding the seller's <c>StoreLock</c>,
    /// after the listing was re-checked. No partial state on failure:
    /// either the full amount moves, or nothing changes.
    /// For jewel payments, the moved rows are returned, so the caller can persist
    /// them together with the sold item in a single save.
    /// </summary>
    /// <param name="buyer">The buying player.</param>
    /// <param name="seller">The selling player.</param>
    /// <param name="amount">The amount.</param>
    /// <returns>The outcome and, on success, the moved payment rows.</returns>
    ValueTask<(ShopPaymentResult Result, IReadOnlyList<Item> MovedRows)> TryTransferAsync(Player buyer, Player seller, int amount);

    /// <summary>
    /// Refreshes the balance display of the player after a successful payment
    /// (money update, cash shop points). Item payments notify per row during
    /// the transfer instead.
    /// </summary>
    /// <param name="player">The player.</param>
    ValueTask NotifyBalanceChangedAsync(Player player);
}
