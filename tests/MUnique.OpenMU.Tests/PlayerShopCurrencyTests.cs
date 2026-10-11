// <copyright file="PlayerShopCurrencyTests.cs" company="MUnique">
// Licensed under the MIT License. See LICENSE file in the project root for full license information.
// </copyright>

namespace MUnique.OpenMU.Tests;

using MUnique.OpenMU.DataModel.Configuration;
using MUnique.OpenMU.DataModel.Configuration.Items;
using MUnique.OpenMU.DataModel.Entities;
using MUnique.OpenMU.GameLogic;
using MUnique.OpenMU.GameLogic.Attributes;
using MUnique.OpenMU.GameLogic.PlayerActions.PlayerStore;
using MUnique.OpenMU.GameLogic.PlugIns.ChatCommands;
using MUnique.OpenMU.GameServer.MessageHandler.Items;
using MUnique.OpenMU.GameServer.MessageHandler.PlayerShop;
using MUnique.OpenMU.PlugIns;
using Moq;

/// <summary>
/// Tests for the multi-currency personal shop.
/// </summary>
[TestFixture]
public class PlayerShopCurrencyTests
{
    /// <summary>
    /// Verifies the currency kind flags of <see cref="ShopPrice"/>.
    /// </summary>
    /// <param name="currency">The currency.</param>
    /// <param name="expectedZen">Whether Zen is expected.</param>
    /// <param name="expectedJewel">Whether jewel is expected.</param>
    /// <param name="expectedCoin">Whether account coin is expected.</param>
    /// <param name="expectedExtended">Whether an extended client is required.</param>
    [TestCase(PlayerShopCurrency.Zen, true, false, false, false)]
    [TestCase(PlayerShopCurrency.JewelOfBless, false, true, false, true)]
    [TestCase(PlayerShopCurrency.JewelOfSoul, false, true, false, true)]
    [TestCase(PlayerShopCurrency.JewelOfChaos, false, true, false, true)]
    [TestCase(PlayerShopCurrency.JewelOfCreation, false, true, false, true)]
    [TestCase(PlayerShopCurrency.JewelOfLife, false, true, false, true)]
    [TestCase(PlayerShopCurrency.WCoinC, false, false, true, true)]
    [TestCase(PlayerShopCurrency.WCoinP, false, false, true, true)]
    [TestCase(PlayerShopCurrency.GoblinPoints, false, false, true, true)]
    public void ShopPriceFlags(PlayerShopCurrency currency, bool expectedZen, bool expectedJewel, bool expectedCoin, bool expectedExtended)
    {
        var price = new ShopPrice(currency, 10);
        Assert.Multiple(() =>
        {
            Assert.That(price.IsZen, Is.EqualTo(expectedZen));
            Assert.That(price.IsJewel, Is.EqualTo(expectedJewel));
            Assert.That(price.IsAccountCoin, Is.EqualTo(expectedCoin));
            Assert.That(price.RequiresExtendedClient, Is.EqualTo(expectedExtended));
        });
    }

    /// <summary>
    /// Verifies that a missing amount means no price.
    /// </summary>
    [Test]
    public void FromItemWithoutAmountReturnsNull()
    {
        Assert.That(ShopPrice.FromItem(PlayerShopCurrency.Zen, null), Is.Null);
    }

    /// <summary>
    /// Verifies that a zero amount never makes an item free.
    /// </summary>
    [Test]
    public void FromItemWithZeroAmountReturnsNull()
    {
        Assert.That(ShopPrice.FromItem(PlayerShopCurrency.JewelOfBless, 0), Is.Null);
    }

    /// <summary>
    /// Verifies that the factory returns a Zen exchange for Zen.
    /// </summary>
    [Test]
    public void FactoryReturnsZenExchange()
    {
        Assert.That(ShopCurrencyExchangeFactory.GetExchange(PlayerShopCurrency.Zen), Is.InstanceOf<ZenShopCurrencyExchange>());
    }

    /// <summary>
    /// Verifies that the factory returns jewel exchanges for jewels.
    /// </summary>
    /// <param name="currency">The currency.</param>
    [TestCase(PlayerShopCurrency.JewelOfBless)]
    [TestCase(PlayerShopCurrency.JewelOfSoul)]
    [TestCase(PlayerShopCurrency.JewelOfChaos)]
    [TestCase(PlayerShopCurrency.JewelOfCreation)]
    [TestCase(PlayerShopCurrency.JewelOfLife)]
    public void FactoryReturnsJewelExchange(PlayerShopCurrency currency)
    {
        Assert.That(ShopCurrencyExchangeFactory.GetExchange(currency), Is.InstanceOf<JewelShopCurrencyExchange>());
    }

