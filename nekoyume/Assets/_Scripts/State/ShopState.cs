using System;
using System.Collections.Generic;
using Libplanet;
using Nekoyume.Game.Item;

namespace Nekoyume.State
{
    /// <summary>
    /// 지금의 상점의 동기화 정책.
    /// `Sell` 액션에 대해서는 매번 직접 `Register`.
    /// `SellCancellation` 액션에 대해서도 매번 직접 `Unregister`.
    /// `Buy` 액션에 대해서도 매번 직접 `Unregister`.
    /// ShopAddress의 Shop 자체에 대한 동기화는 게임 실행 시 한 번.
    /// </summary>
    [Serializable]
    public class ShopState
    {
        public readonly Dictionary<Address, List<ShopItem>> items = new Dictionary<Address, List<ShopItem>>();
        
        public ShopItem Register(Address address, ShopItem item)
        {
            if (!items.ContainsKey(address))
            {
                items.Add(address, new List<ShopItem>());
            }

            item.productId = Guid.NewGuid();
            items[address].Add(item);
            return item;
        }

        public KeyValuePair<Address, ShopItem> Find(Address address, Guid productId)
        {
            if (!items.ContainsKey(address))
            {
                throw new KeyNotFoundException($"address: {address}");
            }

            var list = items[address];
            
            foreach (var shopItem in list)
            {
                if (shopItem.productId != productId) continue;
                
                return new KeyValuePair<Address, ShopItem>(address, shopItem);
            }

            throw new KeyNotFoundException($"productId: {productId}");
        }
        
        public ShopItem Unregister(Address address, Guid productId)
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
        
        public bool Unregister(Address address, ShopItem shopItem)
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
