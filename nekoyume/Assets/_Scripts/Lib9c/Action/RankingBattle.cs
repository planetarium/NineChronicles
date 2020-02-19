using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Serilog;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("ranking_battle")]
    public class RankingBattle : GameAction
    {
        public Address AvatarAddress;
        public Address EnemyAddress;
        public Address WeeklyArenaAddress;
        public BattleLog Result { get; private set; }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                return states.SetState(ctx.Signer, MarkChanged)
                    .SetState(AvatarAddress, MarkChanged)
                    .SetState(WeeklyArenaAddress, MarkChanged)
                    .SetState(ctx.Signer, MarkChanged);
            }

            if (AvatarAddress.Equals(EnemyAddress))
            {
                return states;
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, AvatarAddress, out var agentState,
                out var avatarState))
            {
                return states;
            }

            var enemyAvatarState = states.GetAvatarState(EnemyAddress);
            if (enemyAvatarState is null)
            {
                return states;
            }

            var weeklyArenaState = states.GetWeeklyArenaState(WeeklyArenaAddress);

            if (!weeklyArenaState.ContainsKey(AvatarAddress))
            {
                return states;
            }

            var arenaInfo = weeklyArenaState[AvatarAddress];

            if (arenaInfo.DailyChallengeCount <= 0)
            {
                return states;
            }

            if (!arenaInfo.Active)
            {
                if (agentState.gold >= 100)
                {
                    agentState.gold -= 100;
                    weeklyArenaState.Gold += 100;
                    arenaInfo.Activate();
                }
                else
                {
                    return states;
                }
            }

            if (!weeklyArenaState.ContainsKey(EnemyAddress))

            {
                return states;
            }

            Log.Debug(weeklyArenaState.address.ToHex());

            var tableSheetState = TableSheetsState.FromActionContext(ctx);
            var tableSheets = TableSheets.FromTableSheetsState(tableSheetState);

            var simulator = new RankingSimulator(
                ctx.Random,
                avatarState,
                enemyAvatarState,
                new List<Consumable>(),
                tableSheets);

            simulator.Simulate();

            simulator.Log.diffScore = arenaInfo.Update(avatarState, weeklyArenaState[EnemyAddress], simulator.Result);
            simulator.Log.score = arenaInfo.Score;

            Result = simulator.Log;

            return states
                .SetState(ctx.Signer, agentState.Serialize())
                .SetState(WeeklyArenaAddress, weeklyArenaState.Serialize())
                .SetState(AvatarAddress, avatarState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = AvatarAddress.Serialize(),
                ["enemyAddress"] = EnemyAddress.Serialize(),
                ["weeklyArenaAddress"] = WeeklyArenaAddress.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            EnemyAddress = plainValue["enemyAddress"].ToAddress();
            WeeklyArenaAddress = plainValue["weeklyArenaAddress"].ToAddress();
        }
    }
}
