// <copyright file="ZenShopCurrencyExchange.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic.Views.Inventory;

/// <summary>
/// Moves Zen payments. This is the original shop behavior.
/// </summary>
public class ZenShopCurrencyExchange : IShopCurrencyExchange
{
    private static readonly IReadOnlyList<Item> NoRows = Array.Empty<Item>();

    /// <inheritdoc />
    public PlayerShopCurrency Currency => PlayerShopCurrency.Zen;

    /// <inheritdoc />
    public bool CanCover(Player player, int amount) => player.Money >= amount;

    /// <inheritdoc />
    public ValueTask<(ShopPaymentResult Result, IReadOnlyList<Item> MovedRows)> TryTransferAsync(Player buyer, Player seller, int amount)
    {
        if (!buyer.TryRemoveMoney(amount))
        {
            return ValueTask.FromResult((ShopPaymentResult.InsufficientFunds, NoRows));
        }

        if (!seller.TryAddMoney(amount))
        {
            buyer.TryAddMoney(amount);
            return ValueTask.FromResult((ShopPaymentResult.ReceiverCannotHold, NoRows));
        }

        return ValueTask.FromResult((ShopPaymentResult.Success, NoRows));
    }

    /// <inheritdoc />
    public async ValueTask NotifyBalanceChangedAsync(Player player)
    {
        await player.InvokeViewPlugInAsync<IUpdateMoneyPlugIn>(p => p.UpdateMoneyAsync()).ConfigureAwait(false);
    }
}
