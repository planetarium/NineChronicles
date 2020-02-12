using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Serilog;

namespace Nekoyume.Action
{
    [ActionType("sell_cancellation")]
    public class SellCancellation : GameAction
    {
        public Guid productId;
        public Address sellerAvatarAddress;
        public Result result;

        [Serializable]
        public class Result : AttachmentActionResult
        {
            public ShopItem shopItem;

            protected override string TypeId => "sellCancellation.result";

            public Result()
            {
            }

            public Result(Bencodex.Types.Dictionary serialized) : base(serialized)
            {
                shopItem = new ShopItem((Bencodex.Types.Dictionary) serialized["shopItem"]);
            }

            public override IValue Serialize() =>
                new Bencodex.Types.Dictionary(new Dictionary<IKey, IValue>
                {
                    [(Text) "shopItem"] = shopItem.Serialize(),
                }.Union((Bencodex.Types.Dictionary) base.Serialize()));
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["productId"] = productId.Serialize(),
            ["sellerAvatarAddress"] = sellerAvatarAddress.Serialize(),
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            productId = plainValue["productId"].ToGuid();
            sellerAvatarAddress = plainValue["sellerAvatarAddress"].ToAddress();
        }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(ShopState.Address, MarkChanged);
                return states.SetState(sellerAvatarAddress, MarkChanged);
            }
            var sw = new Stopwatch();
            sw.Start();
            var started = DateTimeOffset.UtcNow;
            Log.Debug($"Sell Cancel exec started.");

            if (!states.TryGetAgentAvatarStates(ctx.Signer, sellerAvatarAddress, out _, out var avatarState))
            {
                return states;
            }
            sw.Stop();
            Log.Debug($"Sell Cancel Get AgentAvatarStates: {sw.Elapsed}");
            sw.Restart();

            if (!avatarState.worldInformation.TryGetUnlockedWorldByLastStageClearedAt(
                out var world))
                return states;

            if (world.StageClearedId < GameConfig.RequireStage.ActionsInShop)
            {
                // 스테이지 클리어 부족 에러.
                return states;
            }

            if (!states.TryGetState(ShopState.Address, out Bencodex.Types.Dictionary d))
            {
                return states;
            }
            var shopState = new ShopState(d);
            sw.Stop();
            Log.Debug($"Sell Cancel Get ShopState: {sw.Elapsed}");
            sw.Restart();

            // 상점에서 아이템을 빼온다.
            if (!shopState.TryUnregister(ctx.Signer, productId, out var outUnregisteredItem))
            {
                return states;
            }

            sw.Stop();
            Log.Debug($"Sell Cancel Get Unregister Item: {sw.Elapsed}");
            sw.Restart();

            // 메일에 아이템을 넣는다.
            result = new Result
            {
                shopItem = outUnregisteredItem,
                itemUsable = outUnregisteredItem.ItemUsable
            };
            var mail = new SellCancelMail(result, ctx.BlockIndex)
            {
                New = false
            };
            avatarState.Update(mail);
            avatarState.UpdateFromAddItem(result.itemUsable, true);
            avatarState.updatedAt = DateTimeOffset.UtcNow;
            avatarState.blockIndex = ctx.BlockIndex;
            sw.Stop();
            Log.Debug($"Sell Cancel Update AvatarState: {sw.Elapsed}");
            sw.Restart();

            states = states.SetState(sellerAvatarAddress, avatarState.Serialize());
            sw.Stop();
            Log.Debug($"Sell Cancel Set AvatarState: {sw.Elapsed}");
            sw.Restart();

            states = states.SetState(ShopState.Address, shopState.Serialize());
            sw.Stop();
            var ended = DateTimeOffset.UtcNow;
            Log.Debug($"Sell Cancel Set ShopState: {sw.Elapsed}");
            Log.Debug($"Sell Cancel Total Executed Time: {ended - started}");
            return states;
        }
    }
}
