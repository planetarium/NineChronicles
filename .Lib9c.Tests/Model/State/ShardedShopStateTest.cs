namespace Lib9c.Tests.Model.State
{
    using System;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Xunit;

    public class ShardedShopStateTest
    {
        private readonly TableSheets _tableSheets;
        private readonly FungibleAssetValue _price;

        public ShardedShopStateTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _price = new FungibleAssetValue(Currency.Legacy("NCG", 2, null));
#pragma warning restore CS0618
        }

        [Theory]
        [InlineData(ItemSubType.Armor, "0", "fF6496E36fcbfCD0EE5938ABFaf166454f100C4B")]
        [InlineData(ItemSubType.Armor, "a", "fA14f9c48b15d32633f8A297EDc765442bD397e4")]
        [InlineData(ItemSubType.Armor, "A", "fA14f9c48b15d32633f8A297EDc765442bD397e4")]
        [InlineData(ItemSubType.FullCostume, "9", "2d977CC12057F55173e50847c8D397b92eFf9d9c")]
        public void DeriveAddress(ItemSubType itemSubType, string nonce, string addressHex)
        {
            Guid guid = new Guid($"{nonce}9168C5E-CEB2-4faa-B6BF-329BF39FA1E4");
            Address expectedAddress = new Address(addressHex);
            Assert.Equal(expectedAddress, ShardedShopState.DeriveAddress(itemSubType, guid));
        }

        [Theory]
        [InlineData(ItemSubType.EquipmentMaterial)]
        [InlineData(ItemSubType.FoodMaterial)]
        [InlineData(ItemSubType.MonsterPart)]
        [InlineData(ItemSubType.NormalMaterial)]
        public void DeriveAddress_Throw_InvalidItemTypeException(ItemSubType itemSubType)
        {
            Assert.Throws<InvalidItemTypeException>(() => ShardedShopState.DeriveAddress(itemSubType, Guid.NewGuid()));
        }

        [Fact]
        public void Register()
        {
            ShardedShopState shardedShopState = new ShardedShopState(default(Address));
            Assert.Empty(shardedShopState.Products);

            Guid itemId = Guid.NewGuid();
            var itemUsable = new Weapon(
                _tableSheets.EquipmentItemSheet.First,
                itemId,
                0);
            ShopItem shopItem = new ShopItem(
                default,
                default,
                default,
                _price,
                itemUsable);
            shardedShopState.Register(shopItem);
            Assert.Single(shardedShopState.Products);
        }

        [Fact]
        public void Serialize()
        {
            ShardedShopState shardedShopState = new ShardedShopState(default(Address));
            for (int i = 0; i < 4; i++)
            {
                var itemUsable = new Weapon(
                    _tableSheets.EquipmentItemSheet.First,
                    Guid.NewGuid(),
                    0);
                ShopItem shopItem = new ShopItem(
                    default,
                    default,
                    Guid.NewGuid(),
                    _price,
                    itemUsable);
                shardedShopState.Register(shopItem);
            }

            Assert.NotEmpty(shardedShopState.Products);

            Dictionary serialized = (Dictionary)shardedShopState.Serialize();
            ShardedShopState deserialized = new ShardedShopState(serialized);
            Assert.NotEmpty(deserialized.Products);
            Assert.Equal(shardedShopState.address, deserialized.address);
            Assert.Equal(shardedShopState.Products.First(), deserialized.Products.First());
        }
    }
}
