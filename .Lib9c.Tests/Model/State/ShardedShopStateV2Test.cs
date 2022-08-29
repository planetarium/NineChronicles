namespace Lib9c.Tests.Model.State
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Lib9c.Model.Order;
    using Lib9c.Tests.Action;
    using Libplanet;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class ShardedShopStateV2Test
    {
        private readonly FungibleAssetValue _price;

        public ShardedShopStateV2Test()
        {
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _price = new FungibleAssetValue(Currency.Legacy("NCG", 2, null));
#pragma warning restore CS0618
        }

        [Theory]
        [InlineData(ItemSubType.Armor, "0", "b24b92E40c9f0c1f593c0bc7E0389320ad8aBA75")]
        [InlineData(ItemSubType.Armor, "a", "11BC751070aCEA35F2c424E7A9b41f6E8EE4cF35")]
        [InlineData(ItemSubType.Armor, "A", "11BC751070aCEA35F2c424E7A9b41f6E8EE4cF35")]
        [InlineData(ItemSubType.FullCostume, "9", "006681cA9BD83Bb35cb6675f2cbAE7bD1Fb15F4D")]
        public void DeriveAddress(ItemSubType itemSubType, string nonce, string addressHex)
        {
            Guid guid = new Guid($"{nonce}9168C5E-CEB2-4faa-B6BF-329BF39FA1E4");
            Address expectedAddress = new Address(addressHex);
            Assert.Equal(expectedAddress, ShardedShopStateV2.DeriveAddress(itemSubType, guid));
        }

        [Theory]
        [InlineData(ItemSubType.EquipmentMaterial)]
        [InlineData(ItemSubType.FoodMaterial)]
        [InlineData(ItemSubType.MonsterPart)]
        [InlineData(ItemSubType.NormalMaterial)]
        public void DeriveAddress_Throw_InvalidItemTypeException(ItemSubType itemSubType)
        {
            Assert.Throws<InvalidItemTypeException>(() => ShardedShopStateV2.DeriveAddress(itemSubType, Guid.NewGuid()));
        }

        [Fact]
        public void Add()
        {
            var orderId = new Guid("F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4");
            var orderId2 = new Guid("936DA01F-9ABD-4d9d-80C7-02AF85C822A8");

            ShardedShopStateV2 shardedShopState = new ShardedShopStateV2(default(Address));
            Assert.Empty(shardedShopState.OrderDigestList);

            var orderDigest = new OrderDigest(
                default,
                1,
                2,
                orderId,
                Guid.NewGuid(),
                _price,
                0,
                0,
                1,
                1
            );
            shardedShopState.Add(orderDigest, 0);
            Assert.Single(shardedShopState.OrderDigestList);

            Assert.Throws<DuplicateOrderIdException>(() => shardedShopState.Add(orderDigest, 0));

            var orderDigest2 = new OrderDigest(
                default,
                2,
                3,
                orderId2,
                Guid.NewGuid(),
                _price,
                0,
                0,
                2,
                1
            );
            shardedShopState.Add(orderDigest2, 3);
            Assert.Single(shardedShopState.OrderDigestList);
            Assert.Equal(orderDigest2, shardedShopState.OrderDigestList.First());
        }

        [Fact]
        public void Serialize()
        {
            ShardedShopStateV2 shardedShopState = new ShardedShopStateV2(default(Address));
            for (int i = 0; i < 4; i++)
            {
                var orderDigest = new OrderDigest(
                    default,
                    i,
                    i + 1,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    _price,
                    0,
                    0,
                    i,
                    1
                );
                shardedShopState.Add(orderDigest, 0);
            }

            Assert.NotEmpty(shardedShopState.OrderDigestList);

            Dictionary serialized = (Dictionary)shardedShopState.Serialize();
            ShardedShopStateV2 deserialized = new ShardedShopStateV2(serialized);
            Assert.NotEmpty(deserialized.OrderDigestList);
            Assert.Equal(shardedShopState.address, deserialized.address);
            Assert.Equal(shardedShopState.OrderDigestList.First(), deserialized.OrderDigestList.First());
        }

        [Fact]
        public void Serialize_DotNet_Api()
        {
            ShardedShopStateV2 shardedShopState = new ShardedShopStateV2(default(Address));
            for (int i = 0; i < 4; i++)
            {
                var orderDigest = new OrderDigest(
                    default,
                    i,
                    i + 1,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    _price,
                    0,
                    0,
                    i,
                    1
                );
                shardedShopState.Add(orderDigest, 0);
            }

            Assert.NotEmpty(shardedShopState.OrderDigestList);

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, shardedShopState);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (ShardedShopStateV2)formatter.Deserialize(ms);

            Assert.Equal(shardedShopState.Serialize(), deserialized.Serialize());
        }

        [Fact]
        public void Remove()
        {
            var tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            var avatarState = new AvatarState(
                default,
                default,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
            var random = new TestRandom();
            var item = (Weapon)ItemFactory.CreateItem(
                tableSheets.ItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Weapon),
                random);
            var item2 = (Weapon)ItemFactory.CreateItem(
                tableSheets.ItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Weapon),
                random);
            avatarState.inventory.AddItem2(item);
            avatarState.inventory.AddItem2(item2);

            var orderId = new Guid("F9168C5E-CEB2-4faa-B6BF-329BF39FA1E4");
            var orderId2 = new Guid("936DA01F-9ABD-4d9d-80C7-02AF85C822A8");

            ShardedShopStateV2 shardedShopState = new ShardedShopStateV2(default(Address));
            Assert.Empty(shardedShopState.OrderDigestList);

            var order = OrderFactory.Create(
                default,
                default,
                orderId,
                _price,
                item.TradableId,
                1,
                ItemSubType.Weapon,
                1
            );
            var order2 = OrderFactory.Create(
                default,
                default,
                orderId2,
                _price,
                item2.TradableId,
                1,
                ItemSubType.Weapon,
                1
            );
            var orderDigest = order.Digest2(avatarState, new CostumeStatSheet());
            var orderDigest2 = order2.Digest2(avatarState, new CostumeStatSheet());
            shardedShopState.Add(orderDigest, 0);
            shardedShopState.Add(orderDigest2, 0);
            Assert.Equal(2, shardedShopState.OrderDigestList.Count);

            shardedShopState.Remove(order, 0);
            Assert.Single(shardedShopState.OrderDigestList);
            Assert.Equal(order2.OrderId, shardedShopState.OrderDigestList.First().OrderId);

            Assert.Throws<OrderIdDoesNotExistException>(() => shardedShopState.Remove(order, 0));

            shardedShopState.Add(orderDigest, 1);
            shardedShopState.Remove(order2, order.ExpiredBlockIndex + 1);
            Assert.Empty(shardedShopState.OrderDigestList);
        }
    }
}
