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

        protected override IAccountStateDelta ExecuteInternal(IActionContext actionCtx)
        {
            var states = actionCtx.PreviousStates;
            if (actionCtx.Rehearsal)
            {
                states = states.SetState(AddressBook.Shop, MarkChanged);
                return states.SetState(actionCtx.Signer, MarkChanged);
            }

            var avatarState = (AvatarState) states.GetState(actionCtx.Signer);
            var shopState = (ShopState) states.GetState(AddressBook.Shop) ?? new ShopState(AddressBook.Shop);

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
                    return SimpleError(actionCtx, avatarState, GameActionErrorCode.SellItemCountNotEnoughInInventory);
                }
                target.Count -= count;
            }

            // 인벤토리에 판매할 아이템이 없는 경우.
            if (ReferenceEquals(target, null))
            {
                return SimpleError(actionCtx, avatarState, GameActionErrorCode.SellItemNotFoundInInventory);
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
            shopItem = shopState.Register(actionCtx.Signer, shopItem);

            avatarState.updatedAt = DateTimeOffset.UtcNow;
            errorCode = GameActionErrorCode.Success;
            result = new ResultModel
            {
                sellerAvatarAddress = actionCtx.Signer,
                shopItem = shopItem
            };

            states = states.SetState(actionCtx.Signer, avatarState);
            return states.SetState(AddressBook.Shop, shopState);
        }
    }
}
