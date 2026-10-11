// <copyright file="ShopPaymentResult.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

/// <summary>
/// The outcome of a shop payment transfer. It names the reason, so the caller
/// never has to guess it with a second check.
/// </summary>
public enum ShopPaymentResult
{
    /// <summary>
    /// The payment moved from buyer to seller.
    /// </summary>
    Success,

    /// <summary>
    /// The buyer cannot pay the amount with exact tender.
    /// </summary>
    InsufficientFunds,

    /// <summary>
    /// The seller cannot receive the payment (balance overflow or no inventory space).
    /// </summary>
    ReceiverCannotHold,

    /// <summary>
    /// The payment was rolled back because it could not be saved.
    /// </summary>
    SaveFailed,
}
