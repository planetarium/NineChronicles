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
                return states
                    .SetState(WeeklyArenaAddress, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, ctx.Signer);
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, AvatarAddress, out var agentState, out _))
            {
                if (agentState is null)
                {
                    throw new FailedLoadStateException(
                        $"Failed Load State: {nameof(AgentState)}. Address: {ctx.Signer}");
                }
                throw new FailedLoadStateException(
                    $"Failed Load State: {nameof(AvatarState)}. Address: {AvatarAddress}");
            }

            var weeklyArenaState = states.GetWeeklyArenaState(WeeklyArenaAddress);
            if (weeklyArenaState is null)
            {
                throw new FailedLoadStateException(
                    $"Failed Load State: {nameof(WeeklyArenaState)}. Address: {WeeklyArenaAddress}");
            }

            if (!weeklyArenaState.Ended)
            {
                throw new ArenaNotEndedException($"Arena has not ended yet. Address: {WeeklyArenaAddress}");
            }

            if (!weeklyArenaState.TryGetValue(AvatarAddress, out var info))
            {
                throw new KeyNotFoundException($"Arena {WeeklyArenaAddress} not contains {AvatarAddress}");
            }

            if (info.Receive)
            {
                throw new AlreadyReceivedException($"Already Received Address. WeeklyArenaAddress: {WeeklyArenaAddress} AvatarAddress: {AvatarAddress}");
            }

            var tier = weeklyArenaState.GetTier(info);
            var gold = weeklyArenaState.GetReward(tier);

            // FIXME: RankingBattle 액션에서 입장료 받아다 WeeklyArenaAddress에다 쌓아두는데 그거 빼서 주면 안되는지?
            states = states.TransferAsset(
                GoldCurrencyState.Address,
                ctx.Signer,
                states.GetGoldCurrency(),
                gold
            );

            weeklyArenaState.SetReceive(AvatarAddress);

            return states.SetState(WeeklyArenaAddress, weeklyArenaState.Serialize());
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
