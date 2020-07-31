using System.Linq;
using System;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("initialize_states")]
    public class InitializeStates : GameAction
    {
        public RankingState RankingState { get; set; }
        public ShopState ShopState { get; set; }
        public TableSheetsState TableSheetsState { get; set; }
        public GameConfigState GameConfigState { get; set; }
        public RedeemCodeState RedeemCodeState { get; set; }

        public AdminState AdminAddressState { get; set; }

        public ActivatedAccountsState ActivatedAccountsState { get; set; }

        public GoldCurrencyState GoldCurrencyState { get; set; }

        public GoldDistribution[] GoldDistributions { get; set; }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            var weeklyArenaState = new WeeklyArenaState(0);
            if (ctx.Rehearsal)
            {
                states = states.SetState(RankingState.Address, MarkChanged);
                states = states.SetState(ShopState.Address, MarkChanged);
                states = states.SetState(TableSheetsState.Address, MarkChanged);
                states = states.SetState(weeklyArenaState.address, MarkChanged);
                states = states.SetState(GameConfigState.Address, MarkChanged);
                states = states.SetState(RedeemCodeState.Address, MarkChanged);
                states = states.SetState(AdminState.Address, MarkChanged);
                states = states.SetState(ActivatedAccountsState.Address, MarkChanged);
                states = states.SetState(GoldCurrencyState.Address, MarkChanged);
                states = states.SetState(Addresses.GoldDistribution, MarkChanged);
                return states;
            }

            if (ctx.BlockIndex != 0)
            {
                return states;
            }

            states = states
                .SetState(weeklyArenaState.address, weeklyArenaState.Serialize())
                .SetState(RankingState.Address, RankingState.Serialize())
                .SetState(ShopState.Address, ShopState.Serialize())
                .SetState(TableSheetsState.Address, TableSheetsState.Serialize())
                .SetState(GameConfigState.Address, GameConfigState.Serialize())
                .SetState(RedeemCodeState.Address, RedeemCodeState.Serialize())
                .SetState(AdminState.Address, AdminAddressState.Serialize())
                .SetState(ActivatedAccountsState.Address, ActivatedAccountsState.Serialize())
                .SetState(GoldCurrencyState.Address, GoldCurrencyState.Serialize())
                .SetState(Addresses.GoldDistribution, GoldDistributions.Select(v => v.Serialize()).Serialize());

            states = states.MintAsset(GoldCurrencyState.Address, GoldCurrencyState.Currency, 1000000000);
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .Add("ranking_state", RankingState.Serialize())
                .Add("shop_state", ShopState.Serialize())
                .Add("table_sheets_state", TableSheetsState.Serialize())
                .Add("game_config_state", GameConfigState.Serialize())
                .Add("redeem_code_state", RedeemCodeState.Serialize())
                .Add("admin_address_state", AdminAddressState.Serialize())
                .Add("activated_accounts_state", ActivatedAccountsState.Serialize())
                .Add("gold_currency_state", GoldCurrencyState.Serialize())
                .Add("gold_distributions", GoldDistributions.Select(v => v.Serialize()).Serialize());

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            RankingState = new RankingState((Bencodex.Types.Dictionary) plainValue["ranking_state"]);
            ShopState = new ShopState((Bencodex.Types.Dictionary) plainValue["shop_state"]);
            TableSheetsState = new TableSheetsState((Bencodex.Types.Dictionary) plainValue["table_sheets_state"]);
            GameConfigState = new GameConfigState((Bencodex.Types.Dictionary) plainValue["game_config_state"]);
            RedeemCodeState = new RedeemCodeState((Bencodex.Types.Dictionary) plainValue["redeem_code_state"]);
            AdminAddressState = new AdminState((Bencodex.Types.Dictionary)plainValue["admin_address_state"]);
            ActivatedAccountsState = new ActivatedAccountsState(
                (Bencodex.Types.Dictionary)plainValue["activated_accounts_state"]
            );
            GoldCurrencyState = new GoldCurrencyState(
                (Bencodex.Types.Dictionary)plainValue["gold_currency_state"]
            );
            GoldDistributions = ((Bencodex.Types.List) plainValue["gold_distributions"])
                .Select(e => new GoldDistribution(e))
                .ToArray();
        }
    }
}
