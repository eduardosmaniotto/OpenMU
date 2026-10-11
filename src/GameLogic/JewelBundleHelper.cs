// <copyright file="JewelBundleHelper.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic;

/// <summary>
/// Converts between packed jewel bundles and their piece count.
/// A bundle's <see cref="Item.Level"/> encodes how many single jewels it holds.
/// </summary>
public static class JewelBundleHelper
{
    /// <summary>
    /// The highest bundle level legitimate mixes produce (10/20/30 pieces).
    /// <see cref="DataModel.Configuration.JewelMix"/> carries no size of its own;
    /// sizes are fixed by the mix protocol, so anything above this cannot exist
    /// legitimately and is rejected instead of wrapping around.
    /// </summary>
    public const byte MaxBundleLevel = 2;

    /// <summary>
    /// Gets the number of single jewels a packed bundle holds.
    /// </summary>
    /// <param name="bundleLevel">The level of the bundle item.</param>
    /// <param name="pieces">The piece count.</param>
    /// <returns><c>true</c>, if the level is legitimate; otherwise, <c>false</c>.</returns>
    public static bool TryGetPieceCount(byte bundleLevel, out int pieces)
    {
        if (bundleLevel > MaxBundleLevel)
        {
            pieces = 0;
            return false;
        }

        pieces = (bundleLevel + 1) * 10;
        return true;
    }

    /// <summary>
    /// Gets the bundle level for a number of single jewels.
    /// </summary>
    /// <param name="pieceCount">The number of single jewels; a positive multiple of 10.</param>
    /// <returns>The level of the resulting bundle item.</returns>
    public static byte GetBundleLevel(int pieceCount) => (byte)((pieceCount / 10) - 1);
}
