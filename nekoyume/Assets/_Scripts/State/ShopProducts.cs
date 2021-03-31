using System.Collections.Generic;
using Libplanet;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.UI.Module;

namespace Nekoyume.State
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
                foreach (var addressKey in ShardedShopState.AddressKeys)
                {
                    var address = ShardedShopState.DeriveAddress(itemSubType, addressKey);
                    var state = new ShardedShopState(Game.Game.instance.Agent.GetState(address));

                    foreach (var product in state.Products.Values)
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
