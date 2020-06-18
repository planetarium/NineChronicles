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
    [ActionType("weekly_arena_reward")]
    public class WeeklyArenaReward : GameAction
    {
        public Address AvatarAddress;
        public Address WeeklyArenaAddress;

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                return states.MarkBalanceChanged(Currencies.Gold, ctx.Signer);
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, AvatarAddress, out AgentState _, out _))
            {
                return states;
            }

            var weeklyArenaState = states.GetWeeklyArenaState(WeeklyArenaAddress);
            if (weeklyArenaState is null)
            {
                return states;
            }

            if (!weeklyArenaState.TryGetValue(AvatarAddress, out var info))
            {
                return states;
            }

            if (info.Receive)
            {
                return states;
            }

            info.Receive = true;

            var tier = weeklyArenaState.GetTier(info);

            var gold = weeklyArenaState.GetReward(tier);

            // FIXME: 사실 여기서 mint를 바로 하면 안되고 미리 펀드 같은 걸 만들어서 거기로부터 TransferAsset()해야 함...
            // 근데 RankingBattle 액션에서 입장료 받아다 WeeklyArenaAddress에다 쌓아두는데 그거 빼서 주면 안되는지?
            return states.MintAsset(ctx.Signer, Currencies.Gold, gold);
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal => new Dictionary<string, IValue>
        {
            ["avatarAddress"] = AvatarAddress.Serialize(),
            ["weeklyArenaAddress"] = WeeklyArenaAddress.Serialize()
        }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            WeeklyArenaAddress = plainValue["weeklyArenaAddress"].ToAddress();
        }
    }
}
