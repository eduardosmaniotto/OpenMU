# C1 3F 0A - PlayerShopCurrenciesExtended (by server)

## Is sent when

After the player opened his own personal shop or requested the shop of another player, if the client supports extended shop packets.

## Causes the following actions on the client side

The client only offers the enabled currencies when the player sets a price or browses prices. A mask of zero means the feature is deactivated and only Zen can be used. The server still validates every price and purchase against its configuration.

## Structure

| Index | Length | Data Type | Value | Description |
|-------|--------|-----------|-------|-------------|
| 0 | 1 |   Byte   | 0xC1  | [Packet type](PacketTypes.md) |
| 1 | 1 |    Byte   |   8   | Packet header - length of the packet |
| 2 | 1 |    Byte   | 0x3F  | Packet header - packet type identifier |
| 3 | 1 |    Byte   | 0x0A  | Packet header - sub packet type identifier |
| 4 | 4 | IntegerLittleEndian |  | CurrencyMask; One bit per shop currency value; bit N set means the currency with value N may be used for prices. Bit 0 (Zen) is always set when the feature is enabled, a zero mask means it is deactivated. Additional bits are reserved for other features which price things in shop currencies. |