---
title: Personal shop
sidebar_label: Personal shop
sidebar_position: 5
description: Selling items to other players from a personal shop, optionally priced in jewels or account coins.
---

# Personal shop

Every player can open a personal shop and sell items to other players for
**Zen**. When the **Personal shop with multiple currencies** plugin is active,
prices can additionally be expressed in **Jewel of Bless, Soul, Chaos, Creation
and Life**, as well as in **WCoin (C), WCoin (P) and Goblin Points**.

Non-Zen prices require the extended client (MuMain). The original Season 6
client only sees and trades Zen-priced items; items with other currencies are
hidden from it and cannot be bought with it.

## Settings

The plugin **Personal shop with multiple currencies** on the
[Plugins](../admin-panel/plugins.md) page is disabled by default. Activating it
enables non-Zen prices; deactivating it returns shops to Zen only. Its
configuration holds one switch per currency and the maximum amounts:

| Setting | Default | Meaning |
|---|---|---|
| Allow Jewel Of Bless | yes | Prices in Jewel of Bless are allowed. |
| Allow Jewel Of Soul | yes | Prices in Jewel of Soul are allowed. |
| Allow Jewel Of Chaos | yes | Prices in Jewel of Chaos are allowed. |
| Allow Jewel Of Creation | yes | Prices in Jewel of Creation are allowed. |
| Allow Jewel Of Life | yes | Prices in Jewel of Life are allowed. |
| Allow WCoin C | yes | Prices in WCoin (C) are allowed. |
| Allow WCoin P | yes | Prices in WCoin (P) are allowed. |
| Allow Goblin Points | yes | Prices in Goblin Points are allowed. |
| Maximum Price Amount | 999999999 | Maximum price amount in any currency. |
| Maximum Jewel Amount | 999 | Maximum price amount for jewel currencies. |
| Maximum Coin Amount | 999999999 | Maximum price amount for account coin currencies. |

Disabling the plugin — or a single currency — while a shop is open keeps the
shop open, but items priced in a disabled currency can no longer be bought
until they are repriced. A store with such items cannot be (re-)opened.

## Pricing items
With the extended client, the shop dialog offers the enabled currencies when a
price is set. With any client, the chat command sets the price as well:

`/shopprice [slot] [amount] [currency]`

Without arguments, it lists the store items with their numbers. The slot is
the store position of the item (1–32, as shown by the item's position in the
shop window). The currency is one of `zen` (default), `bless`, `soul`,
`chaos`, `creation`, `life`, `wcoinc`, `wcoinp` or `goblin`; an amount of `0`
removes the price. For example, `/shopprice 3 10 bless` prices the third
store item at 10 Jewel of Bless.

Jewel prices are paid with single jewels from the buyer's inventory. Packed
jewel bundles count as well, but only when the purchase consumes them entirely
(a 10-bundle pays a price of 10, never 5 of it); otherwise the bundle has to be
unstacked first.
