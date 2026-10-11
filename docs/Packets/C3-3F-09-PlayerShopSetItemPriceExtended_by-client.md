# C3 3F 09 - PlayerShopSetItemPriceExtended (by client)

## Is sent when

The player wants to set a price of an item in his personal item shop, expressed in a specific currency (Zen, jewels, or account coins). Only sent by the extended client.

## Causes the following actions on the server side

The price and currency are set for the specified item. Works only if the shop is currently closed and the multi-currency shop feature is active.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC3  | [Packet type](PacketTypes.md) |
| 1 | 1 |    Byte   |   10   | Packet header - length of the packet |
| 2 | 1 |    Byte   | 0x3F  | Packet header - packet type identifier |
| 3 | 1 |    Byte   | 0x09  | Packet header - sub packet type identifier |
| 4 | 1 | Byte |  | ItemSlot |
| 5 | 1 | ShopCurrencyType |  | Currency; The currency of the price. The values match the shop currencies known by the server. |
| 6 | 4 | IntegerLittleEndian |  | Price |

### ShopCurrencyType Enum

The currency of a personal shop price. The values match the shop currencies known by the server.

| Value | Name | Description |
|-------|------|-------------|
| 0 | Zen | Zen (game money). |
| 1 | JewelOfBless | Jewel of Bless. |
| 2 | JewelOfSoul | Jewel of Soul. |
| 3 | JewelOfChaos | Jewel of Chaos. |
| 4 | JewelOfCreation | Jewel of Creation. |
| 5 | JewelOfLife | Jewel of Life. |
| 6 | WCoinC | WCoin (C) account balance. |
| 7 | WCoinP | WCoin (P) account balance. |
| 8 | GoblinPoints | Goblin Points account balance. |