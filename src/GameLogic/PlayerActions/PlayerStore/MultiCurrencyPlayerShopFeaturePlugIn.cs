// <copyright file="MultiCurrencyPlayerShopFeaturePlugIn.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;

using System.Runtime.InteropServices;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.PlugIns;

/// <summary>
/// The feature plugin for personal shops with multiple currencies.
/// When it's deactivated, shops work as before (Zen only); per-item
/// non-Zen prices can't be set and are neither shown nor buyable.
/// It's disabled by default, so server owners opt in explicitly.
/// </summary>
[PlugIn]
[Display(Name = nameof(PlugInResources.MultiCurrencyPlayerShopFeaturePlugIn_Name), Description = nameof(PlugInResources.MultiCurrencyPlayerShopFeaturePlugIn_Description), ResourceType = typeof(PlugInResources))]
[Guid("00728FA8-D0F6-47F8-94A6-078A250A1BF2")]
public class MultiCurrencyPlayerShopFeaturePlugIn : IFeaturePlugIn, ISupportCustomConfiguration<MultiCurrencyPlayerShopConfiguration>, ISupportDefaultCustomConfiguration, IDisabledByDefault
{
    /// <inheritdoc />
    public MultiCurrencyPlayerShopConfiguration? Configuration { get; set; }

    /// <summary>
    /// Gets the settings when the feature is active.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <returns>The settings; <c>null</c>, if the feature is deactivated.</returns>
    public static MultiCurrencyPlayerShopConfiguration? GetSettings(IGameContext gameContext)
    {
        return gameContext.FeaturePlugIns.GetPlugIn<MultiCurrencyPlayerShopFeaturePlugIn>() is { } plugIn
            ? plugIn.Configuration ?? new MultiCurrencyPlayerShopConfiguration()
            : null;
    }

    /// <summary>
    /// Determines whether the currency can be used for shop prices.
    /// Zen is always allowed, even when the feature is deactivated.
    /// </summary>
    /// <param name="gameContext">The game context.</param>
    /// <param name="currency">The currency.</param>
    /// <returns><c>true</c>, if the currency can be used.</returns>
    public static bool IsCurrencyAllowed(IGameContext gameContext, PlayerShopCurrency currency)
    {
        return IsCurrencyAllowed(GetSettings(gameContext), currency);
    }

    /// <summary>
    /// Determines whether the currency can be used for shop prices.
    /// This overload holds the single mapping between currencies and flags;
    /// <see cref="GetCurrencyMask"/> reuses it, so both stay consistent.
    /// </summary>
    /// <param name="settings">The settings; <c>null</c> when the feature is deactivated.</param>
    /// <param name="currency">The currency.</param>
    /// <returns><c>true</c>, if the currency can be used.</returns>
    public static bool IsCurrencyAllowed(MultiCurrencyPlayerShopConfiguration? settings, PlayerShopCurrency currency)
    {
        if (currency == PlayerShopCurrency.Zen)
        {
            return true;
        }

        return settings is not null
            && ShopCurrencies.TryGet(currency, out var descriptor)
            && descriptor is { } known
            && known.IsEnabled(settings);
    }

    /// <summary>
    /// Gets the currency mask for the <c>PlayerShopCurrenciesExtended</c> packet.
    /// Bit N set means the currency with value N may be used; bit 0 (Zen) is
    /// always set when the feature is active. A <c>null</c> configuration
    /// (feature deactivated) yields zero, which the client reads as deactivated.
    /// </summary>
    /// <param name="settings">The settings; <c>null</c> when the feature is deactivated.</param>
    /// <returns>The currency mask.</returns>
    public static uint GetCurrencyMask(MultiCurrencyPlayerShopConfiguration? settings)
    {
        if (settings is null)
        {
            return 0;
        }

        uint mask = 0;
        foreach (var currency in Enum.GetValues<PlayerShopCurrency>())
        {
            if (IsCurrencyAllowed(settings, currency))
            {
                mask |= 1u << (int)currency;
            }
        }

        return mask;
    }

    /// <inheritdoc />
    public object CreateDefaultConfig()
    {
        return new MultiCurrencyPlayerShopConfiguration();
    }
}