    /// <summary>
    /// Verifies that the factory returns coin exchanges for coins.
    /// </summary>
    /// <param name="currency">The currency.</param>
    [TestCase(PlayerShopCurrency.WCoinC)]
    [TestCase(PlayerShopCurrency.WCoinP)]
    [TestCase(PlayerShopCurrency.GoblinPoints)]
    public void FactoryReturnsCoinExchange(PlayerShopCurrency currency)
    {
        Assert.That(ShopCurrencyExchangeFactory.GetExchange(currency), Is.InstanceOf<AccountCoinShopCurrencyExchange>());
    }

    /// <summary>
    /// Verifies that Zen moves from buyer to seller.
    /// </summary>
    [Test]
    public async ValueTask ZenTransferMovesMoneyAsync()
    {
        var buyer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var seller = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        buyer.GameContext.Configuration.MaximumInventoryMoney = int.MaxValue;
        seller.GameContext.Configuration.MaximumInventoryMoney = int.MaxValue;
        buyer.Money = 1000;

        var exchange = new ZenShopCurrencyExchange();
        var payment = await exchange.TryTransferAsync(buyer, seller, 400).ConfigureAwait(false);
        Assert.That(payment.Result, Is.EqualTo(ShopPaymentResult.Success));
        Assert.Multiple(() =>
        {
            Assert.That(buyer.Money, Is.EqualTo(600));
            Assert.That(seller.Money, Is.EqualTo(400));
        });
    }

