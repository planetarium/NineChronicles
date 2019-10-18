using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.BlockChain;
using Nekoyume.Game.Factory;
using Nekoyume.Game.Item;
using Nekoyume.State;
using UnityEngine;

namespace Nekoyume.Action
{
    [ActionType("sell")]
    public class Sell : GameAction
    {
        [Serializable]
        public class ResultModel
        {
            public Address sellerAvatarAddress;
            public ShopItem shopItem;
        }

        public Address sellerAvatarAddress;
        public Guid productId;
        public ItemUsable itemUsable;
        public decimal price;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["sellerAvatarAddress"] = sellerAvatarAddress.Serialize(),
            ["productId"] = productId.Serialize(),
            ["itemUsable"] = itemUsable.Serialize(),
            ["price"] = price.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            sellerAvatarAddress = plainValue["sellerAvatarAddress"].ToAddress();
            productId = plainValue["productId"].ToGuid();
            itemUsable = (ItemUsable) ItemFactory.Deserialize(
                (Bencodex.Types.Dictionary) plainValue["itemUsable"]
            );
            price = plainValue["price"].ToDecimal();
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(ShopState.Address, MarkChanged);
                states = states.SetState(sellerAvatarAddress, MarkChanged);
                return states.SetState(ctx.Signer, MarkChanged);
            }

            if (price < 0)
            {
                return states;
            }

            if (!states.GetAgentAvatarStates(ctx.Signer, sellerAvatarAddress, out AgentState agentState, out AvatarState avatarState))
            {
                return states;
            }

            if (!states.TryGetState(ShopState.Address, out Bencodex.Types.Dictionary d))
            {
                return states;
            }
            ShopState shopState = new ShopState(d);

            Debug.Log($"Execute Sell. seller : `{sellerAvatarAddress}` " +
                      $"node : `{States.Instance?.AgentState?.Value?.address}` " +
                      $"current avatar: `{States.Instance?.CurrentAvatarState?.Value?.address}`");

            // 인벤토리에서 판매할 아이템을 선택하고 수량을 조절한다.
            if (!avatarState.inventory.TryGetNonFungibleItem(itemUsable, out ItemUsable nonFungibleItem))
            {
                return states;
            }

            avatarState.inventory.RemoveNonFungibleItem(nonFungibleItem);
            if (nonFungibleItem is Equipment equipment)
            {
                equipment.equipped = false;
            }

            // 상점에 아이템을 등록한다.
            shopState.Register(ctx.Signer, new ShopItem(
                sellerAvatarAddress,
                productId,
                nonFungibleItem,
                price
            ));

            avatarState.updatedAt = DateTimeOffset.UtcNow;
            avatarState.BlockIndex = ctx.BlockIndex;

            return states
                .SetState(sellerAvatarAddress, avatarState.Serialize())
                .SetState(ctx.Signer, agentState.Serialize())
                .SetState(ShopState.Address, shopState.Serialize());
        }
    }
}
