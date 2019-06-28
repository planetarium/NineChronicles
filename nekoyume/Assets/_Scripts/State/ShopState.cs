using System;
using System.Collections.Generic;
using System.Linq;
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
    public class ShopState : State
    {
        public static readonly Address Address = new Address(new byte[]
            {
                0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0
            }
        );

        public readonly Dictionary<Address, List<ShopItem>> items = new Dictionary<Address, List<ShopItem>>();

        public ShopState() : base(Address)
        {
        }
        
        public ShopItem Register(Address sellerAvatarAddress, ShopItem item)
        {
            if (!items.ContainsKey(sellerAvatarAddress))
            {
                items.Add(sellerAvatarAddress, new List<ShopItem>());
            }

            items[sellerAvatarAddress].Add(item);
            return item;
        }

        public bool Unregister(Address sellerAvatarAddress,
            ShopItem shopItem)
        {
            if (!items[sellerAvatarAddress].Contains(shopItem))
            {
                return false;
            }

            items[sellerAvatarAddress].Remove(shopItem);

            return true;
        }

        public bool TryGet(Address sellerAvatarAddress, Guid productId,
            out KeyValuePair<Address, ShopItem> outPair)
        {
            if (!items.ContainsKey(sellerAvatarAddress))
            {
                return false;
            }

            var list = items[sellerAvatarAddress];

            foreach (var shopItem in list)
            {
                if (shopItem.productId != productId)
                {
                    continue;
                }

                outPair = new KeyValuePair<Address, ShopItem>(sellerAvatarAddress, shopItem);
                return true;
            }

            return false;
        }
        
        public bool TryUnregister(Address sellerAvatarAddress,
            Guid productId, out ShopItem outUnregisteredItem)
        {
            if (!TryGet(sellerAvatarAddress, productId, out var outPair))
            {
                outUnregisteredItem = null;
                return false;
            }
            
            items[outPair.Key].Remove(outPair.Value);

            outUnregisteredItem = outPair.Value;
            return true;
        }
    }
}