    /// <summary>
    /// Verifies that Zen transfer fails when the buyer lacks money, without changing balances.
    /// </summary>
    [Test]
    public async ValueTask ZenTransferFailsWithoutMoneyAsync()
    {
        var buyer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var seller = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        buyer.Money = 100;

        var exchange = new ZenShopCurrencyExchange();
        var payment = await exchange.TryTransferAsync(buyer, seller, 400).ConfigureAwait(false);
        Assert.That(payment.Result, Is.EqualTo(ShopPaymentResult.InsufficientFunds));
        Assert.Multiple(() =>
        {
            Assert.That(buyer.Money, Is.EqualTo(100));
            Assert.That(seller.Money, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that coin transfer fails closed when no account is attached.
    /// </summary>
    [Test]
    public async ValueTask CoinTransferFailsWithoutAccountAsync()
    {
        var buyer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var seller = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);

        var exchange = new AccountCoinShopCurrencyExchange(PlayerShopCurrency.GoblinPoints);
        Assert.That(exchange.CanCover(buyer, 10), Is.False);
        var payment = await exchange.TryTransferAsync(buyer, seller, 10).ConfigureAwait(false);
        Assert.That(payment.Result, Is.EqualTo(ShopPaymentResult.InsufficientFunds));
    }

    /// <summary>
    /// Verifies that the jewel balance counts only matching single jewels.
    /// </summary>
    [Test]
    public async ValueTask JewelBalanceCountsMatchingJewelsAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var bless = new ItemDefinition { Group = 14, Number = 13, Width = 1, Height = 1 };
        var soul = new ItemDefinition { Group = 14, Number = 14, Width = 1, Height = 1 };
        await player.Inventory!.AddItemAsync(12, CreateJewel(bless)).ConfigureAwait(false);
        await player.Inventory.AddItemAsync(13, CreateJewel(bless)).ConfigureAwait(false);
        await player.Inventory.AddItemAsync(14, CreateJewel(bless)).ConfigureAwait(false);
        await player.Inventory.AddItemAsync(15, CreateJewel(soul)).ConfigureAwait(false);

        var exchange = new JewelShopCurrencyExchange(PlayerShopCurrency.JewelOfBless);
        Assert.Multiple(() =>
        {
            Assert.That(exchange.CanCover(player, 3), Is.True);
            Assert.That(exchange.CanCover(player, 4), Is.False);
        });
    }

    /// <summary>
    /// Verifies that a jewel transfer fails without changing the inventory when jewels are lacking.
    /// </summary>
    [Test]
    public async ValueTask JewelTransferFailsWhenInsufficientAsync()
    {
        var buyer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var seller = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var bless = new ItemDefinition { Group = 14, Number = 13, Width = 1, Height = 1 };
        await buyer.Inventory!.AddItemAsync(12, CreateJewel(bless)).ConfigureAwait(false);

        var exchange = new JewelShopCurrencyExchange(PlayerShopCurrency.JewelOfBless);
        var insufficient = await exchange.TryTransferAsync(buyer, seller, 5).ConfigureAwait(false);
        Assert.Multiple(() =>
        {
            Assert.That(insufficient.Result, Is.EqualTo(ShopPaymentResult.InsufficientFunds));
            Assert.That(buyer.Inventory!.Items.Count(), Is.EqualTo(1));
        });
    }

    /// <summary>
    /// Verifies that a packed bundle pays when it is consumed entirely.
    /// The bundle row moves to the seller unchanged.
    /// </summary>
    [Test]
    public async ValueTask BundlePaysWhenConsumedEntirelyAsync()
    {
        var buyer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var seller = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var (_, bundle) = AddBlessMix(buyer);
        var bundleRow = CreateJewel(bundle, level: 0);
        await buyer.Inventory!.AddItemAsync(12, bundleRow).ConfigureAwait(false);

        var exchange = new JewelShopCurrencyExchange(PlayerShopCurrency.JewelOfBless);
        var payment = await exchange.TryTransferAsync(buyer, seller, 10).ConfigureAwait(false);
        Assert.Multiple(() =>
        {
            Assert.That(payment.Result, Is.EqualTo(ShopPaymentResult.Success));
            Assert.That(buyer.Inventory!.Items.Count(), Is.EqualTo(0));
            Assert.That(seller.Inventory!.Items.Count(), Is.EqualTo(1));
            Assert.That(seller.Inventory.Items.Contains(bundleRow), Is.True);
        });
    }

    /// <summary>
    /// Verifies that a packed bundle cannot be split: an incompatible price fails untouched.
    /// </summary>
    [Test]
    public async ValueTask BundleCannotBeSplitAsync()
    {
        var buyer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var seller = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var (_, bundle) = AddBlessMix(buyer);
        await buyer.Inventory!.AddItemAsync(12, CreateJewel(bundle, level: 2)).ConfigureAwait(false);

        var exchange = new JewelShopCurrencyExchange(PlayerShopCurrency.JewelOfBless);
        var payment = await exchange.TryTransferAsync(buyer, seller, 25).ConfigureAwait(false);
        Assert.Multiple(() =>
        {
            Assert.That(payment.Result, Is.EqualTo(ShopPaymentResult.InsufficientFunds));
            Assert.That(buyer.Inventory!.Items.Count(), Is.EqualTo(1));
            Assert.That(seller.Inventory!.Items.Count(), Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that whole bundles and singles combine into one exact payment.
    /// </summary>
    [Test]
    public async ValueTask BundleAndSinglesCombineExactlyAsync()
    {
        var buyer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var seller = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var (single, bundle) = AddBlessMix(buyer);
        await buyer.Inventory!.AddItemAsync(12, CreateJewel(bundle, level: 1)).ConfigureAwait(false);
        for (var slot = 13; slot < 18; slot++)
        {
            await buyer.Inventory.AddItemAsync((byte)slot, CreateJewel(single)).ConfigureAwait(false);
        }

        var exchange = new JewelShopCurrencyExchange(PlayerShopCurrency.JewelOfBless);
        var payment = await exchange.TryTransferAsync(buyer, seller, 25).ConfigureAwait(false);
        Assert.Multiple(() =>
        {
            Assert.That(payment.Result, Is.EqualTo(ShopPaymentResult.Success));
            Assert.That(buyer.Inventory!.Items.Count(), Is.EqualTo(0));
            Assert.That(seller.Inventory!.Items.Count(), Is.EqualTo(6));
        });
    }

    /// <summary>
    /// Verifies that Zen is refunded when the seller cannot receive it.
    /// </summary>
    [Test]
    public async ValueTask ZenRefundWhenSellerCannotReceiveAsync()
    {
        var buyer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var seller = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        buyer.GameContext.Configuration.MaximumInventoryMoney = int.MaxValue;
        seller.GameContext.Configuration.MaximumInventoryMoney = 0;
        buyer.Money = 1000;

        var exchange = new ZenShopCurrencyExchange();
        var payment = await exchange.TryTransferAsync(buyer, seller, 400).ConfigureAwait(false);
        Assert.Multiple(() =>
        {
            Assert.That(payment.Result, Is.EqualTo(ShopPaymentResult.ReceiverCannotHold));
            Assert.That(buyer.Money, Is.EqualTo(1000));
            Assert.That(seller.Money, Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that account coins move from buyer to seller.
    /// </summary>
    [Test]
    public async ValueTask CoinTransferMovesBalancesAsync()
    {
        var buyer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var seller = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        await buyer.SetAccountAsync(new Account { GoblinPoints = 100 }).ConfigureAwait(false);
        await seller.SetAccountAsync(new Account()).ConfigureAwait(false);

        var exchange = new AccountCoinShopCurrencyExchange(PlayerShopCurrency.GoblinPoints);
        var payment = await exchange.TryTransferAsync(buyer, seller, 40).ConfigureAwait(false);
        Assert.Multiple(() =>
        {
            Assert.That(payment.Result, Is.EqualTo(ShopPaymentResult.Success));
            Assert.That(buyer.Account!.GoblinPoints, Is.EqualTo(60));
            Assert.That(seller.Account!.GoblinPoints, Is.EqualTo(40));
        });
    }

    /// <summary>
    /// Verifies that a coin payment fails without changes when the seller balance would overflow.
    /// </summary>
    [Test]
    public async ValueTask CoinTransferFailsOnSellerOverflowAsync()
    {
        var buyer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var seller = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        await buyer.SetAccountAsync(new Account { GoblinPoints = 100 }).ConfigureAwait(false);
        await seller.SetAccountAsync(new Account { GoblinPoints = int.MaxValue - 10 }).ConfigureAwait(false);

        var exchange = new AccountCoinShopCurrencyExchange(PlayerShopCurrency.GoblinPoints);
        var payment = await exchange.TryTransferAsync(buyer, seller, 40).ConfigureAwait(false);
        Assert.Multiple(() =>
        {
            Assert.That(payment.Result, Is.EqualTo(ShopPaymentResult.ReceiverCannotHold));
            Assert.That(buyer.Account!.GoblinPoints, Is.EqualTo(100));
            Assert.That(seller.Account!.GoblinPoints, Is.EqualTo(int.MaxValue - 10));
        });
    }

    /// <summary>
    /// Verifies that the extended price handler silently rejects hostile packets.
    /// </summary>
    /// <param name="packet">The packet bytes.</param>
    [TestCase(new byte[] { 0xC3, 0x04, 0x3F })]
    [TestCase(new byte[] { 0xC3, 0x0A, 0x3F, 0x09, 204, 0xFF, 0x05, 0x00, 0x00, 0x00 })]
    public async ValueTask ExtendedPriceHandlerRejectsHostilePacketsAsync(byte[] packet)
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.Attributes![Stats.Level] = 10;
        var item = new Item { ItemSlot = 204 };
        player.SelectedCharacter!.Inventory!.Items.Add(item);

        await new PlayerShopSetItemPriceExtendedPacketHandlerPlugIn().HandlePacketAsync(player, new Memory<byte>(packet)).ConfigureAwait(false);

        Assert.That(item.StorePrice, Is.Null);
    }

    /// <summary>
    /// Verifies that a store with a disallowed currency price cannot be opened.
    /// </summary>
    [Test]
    public async ValueTask StoreWithDisallowedCurrencyCannotOpenAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var item = CreateJewel(new ItemDefinition { Group = 1, Number = 1, Width = 1, Height = 1 });
        item.ItemSlot = 204;
        item.StorePrice = 5;
        item.StorePriceCurrency = PlayerShopCurrency.JewelOfBless;
        await player.ShopStorage!.AddItemAsync(204, item).ConfigureAwait(false);

        await new OpenStoreAction().OpenStoreAsync(player, "shop").ConfigureAwait(false);

        Assert.That(player.ShopStorage.StoreOpen, Is.False);
    }

    /// <summary>
    /// Verifies that a Zen purchase moves money and the item, and clears the listing.
    /// </summary>
    [Test]
    public async ValueTask ZenPurchaseMovesMoneyAndItemAsync()
    {
        var (seller, listing) = await CreateSellerWithListingAsync(400, PlayerShopCurrency.Zen).ConfigureAwait(false);
        var buyer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        buyer.GameContext.Configuration.MaximumInventoryMoney = int.MaxValue;
        buyer.Money = 1000;

        await new BuyRequestAction().BuyItemAsync(buyer, seller, 204).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(buyer.Money, Is.EqualTo(600));
            Assert.That(seller.Money, Is.EqualTo(400));
            Assert.That(buyer.Inventory!.Items.Contains(listing), Is.True);
            Assert.That(listing.StorePrice, Is.Null);
            Assert.That(seller.ShopStorage!.GetItem(204), Is.Null);
        });
    }

    /// <summary>
    /// Verifies that a purchase in a disabled currency changes nothing.
    /// </summary>
    [Test]
    public async ValueTask DisallowedCurrencyPurchaseChangesNothingAsync()
    {
        var (seller, listing) = await CreateSellerWithListingAsync(5, PlayerShopCurrency.JewelOfBless).ConfigureAwait(false);
        var buyer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        buyer.GameContext.Configuration.MaximumInventoryMoney = int.MaxValue;
        buyer.Money = 1000;

        await new BuyRequestAction().BuyItemAsync(buyer, seller, 204).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(buyer.Money, Is.EqualTo(1000));
            Assert.That(buyer.Inventory!.Items.Count(), Is.EqualTo(0));
            Assert.That(seller.ShopStorage!.GetItem(204), Is.SameAs(listing));
        });
    }

    /// <summary>
    /// Verifies that a vanilla client cannot buy a non-Zen item.
    /// </summary>
    [Test]
    public async ValueTask VanillaBuyerCannotBuyJewelItemAsync()
    {
        var (seller, listing) = await CreateSellerWithListingAsync(5, PlayerShopCurrency.JewelOfBless).ConfigureAwait(false);
        var buyer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        buyer.GameContext.FeaturePlugIns.AddPlugIn(new MultiCurrencyPlayerShopFeaturePlugIn(), true);
        Assert.That(buyer.SupportsMultiCurrencyShop, Is.False);

        await new BuyRequestAction().BuyItemAsync(buyer, seller, 204).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(buyer.Inventory!.Items.Count(), Is.EqualTo(0));
            Assert.That(seller.ShopStorage!.GetItem(204), Is.SameAs(listing));
        });
    }

    /// <summary>
    /// Verifies that a purchase is refunded when the seller cannot receive the money.
    /// </summary>
    [Test]
    public async ValueTask PurchaseRefundedWhenSellerCannotReceiveAsync()
    {
        var (seller, listing) = await CreateSellerWithListingAsync(400, PlayerShopCurrency.Zen).ConfigureAwait(false);
        seller.GameContext.Configuration.MaximumInventoryMoney = 0;
        var buyer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        buyer.GameContext.Configuration.MaximumInventoryMoney = int.MaxValue;
        buyer.Money = 1000;

        await new BuyRequestAction().BuyItemAsync(buyer, seller, 204).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(buyer.Money, Is.EqualTo(1000));
            Assert.That(seller.Money, Is.EqualTo(0));
            Assert.That(seller.ShopStorage!.GetItem(204), Is.SameAs(listing));
        });
    }

