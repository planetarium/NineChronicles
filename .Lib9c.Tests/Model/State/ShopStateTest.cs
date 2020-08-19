namespace Lib9c.Tests.Model.State
{
    using System;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class ShopStateTest
    {
        [Fact]
        public void Serialization()
        {
            var shopState = new ShopState();
            var agentAddress = new PrivateKey().ToAddress();
            var avatarAddress = new PrivateKey().ToAddress();
            var productId = Guid.NewGuid();
            var weaponRow = new EquipmentItemSheet.Row();
            weaponRow.Set(new[]
            {
                "10100000", "Weapon", "0", "Normal", "0", "ATK", "1", "2", "10100000",
            });
            var itemUsable = new Weapon(
                weaponRow,
                Guid.NewGuid(),
                0);
            var shopItem = new ShopItem(
                avatarAddress,
                productId,
                itemUsable,
                0
            );

            shopState.Register(agentAddress, shopItem);
            var serialized = (Dictionary)shopState.Serialize();
            shopState = new ShopState(serialized);

            Assert.Equal(1, shopState.AgentProducts.Count);
            Assert.True(shopState.AgentProducts.ContainsKey(agentAddress));
            Assert.Single(shopState.AgentProducts[agentAddress]);
            Assert.Contains(productId, shopState.AgentProducts[agentAddress]);
            Assert.Equal(1, shopState.Products.Count);
            Assert.Contains(productId, shopState.Products);
            Assert.Equal(shopItem, shopState.Products[productId]);
            Assert.Equal(1, shopState.ItemSubTypeProducts.Count);
            Assert.Contains(ItemSubType.Weapon, shopState.ItemSubTypeProducts);
            Assert.Single(shopState.ItemSubTypeProducts[ItemSubType.Weapon]);
            Assert.Contains(productId, shopState.ItemSubTypeProducts[ItemSubType.Weapon]);
        }

        [Fact]
        public void Register()
        {
            var shopState = new ShopState();
            var agentAddress = new PrivateKey().ToAddress();
            var avatarAddress = new PrivateKey().ToAddress();
            var productId = Guid.NewGuid();
            var weaponRow = new EquipmentItemSheet.Row();
            weaponRow.Set(new[]
            {
                "10100000", "Weapon", "0", "Normal", "0", "ATK", "1", "2", "10100000",
            });
            var itemUsable = new Weapon(
                weaponRow,
                Guid.NewGuid(),
                0);
            var shopItem = new ShopItem(
                avatarAddress,
                productId,
                itemUsable,
                0
            );

            shopState.Register(agentAddress, shopItem);

            Assert.Equal(1, shopState.AgentProducts.Count);
            Assert.True(shopState.AgentProducts.ContainsKey(agentAddress));
            Assert.Single(shopState.AgentProducts[agentAddress]);
            Assert.Contains(productId, shopState.AgentProducts[agentAddress]);
            Assert.Equal(1, shopState.Products.Count);
            Assert.Contains(productId, shopState.Products);
            Assert.Equal(shopItem, shopState.Products[productId]);
            Assert.Equal(1, shopState.ItemSubTypeProducts.Count);
            Assert.Contains(ItemSubType.Weapon, shopState.ItemSubTypeProducts);
            Assert.Single(shopState.ItemSubTypeProducts[ItemSubType.Weapon]);
            Assert.Contains(productId, shopState.ItemSubTypeProducts[ItemSubType.Weapon]);

            Assert.Throws<ShopStateAlreadyContainsException>(() =>
                shopState.Register(agentAddress, shopItem));
        }

        [Fact]
        public void Unregister()
        {
            var shopState = new ShopState();
            var agentAddress = new PrivateKey().ToAddress();
            var avatarAddress = new PrivateKey().ToAddress();
            var productId = Guid.NewGuid();
            var weaponRow = new EquipmentItemSheet.Row();
            weaponRow.Set(new[]
            {
                "10100000", "Weapon", "0", "Normal", "0", "ATK", "1", "2", "10100000",
            });
            var itemUsable = new Weapon(
                weaponRow,
                Guid.NewGuid(),
                0);
            var shopItem = new ShopItem(
                avatarAddress,
                productId,
                itemUsable,
                0
            );

            shopState.Register(agentAddress, shopItem);
            shopState.Unregister(agentAddress, shopItem);

            Assert.Equal(0, shopState.AgentProducts.Count);
            Assert.Equal(0, shopState.Products.Count);
            Assert.Equal(0, shopState.ItemSubTypeProducts.Count);

            Assert.Throws<FailedToUnregisterInShopStateException>(() =>
                shopState.Unregister(agentAddress, shopItem));
        }

        [Fact]
        public void TryUnregister()
        {
            var shopState = new ShopState();
            var agentAddress = new PrivateKey().ToAddress();
            var avatarAddress = new PrivateKey().ToAddress();
            var productId = Guid.NewGuid();
            var weaponRow = new EquipmentItemSheet.Row();
            weaponRow.Set(new[]
            {
                "10100000", "Weapon", "0", "Normal", "0", "ATK", "1", "2", "10100000",
            });
            var itemUsable = new Weapon(
                weaponRow,
                Guid.NewGuid(),
                0);
            var shopItem = new ShopItem(
                avatarAddress,
                productId,
                itemUsable,
                0
            );

            shopState.Register(agentAddress, shopItem);

            Assert.True(shopState.TryUnregister(
                agentAddress,
                shopItem.ProductId,
                out var unregisteredItem));
            Assert.Equal(shopItem, unregisteredItem);

            Assert.Equal(0, shopState.AgentProducts.Count);
            Assert.Equal(0, shopState.Products.Count);
            Assert.Equal(0, shopState.ItemSubTypeProducts.Count);

            Assert.Throws<FailedToUnregisterInShopStateException>(() =>
                shopState.Unregister(agentAddress, shopItem));
        }

        [Fact]
        public void TryGet()
        {
            var shopState = new ShopState();
            var agentAddress = new PrivateKey().ToAddress();
            var avatarAddress = new PrivateKey().ToAddress();
            var productId = Guid.NewGuid();
            var weaponRow = new EquipmentItemSheet.Row();
            weaponRow.Set(new[]
            {
                "10100000", "Weapon", "0", "Normal", "0", "ATK", "1", "2", "10100000",
            });
            var itemUsable = new Weapon(
                weaponRow,
                Guid.NewGuid(),
                0);
            var shopItem = new ShopItem(
                avatarAddress,
                productId,
                itemUsable,
                0
            );

            shopState.Register(agentAddress, shopItem);

            Assert.True(shopState.TryGet(agentAddress, shopItem.ProductId, out var outShopItem));
            Assert.Equal(shopItem, outShopItem);

            shopState.Unregister(agentAddress, shopItem);

            Assert.False(shopState.TryGet(agentAddress, shopItem.ProductId, out _));
        }
    }
}
