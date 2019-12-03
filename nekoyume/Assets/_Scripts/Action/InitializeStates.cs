using System;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Nekoyume.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("initialize_states")]
    public class InitializeStates : GameAction
    {
        public RankingState RankingState { get; set; }
        public ShopState ShopState { get; set; }
        public TableSheetsState TableSheetsState { get; set; }

        public override IAccountStateDelta Execute(IActionContext ctx)
        {
            var states = ctx.PreviousStates;
            if (ctx.Rehearsal)
            {
                states = states.SetState(RankingState.Address, MarkChanged);
                states = states.SetState(ShopState.Address, MarkChanged);
                states = states.SetState(TableSheetsState.Address, MarkChanged);
                return states;
            }

            if (ctx.BlockIndex != 0)
            {
                return states;
            }

            states = states
                .SetState(RankingState.Address, RankingState.Serialize())
                .SetState(ShopState.Address, ShopState.Serialize())
                .SetState(TableSheetsState.Address, TableSheetsState.Serialize());
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .Add("ranking_state", RankingState.Serialize())
                .Add("shop_state", ShopState.Serialize())
                .Add("table_sheets_state", TableSheetsState.Serialize());

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            RankingState = new RankingState((Bencodex.Types.Dictionary) plainValue["ranking_state"]);
            ShopState = new ShopState((Bencodex.Types.Dictionary) plainValue["shop_state"]);
            TableSheetsState = new TableSheetsState((Bencodex.Types.Dictionary) plainValue["table_sheets_state"]);
        }
    }
}
