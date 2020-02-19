using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("daily_reward")]
    public class DailyReward : GameAction
    {
        public Address avatarAddress;
        public int refillPoint;

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                return states.SetState(avatarAddress, MarkChanged);
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, avatarAddress, out _, out AvatarState avatarState))
            {
                return states;
            }

            if (ctx.BlockIndex - avatarState.dailyRewardReceivedIndex >= GameConfig.DailyRewardInterval)
            {
                avatarState.dailyRewardReceivedIndex = ctx.BlockIndex;
                avatarState.actionPoint = refillPoint;
            }

            return states.SetState(avatarAddress, avatarState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["avatarAddress"] = avatarAddress.Serialize(),
            ["refillPoint"] = (Integer) refillPoint,
        }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            refillPoint = (int) ((Integer) plainValue["refillPoint"]).Value;
        }
    }
}