    /// <summary>
    /// Verifies that many bundles are covered without combinatorial search.
    /// </summary>
    [Test]
    public async ValueTask ManyBundlesAreCoveredAsync()
    {
        var buyer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var seller = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var (_, bundle) = AddBlessMix(buyer);
        for (var slot = 0; slot < 25; slot++)
        {
            await buyer.Inventory!.AddItemAsync((byte)(12 + slot), CreateJewel(bundle, level: 0)).ConfigureAwait(false);
        }

        var exchange = new JewelShopCurrencyExchange(PlayerShopCurrency.JewelOfBless);
        var payment = await exchange.TryTransferAsync(buyer, seller, 250).ConfigureAwait(false);
        Assert.Multiple(() =>
        {
            Assert.That(payment.Result, Is.EqualTo(ShopPaymentResult.Success));
            Assert.That(buyer.Inventory!.Items.Count(), Is.EqualTo(0));
            Assert.That(seller.Inventory!.Items.Count(), Is.EqualTo(25));
        });
    }

    /// <summary>
    /// Verifies that the chat command accepts the store position of the item.
    /// </summary>
    [Test]
    public async ValueTask ShopPriceCommandAcceptsRelativeSlotAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.Attributes![Stats.Level] = 10;
        var item = new Item { ItemSlot = 204 };
        player.SelectedCharacter!.Inventory!.Items.Add(item);

