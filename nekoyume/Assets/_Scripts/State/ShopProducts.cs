using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.Model.Order;
using Libplanet;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using BxDictionary = Bencodex.Types.Dictionary;

namespace Nekoyume.State
{
    public class ShopProducts
    {
        public readonly Dictionary<Address, List<ShopItem>> Products = new Dictionary<Address, List<ShopItem>>();
        public readonly List<OrderDigest> ProductsV2 = new List<OrderDigest>();

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
                        
                        break;
                    }
                }
                else
                {
                    var address = ShardedShopState.DeriveAddress(itemSubType, string.Empty);
                    AddProduct(address);
                }
                
                break;
            }
        }
        
        public void UpdateProductsV2()
        {
            ProductsV2.Clear();
            foreach (var itemSubType in _itemSubTypes)
            {
                if (_shardedSubTypes.Contains(itemSubType))
                {
                    foreach (var addressKey in ShardedShopState.AddressKeys)
                    {
                        var address = ShardedShopStateV2.DeriveAddress(itemSubType, addressKey);
                        AddProductV2(address);
                        
                        break;
                    }
                }
                else
                {
                    var address = ShardedShopStateV2.DeriveAddress(itemSubType, string.Empty);
                    AddProductV2(address);
                }

                break;
            }
        }

        private void AddProduct(Address address)
        {
            var shardedShopState = Game.Game.instance.Agent.GetState(address);
            if (shardedShopState is Dictionary dictionary)
            {
                var state = new ShardedShopState(dictionary);
                foreach (var product in state.Products.Values)
                {
                    if (product.ExpiredBlockIndex != 0 && product.ExpiredBlockIndex > Game.Game.instance.Agent.BlockIndex)
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
        
        private void AddProductV2(Address address)
        {
            var shardedShopValue = Game.Game.instance.Agent.GetState(address);
            if (!(shardedShopValue is BxDictionary serialized))
            {
                return;
            }
            
            var state = new ShardedShopStateV2(serialized);
            foreach (var orderDigest in state.OrderDigestList.Where(orderDigest =>
                orderDigest.ExpiredBlockIndex != 0 &&
                orderDigest.ExpiredBlockIndex > Game.Game.instance.Agent.BlockIndex))
            {
                ProductsV2.Add(orderDigest);
            }
        }
    }
}
