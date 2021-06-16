using System.Collections.Generic;
using Bencodex.Types;
using Lib9c.Model.Order;
using Libplanet;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using static Lib9c.SerializeKeys;

namespace Nekoyume.State
{
    public class ShopProducts
    {
        public readonly Dictionary<Address, List<ShopItem>> Products = new Dictionary<Address, List<ShopItem>>();

        public readonly Dictionary<Address, List<OrderDigest>> OrderDigests =
            new Dictionary<Address, List<OrderDigest>>();

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
            OrderDigests.Clear();
            foreach (var itemSubType in _itemSubTypes)
            {
                if (_shardedSubTypes.Contains(itemSubType))
                {
                    foreach (var addressKey in ShardedShopState.AddressKeys)
                    {
                        var address = ShardedShopStateV2.DeriveAddress(itemSubType, addressKey);
                        AddProduct(address);
                    }
                }
                else
                {
                    var address = ShardedShopStateV2.DeriveAddress(itemSubType, string.Empty);
                    AddProduct(address);
                }
            }
        }

        private void AddProduct(Address address)
        {
            var shardedShopState = Game.Game.instance.Agent.GetState(address);
            if (shardedShopState is Dictionary dictionary)
            {
                var state = new ShardedShopStateV2(dictionary);
                // foreach (var product in state.Products.Values)
                // {
                //     if (product.ExpiredBlockIndex != 0 && product.ExpiredBlockIndex > Game.Game.instance.Agent.BlockIndex)
                //     {
                //         var agentAddress = product.SellerAgentAddress;
                //         if (!Products.ContainsKey(agentAddress))
                //         {
                //             Products.Add(agentAddress, new List<ShopItem>());
                //         }
                //
                //         Products[agentAddress].Add(product);
                //     }
                // }
                foreach (var orderDigest in state.OrderDigestList)
                {
                    if (orderDigest.ExpiredBlockIndex != 0 && orderDigest.ExpiredBlockIndex > Game.Game.instance.Agent.BlockIndex)
                    {
                        var agentAddress = orderDigest.SellerAgentAddress;
                        if (!OrderDigests.ContainsKey(agentAddress))
                        {
                            OrderDigests.Add(agentAddress, new List<OrderDigest>());
                        }
                
                        OrderDigests[agentAddress].Add(orderDigest);
                    }
                }
            }
        }
    }
}
