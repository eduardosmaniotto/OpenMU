// <copyright file="ExtendedShopClient.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameServer;

/// <summary>
/// The minimum client version which supports the extended personal shop packets
/// (multi-currency prices and currency lists). Both the <c>MinimumClient</c>
/// attributes of the extended shop plug-ins and the runtime capability checks
/// derive from here, so they cannot drift apart.
/// </summary>
public static class ExtendedShopClient
{
    /// <summary>
    /// The minimum season of compatible clients.
    /// </summary>
    public const byte Season = 106;

    /// <summary>
    /// The minimum episode of compatible clients.
    /// </summary>
    public const byte Episode = 3;
}
