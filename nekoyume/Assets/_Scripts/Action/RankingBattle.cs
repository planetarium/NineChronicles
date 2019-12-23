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

namespace Nekoyume.Action
{
    [ActionType("ranking_battle")]
    public class RankingBattle: GameAction
    {
        public Address avatarAddress;
        public Address enemyAddress;
        public BattleLog Result { get; private set; }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                return states.SetState(ctx.Signer, MarkChanged)
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(enemyAddress, MarkChanged);
            }

            if (!states.TryGetAgentAvatarStates(ctx.Signer, avatarAddress, out AgentState agentState,
                out AvatarState avatarState))
            {
                return states;
            }

            var enemyAvatarState = states.GetAvatarState(enemyAddress);
            if (enemyAvatarState is null)
            {
                return states;
            }

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
            return states.SetState(avatarAddress, avatarState.Serialize());
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["enemyAddress"] = enemyAddress.Serialize(),
            }.ToImmutableDictionary();
        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            enemyAddress = plainValue["enemyAddress"].ToAddress();
        }
    }
}
