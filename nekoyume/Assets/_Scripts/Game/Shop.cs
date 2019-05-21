using System;
using System.Collections.Generic;
using Libplanet;
using Nekoyume.Game.Item;

namespace Nekoyume.Game
{
    /// <summary>
    /// 지금의 상점의 동기화 정책.
    /// `Sell` 액션에 대해서는 매번 직접 `Register`.
    /// `Buy` 액션에 대해서도 매번 직접 `Unregister`.
    /// ShopAddress의 Shop 자체에 대한 동기화는 게임 실행 시 한 번.
    /// </summary>
    [Serializable]
    public class Shop
    {
        public readonly Dictionary<string, List<ShopItem>> items = new Dictionary<string, List<ShopItem>>();
        
        public ShopItem Register(Address address, ShopItem item)
        {
            var key = address.ToString();
            if (!items.ContainsKey(key))
            {
                items.Add(key, new List<ShopItem>());
            }

            item.productId = Guid.NewGuid();
            items[key].Add(item);
            return item;
        }

        public KeyValuePair<string, ShopItem> Find(string address, Guid productId)
        {
            if (!items.ContainsKey(address))
            {
                throw new KeyNotFoundException($"address: {address}");
            }

            var list = items[address];
            
            foreach (var shopItem in list)
            {
                if (shopItem.productId != productId) continue;
                
                return new KeyValuePair<string, ShopItem>(address, shopItem);
            }

            throw new KeyNotFoundException($"productId: {productId}");
        }
        
        public ShopItem Unregister(string address, Guid productId)
        {
            try
            {
                var pair = Find(address, productId);
                items[pair.Key].Remove(pair.Value);

                return pair.Value;
            }
            catch
            {
                throw new KeyNotFoundException($"address: {address}, productId: {productId}");
            }
        }
        
        public bool Unregister(string address, ShopItem shopItem)
        {
            if (!items[address].Contains(shopItem))
            {
                return false;
            }
            
            items[address].Remove(shopItem);
            
            return true;
        }
    }
}
