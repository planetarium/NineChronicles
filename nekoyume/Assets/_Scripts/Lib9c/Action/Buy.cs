using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
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
            public ShopItem shopItem;
            public Guid id;

            protected override string TypeId => "buy.buyerResult";

            public BuyerResult()
            {
            }

            public BuyerResult(Bencodex.Types.Dictionary serialized) : base(serialized)
            {
                shopItem = new ShopItem((Bencodex.Types.Dictionary) serialized["shopItem"]);
                id = serialized["id"].ToGuid();
            }

            public override IValue Serialize() =>
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "shopItem"] = shopItem.Serialize(),
                    [(Text) "id"] = id.Serialize(),
                }.Union((Bencodex.Types.Dictionary) base.Serialize()));
        }

        [Serializable]
        public class SellerResult : AttachmentActionResult
        {
            public ShopItem shopItem;
            public Guid id;
            public decimal gold;

            protected override string TypeId => "buy.sellerResult";

            public SellerResult()
            {
            }

            public SellerResult(Bencodex.Types.Dictionary serialized) : base(serialized)
            {
                shopItem = new ShopItem((Bencodex.Types.Dictionary) serialized["shopItem"]);
                id = serialized["id"].ToGuid();
                gold = serialized["gold"].ToDecimal();
            }

            public override IValue Serialize() =>
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "shopItem"] = shopItem.Serialize(),
                    [(Text) "id"] = id.Serialize(),
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

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
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
            {
                return LogError(context, "Aborted as the signer is the seller.");
            }

            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug("Buy exec started.");

            if (!states.TryGetAgentAvatarStates(ctx.Signer, buyerAvatarAddress, out var buyerAgentState, out var buyerAvatarState))
            {
                return LogError(context, "Aborted as the avatar state of the buyer was failed to load.");
            }
            sw.Stop();
            Log.Debug("Buy Get Buyer AgentAvatarStates: {Elapsed}", sw.Elapsed);
            sw.Restart();

            if (!buyerAvatarState.worldInformation.TryGetUnlockedWorldByStageClearedBlockIndex(out var world))
            {
                return LogError(context, "Aborted as the WorldInformation was failed to load.");
            }

            if (world.StageClearedId < GameConfig.RequireClearedStageLevel.ActionsInShop)
            {
                // 스테이지 클리어 부족 에러.
                return LogError(
                    context,
                    "Aborted as the signer is not cleared the minimum stage level required to use shop yet: {ClearedLevel} < {RequiredLevel}.",
                    world.StageClearedId,
                    GameConfig.RequireClearedStageLevel.ActionsInShop
                );
            }

            if (!states.TryGetState(ShopState.Address, out Bencodex.Types.Dictionary d))
            {
                return LogError(context, "Aborted as the shop state was failed to load.");
            }

            var shopState = new ShopState(d);
            sw.Stop();
            Log.Debug("Buy Get ShopState: {Elapsed}", sw.Elapsed);
            sw.Restart();

            Log.Debug("Execute Buy; buyer: {Buyer} seller: {Seller}", buyerAvatarAddress, sellerAvatarAddress);
            // 상점에서 구매할 아이템을 찾는다.
            if (!shopState.TryGet(sellerAgentAddress, productId, out var outPair))
            {
                return LogError(
                    context,
                    "Aborted as the shop item ({ProductId}) was failed to get from the seller agent ({SellerAgent}).",
                    productId,
                    sellerAgentAddress
                );
            }
            sw.Stop();
            Log.Debug($"Buy Get Item: {sw.Elapsed}");
            sw.Restart();

            if (!states.TryGetAgentAvatarStates(sellerAgentAddress, sellerAvatarAddress, out var sellerAgentState, out var sellerAvatarState))
            {
                return LogError(
                    context,
                    "Aborted as the seller agent/avatar was filed to load from {SellerAgent}/{SellerAvatar}.",
                    sellerAgentAddress,
                    sellerAvatarAddress
                );
            }
            sw.Stop();
            Log.Debug($"Buy Get Seller AgentAvatarStates: {sw.Elapsed}");
            sw.Restart();

            // 돈은 있냐?
            if (buyerAgentState.gold < outPair.Value.Price)
            {
                return LogError(
                    context,
                    "Aborted as the buyer ({Buyer}) has no sufficient gold: {BuyerBalance} < {ItemPrice}",
                    ctx.Signer,
                    buyerAgentState.gold,
                    outPair.Value.Price
                );
            }

            var taxedPrice = decimal.Round(outPair.Value.Price * 0.92m);

            // 구매자의 돈을 감소시킨다.
            buyerAgentState.gold -= outPair.Value.Price;
            // 판매자의 돈을 증가시킨다.
            sellerAgentState.gold += taxedPrice;

            // 상점에서 구매할 아이템을 제거한다.
            if (!shopState.Unregister(sellerAgentAddress, outPair.Value))
            {
                return LogError(
                    context,
                    "Aborted as the item ({@ProductId}) was failed to unregister from seller ({Seller})'s inventory.",
                    outPair.Value.ProductId,
                    sellerAgentAddress
                );
            }

            // 구매자, 판매자에게 결과 메일 전송
            buyerResult = new BuyerResult
            {
                shopItem = outPair.Value,
                itemUsable = outPair.Value.ItemUsable
            };
            var buyerMail = new BuyerMail(buyerResult, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(), ctx.BlockIndex);
            buyerResult.id = buyerMail.id;

            sellerResult = new SellerResult
            {
                shopItem = outPair.Value,
                itemUsable = outPair.Value.ItemUsable,
                gold = taxedPrice
            };
            var sellerMail = new SellerMail(sellerResult, ctx.BlockIndex, ctx.Random.GenerateRandomGuid(),
                ctx.BlockIndex);
            sellerResult.id = sellerMail.id;

            buyerAvatarState.Update(buyerMail);
            buyerAvatarState.UpdateFromAddItem(buyerResult.itemUsable, false);
            sellerAvatarState.Update(sellerMail);

            // 퀘스트 업데이트
            buyerAvatarState.questList.UpdateTradeQuest(TradeType.Buy, outPair.Value.Price);
            sellerAvatarState.questList.UpdateTradeQuest(TradeType.Sell, outPair.Value.Price);

            var timestamp = DateTimeOffset.UtcNow;
            buyerAvatarState.updatedAt = timestamp;
            buyerAvatarState.blockIndex = ctx.BlockIndex;
            sellerAvatarState.updatedAt = timestamp;
            sellerAvatarState.blockIndex = ctx.BlockIndex;

            buyerAvatarState.UpdateQuestRewards(ctx);
            sellerAvatarState.UpdateQuestRewards(ctx);

            states = states.SetState(sellerAvatarAddress, sellerAvatarState.Serialize());
            sw.Stop();
            Log.Debug("Buy Set Seller AvatarState: {Elapsed}", sw.Elapsed);
            sw.Restart();

            states = states.SetState(buyerAvatarAddress, buyerAvatarState.Serialize());
            sw.Stop();
            Log.Debug("Buy Set Buyer AvatarState: {Elapsed}", sw.Elapsed);
            sw.Restart();

            states = states.SetState(ShopState.Address, shopState.Serialize());
            sw.Stop();
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("Buy Set ShopState: {Elapsed}", sw.Elapsed);
            Log.Debug("Buy Total Executed Time: {Elapsed}", ended - started);

            return states
                .SetState(ctx.Signer, buyerAgentState.Serialize())
                .SetState(sellerAgentAddress, sellerAgentState.Serialize());
        }
    }
}
