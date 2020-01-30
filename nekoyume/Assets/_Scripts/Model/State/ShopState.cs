using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;

namespace Nekoyume.Model.State
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

        public readonly Dictionary<Address, List<ShopItem>> AgentProducts = new Dictionary<Address, List<ShopItem>>();

        public ShopState() : base(Address)
        {
        }

        public ShopState(Dictionary serialized)
            : base(serialized)
        {
            AgentProducts = ((Dictionary)serialized["agentProducts"]).ToDictionary(
                kv => kv.Key.ToAddress(),
                kv => ((List)kv.Value)
                    .Select(d => new ShopItem((Dictionary)d))
                    .ToList()
            );
        }

        public ShopItem Register(Address sellerAgentAddress, ShopItem shopItem)
        {
            if (!AgentProducts.ContainsKey(sellerAgentAddress))
            {
                AgentProducts.Add(sellerAgentAddress, new List<ShopItem>());
            }

            AgentProducts[sellerAgentAddress].Add(shopItem);
            return shopItem;
        }

        public bool Unregister(Address sellerAgentAddress, ShopItem shopItem)
        {
            return Unregister(sellerAgentAddress, shopItem.ProductId);
        }

        public bool Unregister(Address sellerAgentAddress, Guid productId)
        {
            if (!AgentProducts.ContainsKey(sellerAgentAddress))
                return false;

            var shopItems = AgentProducts[sellerAgentAddress];
            var shopItem = shopItems.FirstOrDefault(item => item.ProductId.Equals(productId));
            if (shopItem is null)
                return false;

            shopItems.Remove(shopItem);
            if (shopItems.Count == 0)
            {
                AgentProducts.Remove(sellerAgentAddress);
            }

            return true;
        }

        public bool TryGet(Address sellerAgentAddress, Guid productId,
            out KeyValuePair<Address, ShopItem> outPair)
        {
            if (!AgentProducts.ContainsKey(sellerAgentAddress))
            {
                return false;
            }

            var list = AgentProducts[sellerAgentAddress];

            foreach (var shopItem in list)
            {
                if (shopItem.ProductId != productId)
                {
                    continue;
                }

                outPair = new KeyValuePair<Address, ShopItem>(sellerAgentAddress, shopItem);
                return true;
            }

            return false;
        }

        public bool TryUnregister(Address sellerAgentAddress,
            Guid productId, out ShopItem outUnregisteredItem)
        {
            if (!TryGet(sellerAgentAddress, productId, out var outPair))
            {
                outUnregisteredItem = null;
                return false;
            }

            AgentProducts[outPair.Key].Remove(outPair.Value);

            outUnregisteredItem = outPair.Value;
            return true;
        }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"agentProducts"] = new Dictionary(
                    AgentProducts.Select(kv =>
                        new KeyValuePair<IKey, IValue>(
                            (Binary)kv.Key.Serialize(),
                            new List(kv.Value.Select(i => i.Serialize()))
                        )
                    )
                )
            }.Union((Dictionary)base.Serialize()));
    }
}
