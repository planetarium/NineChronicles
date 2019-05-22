using System;
using System.Collections.Generic;
using Libplanet;
using Nekoyume.Game.Item;

namespace Nekoyume.State
{
    /// <summary>
    /// Shop의 상태 모델이다.
    /// 
    /// ---- 지금의 상점의 동기화 정책.
    /// `Sell` 액션에 대해서는 매번 직접 `Register`.
    /// `SellCancellation` 액션에 대해서도 매번 직접 `Unregister`.
    /// `Buy` 액션에 대해서도 매번 직접 `Unregister`.
    /// ShopAddress의 Shop 자체에 대한 동기화는 게임 실행 시 한 번.
    ///
    /// ---- 추후에 예정된 이슈.
    /// 상점의 아이템 수는 계속 증가할 것인데, 나중에는 전부를 동기화 하는 것이 무리라고 생각됨.
    /// 상점을 단일 상태로 관리하지 않고, 1000개나 10000개 정도를 갖고 있는 단위로 채널 처럼 관리하는 것이 좋겠음.
    /// 무작위로 접근해서 조회하도록.
    /// 단, 이때 각 아바타의 판매 목록을 불러오는 것에 문제가 생기니, 이 목록에 접근하는 방법을 아바타의 상태에 포함해야 함.
    /// </summary>
    [Serializable]
    public class ShopState
    {
        public readonly Dictionary<Address, List<ShopItem>> items = new Dictionary<Address, List<ShopItem>>();
        
        public static ShopItem Register(IDictionary<Address, List<ShopItem>> dictionary, Address address, ShopItem item)
        {
            if (!dictionary.ContainsKey(address))
            {
                dictionary.Add(address, new List<ShopItem>());
            }

            item.productId = Guid.NewGuid();
            dictionary[address].Add(item);
            return item;
        }
        
        public static KeyValuePair<Address, ShopItem> Find(IDictionary<Address, List<ShopItem>> dictionary, Address address, Guid productId)
        {
            if (!dictionary.ContainsKey(address))
            {
                throw new KeyNotFoundException($"address: {address}");
            }

            var list = dictionary[address];
            
            foreach (var shopItem in list)
            {
                if (shopItem.productId != productId) continue;
                
                return new KeyValuePair<Address, ShopItem>(address, shopItem);
            }

            throw new KeyNotFoundException($"productId: {productId}");
        }
        
        public static ShopItem Unregister(IDictionary<Address, List<ShopItem>> dictionary, Address address, Guid productId)
        {
            try
            {
                var pair = Find(dictionary, address, productId);
                dictionary[pair.Key].Remove(pair.Value);

                return pair.Value;
            }
            catch
            {
                throw new KeyNotFoundException($"address: {address}, productId: {productId}");
            }
        }
        
        public static bool Unregister(IDictionary<Address, List<ShopItem>> dictionary, Address address, ShopItem shopItem)
        {
            if (!dictionary[address].Contains(shopItem))
            {
                return false;
            }
            
            dictionary[address].Remove(shopItem);
            
            return true;
        }
        
        public ShopItem Register(Address address, ShopItem item)
        {
            return Register(items, address, item);
        }

        public KeyValuePair<Address, ShopItem> Find(Address address, Guid productId)
        {
            return Find(items, address, productId);
        }
        
        public ShopItem Unregister(Address address, Guid productId)
        {
            return Unregister(items, address, productId);
        }
        
        public bool Unregister(Address address, ShopItem shopItem)
        {
            return Unregister(items, address, shopItem);
        }
    }
}