        await new ShopPriceChatCommandPlugIn().HandleCommandAsync(player, "/shopprice 1 100").ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(item.StorePrice, Is.EqualTo(100));
            Assert.That(item.StorePriceCurrency, Is.EqualTo(PlayerShopCurrency.Zen));
        });
    }

    /// <summary>
    /// Verifies that the chat command still accepts absolute store slots.
    /// </summary>
    [Test]
    public async ValueTask ShopPriceCommandAcceptsAbsoluteSlotAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.Attributes![Stats.Level] = 10;
        var item = new Item { ItemSlot = 205 };
        player.SelectedCharacter!.Inventory!.Items.Add(item);

        await new ShopPriceChatCommandPlugIn().HandleCommandAsync(player, "/shopprice 205 100").ConfigureAwait(false);

        Assert.That(item.StorePrice, Is.EqualTo(100));
    }

    /// <summary>
    /// Verifies that the chat command without arguments lists instead of failing.
    /// </summary>
    [Test]
    public async ValueTask ShopPriceCommandWithoutArgumentsListsAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.Attributes![Stats.Level] = 10;
        var item = new Item { ItemSlot = 204 };
        player.SelectedCharacter!.Inventory!.Items.Add(item);

        await new ShopPriceChatCommandPlugIn().HandleCommandAsync(player, "/shopprice").ConfigureAwait(false);

        Assert.That(item.StorePrice, Is.Null);
    }

    /// <summary>
    /// Verifies that the chat command rejects slots outside the store.
    /// </summary>
    [Test]
    public async ValueTask ShopPriceCommandRejectsForeignSlotAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.Attributes![Stats.Level] = 10;
        var item = new Item { ItemSlot = 204 };
        player.SelectedCharacter!.Inventory!.Items.Add(item);

        await new ShopPriceChatCommandPlugIn().HandleCommandAsync(player, "/shopprice 99 100").ConfigureAwait(false);

        Assert.That(item.StorePrice, Is.Null);
    }

    /// <summary>
    /// Verifies that a broke buyer changes nothing.
    /// </summary>
    [Test]
    public async ValueTask BrokeBuyerChangesNothingAsync()
    {
        var (seller, listing) = await CreateSellerWithListingAsync(400, PlayerShopCurrency.Zen).ConfigureAwait(false);
        var buyer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        buyer.GameContext.Configuration.MaximumInventoryMoney = int.MaxValue;
        buyer.Money = 0;

        await new BuyRequestAction().BuyItemAsync(buyer, seller, 204).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(buyer.Money, Is.EqualTo(0));
            Assert.That(seller.Money, Is.EqualTo(0));
            Assert.That(seller.ShopStorage!.GetItem(204), Is.SameAs(listing));
        });
    }

    /// <summary>
    /// Verifies that only legitimate bundle levels resolve to piece counts.
    /// </summary>
    /// <param name="level">The bundle level.</param>
    /// <param name="expectedPieces">The expected piece count, if legitimate.</param>
    [TestCase(0, 10)]
    [TestCase(1, 20)]
    [TestCase(2, 30)]
    public void LegitimateBundleLevelsResolve(byte level, int expectedPieces)
    {
        Assert.Multiple(() =>
        {
            Assert.That(JewelBundleHelper.TryGetPieceCount(level, out var pieces), Is.True);
            Assert.That(pieces, Is.EqualTo(expectedPieces));
        });
    }

    /// <summary>
    /// Verifies that exotic bundle levels never resolve, so they pay nothing anywhere.
    /// </summary>
    /// <param name="level">The bundle level.</param>
    [TestCase(3)]
    [TestCase(25)]
    [TestCase(255)]
    public void ExoticBundleLevelsResolveToNothing(byte level)
    {
        Assert.That(JewelBundleHelper.TryGetPieceCount(level, out _), Is.False);
    }

    /// <summary>
    /// Verifies that a bundle with an exotic level cannot pay, even below its face value.
    /// </summary>
    [Test]
    public async ValueTask ExoticBundleCannotPayAsync()
    {
        var buyer = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var seller = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        var (_, bundle) = AddBlessMix(buyer);
        await buyer.Inventory!.AddItemAsync(12, CreateJewel(bundle, level: 5)).ConfigureAwait(false);

        var exchange = new JewelShopCurrencyExchange(PlayerShopCurrency.JewelOfBless);
        Assert.Multiple(() =>
        {
            Assert.That(exchange.CanCover(buyer, 10), Is.False);
            Assert.That(exchange.CanCover(buyer, 60), Is.False);
        });
        var payment = await exchange.TryTransferAsync(buyer, seller, 10).ConfigureAwait(false);
        Assert.Multiple(() =>
        {
            Assert.That(payment.Result, Is.EqualTo(ShopPaymentResult.InsufficientFunds));
            Assert.That(buyer.Inventory!.Items.Count(), Is.EqualTo(1));
            Assert.That(seller.Inventory!.Items.Count(), Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that the jewel mix handler quietly drops unknown operations.
    /// </summary>
    [Test]
    public async ValueTask JewelMixHandlerDropsUnknownOperationAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.GameContext.Configuration.MaximumInventoryMoney = int.MaxValue;
        player.Money = 1000;
        var packet = new byte[] { 0xC1, 0x07, 0xBC, 0xFF, 0x00, 0x00, 0x00 };

        await new JewelMixHandlerPlugIn().HandlePacketAsync(player, new Memory<byte>(packet)).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(player.Money, Is.EqualTo(1000));
            Assert.That(player.Inventory!.Items.Count(), Is.EqualTo(0));
        });
    }

    /// <summary>
    /// Verifies that a buyer with an open own shop still receives the bought item.
    /// </summary>
    [Test]
    public async ValueTask BuyerWithOpenShopReceivesItemAsync()
    {
        var (seller, listing) = await CreateSellerWithListingAsync(400, PlayerShopCurrency.Zen).ConfigureAwait(false);
        var (buyer, _) = await CreateSellerWithListingAsync(100, PlayerShopCurrency.Zen).ConfigureAwait(false);
        buyer.GameContext.Configuration.MaximumInventoryMoney = int.MaxValue;
        buyer.Money = 1000;

        await new BuyRequestAction().BuyItemAsync(buyer, seller, 204).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(buyer.Money, Is.EqualTo(600));
            Assert.That(seller.Money, Is.EqualTo(400));
            Assert.That(buyer.Inventory!.Items.Contains(listing), Is.True);
            Assert.That(listing.StorePrice, Is.Null);
            Assert.That(buyer.ShopStorage!.StoreOpen, Is.True);
        });
    }

    /// <summary>
    /// Verifies that a Zen price can be set while the feature is deactivated.
    /// </summary>
    [Test]
    public async ValueTask ZenPriceCanBeSetWhenFeatureInactiveAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.Attributes![Stats.Level] = 10;
        var item = new Item { ItemSlot = 204 };
        player.SelectedCharacter!.Inventory!.Items.Add(item);

        await new SetItemPriceAction().SetPriceAsync(player, 204, 1000).ConfigureAwait(false);

        Assert.Multiple(() =>
        {
            Assert.That(item.StorePrice, Is.EqualTo(1000));
            Assert.That(item.StorePriceCurrency, Is.EqualTo(PlayerShopCurrency.Zen));
        });
    }

    /// <summary>
    /// Verifies that a non-Zen price is rejected while the feature is deactivated.
    /// </summary>
    [Test]
    public async ValueTask JewelPriceIsRejectedWhenFeatureInactiveAsync()
    {
        var player = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        player.Attributes![Stats.Level] = 10;
        var item = new Item { ItemSlot = 204 };
        player.SelectedCharacter!.Inventory!.Items.Add(item);

        await new SetItemPriceAction().SetPriceAsync(player, 204, new ShopPrice(PlayerShopCurrency.JewelOfBless, 5)).ConfigureAwait(false);

        Assert.That(item.StorePrice, Is.Null);
    }

    /// <summary>
    /// Verifies that a deactivated feature yields a zero currency mask.
    /// </summary>
    [Test]
    public void CurrencyMaskIsZeroWhenFeatureDeactivated()
    {
        Assert.That(MultiCurrencyPlayerShopFeaturePlugIn.GetCurrencyMask(null), Is.EqualTo(0u));
    }

    /// <summary>
    /// Verifies that the default configuration enables all nine currencies in the mask.
    /// </summary>
    [Test]
    public void CurrencyMaskContainsAllCurrenciesByDefault()
    {
        const uint expected = (1u << 9) - 1;
        Assert.That(MultiCurrencyPlayerShopFeaturePlugIn.GetCurrencyMask(new MultiCurrencyPlayerShopConfiguration()), Is.EqualTo(expected));
    }

    /// <summary>
    /// Verifies that a disabled currency clears its bit in the mask.
    /// </summary>
    [Test]
    public void CurrencyMaskOmitsDisabledCurrency()
    {
        var configuration = new MultiCurrencyPlayerShopConfiguration { AllowWCoinP = false };
        var mask = MultiCurrencyPlayerShopFeaturePlugIn.GetCurrencyMask(configuration);
        Assert.Multiple(() =>
        {
            Assert.That(mask & (1u << (int)PlayerShopCurrency.WCoinP), Is.EqualTo(0u));
            Assert.That(mask & (1u << (int)PlayerShopCurrency.Zen), Is.Not.EqualTo(0u));
            Assert.That(mask & (1u << (int)PlayerShopCurrency.JewelOfBless), Is.Not.EqualTo(0u));
        });
    }

    /// <summary>
    /// Verifies that the mask bits always agree with <c>IsCurrencyAllowed</c>,
    /// so display and enforcement can't drift apart.
    /// </summary>
    [Test]
    public void CurrencyMaskMatchesAllowedCurrencies()
    {
        var configuration = new MultiCurrencyPlayerShopConfiguration { AllowWCoinP = false, AllowJewelOfSoul = false };
        var mask = MultiCurrencyPlayerShopFeaturePlugIn.GetCurrencyMask(configuration);
        foreach (var currency in Enum.GetValues<PlayerShopCurrency>())
        {
            var bit = (mask >> (int)currency) & 1u;
            Assert.That(bit == 1u, Is.EqualTo(MultiCurrencyPlayerShopFeaturePlugIn.IsCurrencyAllowed(configuration, currency)), $"Currency {currency}");
        }
    }

    /// <summary>
    /// Verifies that the feature configuration survives the JSON round-trip
    /// which the admin panel uses when saving it.
    /// </summary>
    [Test]
    public void ConfigurationSurvivesAdminPanelRoundTrip()
    {
        var stored = new PlugInConfiguration();
        var configuration = new MultiCurrencyPlayerShopConfiguration { AllowJewelOfBless = false, MaximumPriceAmount = 100 };
        stored.SetConfiguration(configuration, null);

        var loaded = stored.GetConfiguration<MultiCurrencyPlayerShopConfiguration>(null);
        Assert.Multiple(() =>
        {
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded!.AllowJewelOfBless, Is.False);
            Assert.That(loaded.AllowJewelOfSoul, Is.True);
            Assert.That(loaded.MaximumPriceAmount, Is.EqualTo(100));
        });
    }

    private static async ValueTask<(Player Seller, Item Listing)> CreateSellerWithListingAsync(int price, PlayerShopCurrency currency)
    {
        var seller = await PlayerTestHelper.CreatePlayerAsync().ConfigureAwait(false);
        seller.GameContext.Configuration.MaximumInventoryMoney = int.MaxValue;
        var listing = CreateJewel(new ItemDefinition { Group = 1, Number = 1, Width = 1, Height = 1 });
        listing.ItemSlot = 204;
        listing.StorePrice = price;
        listing.StorePriceCurrency = currency;
        await seller.ShopStorage!.AddItemAsync(204, listing).ConfigureAwait(false);
        seller.ShopStorage.StoreOpen = true;
        return (seller, listing);
    }

    private static (ItemDefinition Single, ItemDefinition Bundle) AddBlessMix(Player player)
    {
        var single = new ItemDefinition { Group = 14, Number = 13, Width = 1, Height = 1 };
        var bundle = new ItemDefinition { Group = 0x0C, Number = 30, Width = 1, Height = 1 };
        var mixes = new List<JewelMix> { new() { SingleJewel = single, MixedJewel = bundle } };
        Mock.Get(player.GameContext.Configuration).Setup(c => c.JewelMixes).Returns(mixes);
        return (single, bundle);
    }

    private static Item CreateJewel(ItemDefinition definition, byte level = 0)
    {
        var item = new Mock<Item>();
        item.SetupAllProperties();
        item.Setup(i => i.ItemOptions).Returns(new List<ItemOptionLink>());
        item.Setup(i => i.ItemSetGroups).Returns(new List<ItemOfItemSet>());
        item.Object.Definition = definition;
        item.Object.Durability = 1;
        item.Object.Level = level;
        return item.Object;
    }
}
