using System;
using System.Collections.Generic;
using Libplanet;
using Nekoyume.Game.Item;
using UnityEditor;

namespace Nekoyume.Game
{
    [Serializable]
    public class Shop
    {
        public readonly Dictionary<byte[], List<ShopItem>> items = new Dictionary<byte[], List<ShopItem>>();
        
        public string Register(Address key, ShopItem item)
        {
            var addr = key.ToByteArray();
            if (!items.ContainsKey(addr))
            {
                items.Add(addr, new List<ShopItem>());
            }

            item.productId = new GUID().ToString();
            items[addr].Add(item);
            return item.productId;
        }

        public KeyValuePair<byte[], ShopItem> Unregister(string productId)
        {
            foreach (var pair in items)
            {
                foreach (var shopItem in pair.Value)
                {
                    if (shopItem.productId != productId) continue;
                    
                    return new KeyValuePair<byte[], ShopItem>(pair.Key, shopItem);
                }
            }

            throw new KeyNotFoundException("productId");
        }
    }
}
