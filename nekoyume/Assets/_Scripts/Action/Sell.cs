using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Libplanet;
using Libplanet.Action;
using Nekoyume.BlockChain;
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

        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>
        {
            ["sellerAvatarAddress"] = sellerAvatarAddress.ToByteArray(),
            ["productId"] = productId.ToByteArray(),
            ["itemUsable"] = ByteSerializer.Serialize(itemUsable),
            ["price"] = price.ToString(CultureInfo.InvariantCulture),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            sellerAvatarAddress = new Address((byte[]) plainValue["sellerAvatarAddress"]);
            productId = new Guid((byte[]) plainValue["productId"]);
            itemUsable = ByteSerializer.Deserialize<ItemUsable>((byte[]) plainValue["itemUsable"]);
            price = decimal.Parse(plainValue["price"].ToString());
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

            var agentState = (AgentState) states.GetState(ctx.Signer);
            if (!agentState.avatarAddresses.ContainsValue(sellerAvatarAddress))
                return states;

            var avatarState = (AvatarState) states.GetState(sellerAvatarAddress);
            if (avatarState == null)
            {
                return states;
            }

            var shopState = (ShopState) states.GetState(ShopState.Address);

            Debug.Log($"Execute Sell. seller : `{sellerAvatarAddress}` " +
                      $"node : `{States.Instance.agentState.Value.address}` " +
                      $"current avatar: `{States.Instance.currentAvatarState?.Value?.address}`");

            // 인벤토리에서 판매할 아이템을 선택하고 수량을 조절한다.
            if (!avatarState.inventory.TryGetNonFungibleItem(itemUsable, out ItemUsable nonFungibleItem))
            {
                return states;
            }

            avatarState.inventory.RemoveNonFungibleItem(nonFungibleItem);
            if(nonFungibleItem is Equipment equipment)
            {
                equipment.equipped = false;
            }
            
            // 상점에 아이템을 등록한다.
            var shopItem = shopState.Register(ctx.Signer, new ShopItem
            {
                sellerAvatarAddress = sellerAvatarAddress,
                productId = productId,
                itemUsable = nonFungibleItem,
                price = price
            });

            avatarState.updatedAt = DateTimeOffset.UtcNow;

            states = states.SetState(sellerAvatarAddress, avatarState);
            states = states.SetState(ctx.Signer, agentState);
            return states.SetState(ShopState.Address, shopState);
        }
    }
}
