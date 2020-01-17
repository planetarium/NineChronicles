using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.State;
using Nekoyume.TableData;
using UnityEngine;

namespace Nekoyume.Action
{
    [ActionType("ranking_battle")]
    public class RankingBattle: GameAction
    {
        public Address AvatarAddress;
        public Address EnemyAddress;
        public Address WeeklyArenaAddress;
        public BattleLog Result { get; private set; }

        private const int BaseVictoryPoint = 20;
        private const int BaseDefeatPoint = -15;

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
                    arenaInfo = weeklyArenaState.Active(avatarState, 100);
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

            Debug.Log(weeklyArenaState.address.ToHex());

            var tableSheetState = TableSheetsState.FromActionContext(ctx);
            var tableSheets = TableSheets.FromTableSheetsState(tableSheetState);

            var simulator = new RankingSimulator(
                ctx.Random,
                avatarState,
                enemyAvatarState,
                new List<Consumable>(),
                tableSheets);

            simulator.Simulate();

            Result = simulator.Log;

            var score = CalculatePoint(arenaInfo, weeklyArenaState[EnemyAddress], simulator.Result);
            arenaInfo.Update(score);
            weeklyArenaState.Update(arenaInfo);

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
            }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            AvatarAddress = plainValue["avatarAddress"].ToAddress();
            EnemyAddress = plainValue["enemyAddress"].ToAddress();
        }

        private int CalculatePoint(ArenaInfo info, ArenaInfo enemyInfo, BattleLog.Result result)
        {
            if (result == BattleLog.Result.TimeOver)
            {
                return 0;
            }

            var rating = info.Score;
            var enemyRating = enemyInfo.Score;
            if (rating == enemyRating)
            {
                switch (result)
                {
                    case BattleLog.Result.Win:
                        return BaseVictoryPoint;
                    case BattleLog.Result.Lose:
                        return BaseDefeatPoint;
                }
            }

            switch (result)
            {
                case BattleLog.Result.Win:
                    return (int) ((decimal) enemyRating / rating / 0.75m) * BaseVictoryPoint;
                case BattleLog.Result.Lose:
                    return (int) ((decimal) rating / enemyRating / 0.75m) * BaseDefeatPoint;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), result, null);
            }
        }
    }
}
