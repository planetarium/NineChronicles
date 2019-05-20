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
