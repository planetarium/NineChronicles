using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Game
{
    public class ShopProducts
    {
        public readonly Dictionary<Address, List<ShopItem>> Products = new Dictionary<Address, List<ShopItem>>();

        private readonly List<ItemSubType> _itemSubTypes = new List<ItemSubType>()
        {
            ItemSubType.Weapon,
            ItemSubType.Armor,
            ItemSubType.Belt,
            ItemSubType.Necklace,
            ItemSubType.Ring,
            ItemSubType.Food,
            ItemSubType.FullCostume,
            ItemSubType.HairCostume,
            ItemSubType.EarCostume,
            ItemSubType.EyeCostume,
            ItemSubType.TailCostume,
            ItemSubType.Title,
            ItemSubType.Hourglass,
            ItemSubType.ApStone,
        };

        private readonly List<ItemSubType> _shardedSubTypes = new List<ItemSubType>()
        {
            ItemSubType.Weapon,
            ItemSubType.Armor,
            ItemSubType.Belt,
            ItemSubType.Necklace,
            ItemSubType.Ring,
            ItemSubType.Food,
            ItemSubType.Hourglass,
            ItemSubType.ApStone,
        };

        public ShopProducts()
        {
            UpdateProducts();
        }

        public void UpdateProducts()
        {
            Products.Clear();
            foreach (var itemSubType in _itemSubTypes)
            {
                if (_shardedSubTypes.Contains(itemSubType))
                {
                    foreach (var addressKey in ShardedShopState.AddressKeys)
                    {
                        var address = ShardedShopState.DeriveAddress(itemSubType, addressKey);
                        AddProduct(address);
                    }
                }
                else
                {
                    var address = ShardedShopState.DeriveAddress(itemSubType, string.Empty);
                    AddProduct(address);
                }
            }
        }

        private void AddProduct(Address address)
        {
            var shardedShopState = Game.instance.Agent.GetState(address);
            if (shardedShopState is Dictionary dictionary)
            {
                var state = new ShardedShopState(dictionary);
                foreach (var product in state.Products.Values)
                {
                    if (product.SellerAgentAddress == Game.instance.Agent.Address &&
                        product.ExpiredBlockIndex != 0 &&
                        product.ExpiredBlockIndex > Game.instance.Agent.BlockIndex)
                    {
                        var agentAddress = product.SellerAgentAddress;
                        if (!Products.ContainsKey(agentAddress))
                        {
                            Products.Add(agentAddress, new List<ShopItem>());
                        }

                        Products[agentAddress].Add(product);
                    }
                }
            }
        }
    }
}
