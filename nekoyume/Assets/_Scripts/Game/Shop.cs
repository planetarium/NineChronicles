using System;
using System.Collections.Generic;
using Libplanet;
using Nekoyume.Game.Item;

namespace Nekoyume.Game
{
    [Serializable]
    public class Shop
    {
        public readonly Dictionary<string, List<ShopItem>> items = new Dictionary<string, List<ShopItem>>();
        
        public Guid Register(Address key, ShopItem item)
        {
            var addr = key.ToString();
            if (!items.ContainsKey(addr))
            {
                items.Add(addr, new List<ShopItem>());
            }

            item.productId = Guid.NewGuid();
            items[addr].Add(item);
            return item.productId;
        }

        public KeyValuePair<string, ShopItem> Unregister(Guid productId)
        {
            foreach (var pair in items)
            {
                foreach (var shopItem in pair.Value)
                {
                    if (shopItem.productId != productId) continue;
                    
                    return new KeyValuePair<string, ShopItem>(pair.Key, shopItem);
                }
            }

            throw new KeyNotFoundException("productId");
        }
    }
}
