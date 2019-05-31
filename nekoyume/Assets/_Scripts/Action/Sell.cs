using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Libplanet;
using Libplanet.Action;
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

        public Address sellerAgentAddress;
        public Guid productId;
        public int itemId;
        public int count;
        public decimal price;

        public ResultModel result;

        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>
        {
            ["sellerAgentAddress"] = sellerAgentAddress.ToByteArray(),
            ["productId"] = productId.ToByteArray(),
            ["itemId"] = itemId.ToString(),
            ["count"] = count.ToString(),
            ["price"] = price.ToString(CultureInfo.InvariantCulture),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            sellerAgentAddress = new Address((byte[]) plainValue["sellerAgentAddress"]);
            productId = new Guid((byte[]) plainValue["productId"]);
            itemId = int.Parse(plainValue["itemId"].ToString());
            count = int.Parse(plainValue["count"].ToString());
            price = decimal.Parse(plainValue["price"].ToString());
        }

        protected override IAccountStateDelta ExecuteInternal(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(ShopState.Address, MarkChanged);
                return states.SetState(ctx.Signer, MarkChanged);
            }

            var avatarState = (AvatarState) states.GetState(ctx.Signer);
            if (avatarState == null)
            {
                return SimpleError(ctx, ErrorCode.AvatarNotFound);
            }
            
            var shopState = (ShopState) states.GetState(ShopState.Address) ?? new ShopState();

            // 인벤토리에서 판매할 아이템을 선택하고 수량을 조절한다.
            Inventory.InventoryItem target = null;
            foreach (var item in avatarState.items)
            {
                if (item.Item.Data.id != itemId ||
                    item.Count == 0)
                {
                    continue;
                }

                target = item;
                if (target.Count < count)
                {
                    return SimpleError(ctx, ErrorCode.SellItemCountNotEnoughInInventory);
                }
                target.Count -= count;
            }

            // 인벤토리에 판매할 아이템이 없는 경우.
            if (ReferenceEquals(target, null))
            {
                return SimpleError(ctx, ErrorCode.SellItemNotFoundInInventory);
            }

            // 인벤토리에서 판매할 아이템을 뺀 후에 수량이 0일 경우.
            if (target.Count == 0)
            {
                avatarState.items.Remove(target);
            }

            // 상점에 아이템을 등록한다.
            var shopItem = new ShopItem
            {
                sellerAgentAddress = sellerAgentAddress,
                productId = productId,
                item = target.Item,
                count = count,
                price = price
            };
            shopItem = shopState.Register(ctx.Signer, shopItem);

            avatarState.updatedAt = DateTimeOffset.UtcNow;
            result = new ResultModel
            {
                sellerAvatarAddress = ctx.Signer,
                shopItem = shopItem
            };

            states = states.SetState(ctx.Signer, avatarState);
            return states.SetState(ShopState.Address, shopState);
        }
    }
}
