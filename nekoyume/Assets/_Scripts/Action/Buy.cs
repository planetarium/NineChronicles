using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using Nekoyume.Game.Mail;
using Nekoyume.State;
using UnityEngine;

namespace Nekoyume.Action
{
    [ActionType("buy")]
    public class Buy : GameAction
    {
        public Address buyerAvatarAddress;
        public Address sellerAgentAddress;
        public Address sellerAvatarAddress;
        public Guid productId;
        public BuyerResult buyerResult;
        public SellerResult sellerResult;

        [Serializable]
        public class BuyerResult : AttachmentActionResult
        {
            public Game.Item.ShopItem shopItem;

            protected override string TypeId => "buy.buyerResult";

            public BuyerResult()
            {
            }

            public BuyerResult(Bencodex.Types.Dictionary serialized) : base(serialized)
            {
                shopItem = new ShopItem((Bencodex.Types.Dictionary) serialized["shopItem"]);
            }

            public override IValue Serialize() =>
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "shopItem"] = shopItem.Serialize(),
                }.Union((Bencodex.Types.Dictionary) base.Serialize()));
        }

        [Serializable]
        public class SellerResult : AttachmentActionResult
        {
            public Game.Item.ShopItem shopItem;
            public decimal gold;

            protected override string TypeId => "buy.sellerResult";

            public SellerResult()
            {
            }

            public SellerResult(Bencodex.Types.Dictionary serialized) : base(serialized)
            {
                shopItem = new ShopItem((Bencodex.Types.Dictionary) serialized["shopItem"]);
                gold = serialized["gold"].ToDecimal();
            }

            public override IValue Serialize() =>
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "shopItem"] = shopItem.Serialize(),
                    [(Text) "gold"] = gold.Serialize(),
                }.Union((Bencodex.Types.Dictionary) base.Serialize()));
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["buyerAvatarAddress"] = buyerAvatarAddress.Serialize(),
            ["sellerAgentAddress"] = sellerAgentAddress.Serialize(),
            ["sellerAvatarAddress"] = sellerAvatarAddress.Serialize(),
            ["productId"] = productId.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            buyerAvatarAddress = plainValue["buyerAvatarAddress"].ToAddress();
            sellerAgentAddress = plainValue["sellerAgentAddress"].ToAddress();
            sellerAvatarAddress = plainValue["sellerAvatarAddress"].ToAddress();
            productId = plainValue["productId"].ToGuid();
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(buyerAvatarAddress, MarkChanged);
                states = states.SetState(ctx.Signer, MarkChanged);
                states = states.SetState(sellerAvatarAddress, MarkChanged);
                states = states.SetState(sellerAgentAddress, MarkChanged);
                return states.SetState(ShopState.Address, MarkChanged);
            }

            if (ctx.Signer.Equals(sellerAgentAddress))
                return states;

            if (!states.TryGetAgentAvatarStates(ctx.Signer, buyerAvatarAddress, out AgentState buyerAgentState, out AvatarState buyerAvatarState))
            {
                return states;
            }

            ShopState shopState;
            if (!states.TryGetState(ShopState.Address, out Bencodex.Types.Dictionary d))
            {
                return states;
            }
            shopState = new ShopState(d);

            Debug.Log($"Execute Buy. buyer : `{buyerAvatarAddress}` seller: `{sellerAvatarAddress}`" +
                      $"node : `{States.Instance?.AgentState?.Value?.address}` " +
                      $"current avatar: `{States.Instance?.CurrentAvatarState?.Value?.address}`");
            // 상점에서 구매할 아이템을 찾는다.
            if (!shopState.TryGet(sellerAgentAddress, productId, out var outPair))
            {
                return states;
            }

            if (!states.TryGetAgentAvatarStates(sellerAgentAddress, sellerAvatarAddress, out AgentState sellerAgentState, out AvatarState sellerAvatarState))
            {
                return states;
            }

            // 돈은 있냐?
            if (buyerAgentState.gold < outPair.Value.Price)
            {
                return states;
            }

            // 상점에서 구매할 아이템을 제거한다.
            if (!shopState.Unregister(sellerAgentAddress, outPair.Value))
            {
                return states;
            }

            // 구매자의 돈을 감소 시킨다.
            buyerAgentState.gold -= outPair.Value.Price;

            // 구매자, 판매자에게 결과 메일 전송
            buyerResult = new BuyerResult
            {
                shopItem = outPair.Value,
                itemUsable = outPair.Value.ItemUsable
            };
            var buyerMail = new BuyerMail(buyerResult, ctx.BlockIndex);
            buyerAvatarState.Update(buyerMail);

            sellerResult = new SellerResult
            {
                shopItem = outPair.Value,
                itemUsable = outPair.Value.ItemUsable,
                gold = decimal.Round(outPair.Value.Price * 0.92m)
            };
            var sellerMail = new SellerMail(sellerResult, ctx.BlockIndex);
            sellerAvatarState.Update(sellerMail);

            // 퀘스트 업데이트
            buyerAvatarState.questList.UpdateTradeQuest(TradeType.Buy, outPair.Value.Price);
            sellerAvatarState.questList.UpdateTradeQuest(TradeType.Sell, outPair.Value.Price);

            var timestamp = DateTimeOffset.UtcNow;
            buyerAvatarState.updatedAt = timestamp;
            buyerAvatarState.BlockIndex = ctx.BlockIndex;
            sellerAvatarState.updatedAt = timestamp;
            sellerAvatarState.BlockIndex = ctx.BlockIndex;

            return states
                .SetState(buyerAvatarAddress, buyerAvatarState.Serialize())
                .SetState(ctx.Signer, buyerAgentState.Serialize())
                .SetState(sellerAvatarAddress, sellerAvatarState.Serialize())
                .SetState(sellerAgentAddress, sellerAgentState.Serialize())
                .SetState(ShopState.Address, shopState.Serialize());
        }
    }
}
