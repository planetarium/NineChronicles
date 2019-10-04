using System.Collections.Generic;
using System.Collections.Immutable;
using Libplanet;
using Libplanet.Action;
using Nekoyume.State;

namespace Nekoyume.Action
{
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

            var avatarState = (AvatarState) states.GetState(avatarAddress);
            if (avatarState is null)
                return states;

            var dailyBlockState = (DailyBlockState) states.GetState(DailyBlockState.Address);
            if (avatarState.nextDailyRewardIndex <= dailyBlockState.nextBlockIndex)
            {
                avatarState.nextDailyRewardIndex = dailyBlockState.nextBlockIndex;
                avatarState.actionPoint = refillPoint;
            }

            return states.SetState(avatarAddress, avatarState);
        }

        protected override IImmutableDictionary<string, object> PlainValueInternal => new Dictionary<string, object>
        {
            ["avatarAddress"] = avatarAddress.ToByteArray(),
            ["refillPoint"] = ByteSerializer.Serialize(refillPoint)
        }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, object> plainValue)
        {
            avatarAddress = new Address((byte[]) plainValue["avatarAddress"]);
            refillPoint = ByteSerializer.Deserialize<int>((byte[]) plainValue["refillPoint"]);
        }
    }
}
