using System.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("initialize_states")]
    public class InitializeStates : GameAction
    {
        public RankingState RankingState { get; set; }
        public ShopState ShopState { get; set; }
        public Dictionary<string, string> TableSheets { get; set; }
        public GameConfigState GameConfigState { get; set; }
        public RedeemCodeState RedeemCodeState { get; set; }

        public AdminState AdminAddressState { get; set; }

        public ActivatedAccountsState ActivatedAccountsState { get; set; }

        public GoldCurrencyState GoldCurrencyState { get; set; }

        public GoldDistribution[] GoldDistributions { get; set; }

        public PendingActivationState[] PendingActivationStates { get; set; }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            var weeklyArenaState = new WeeklyArenaState(0);
            if (ctx.Rehearsal)
            {
                states = states.SetState(RankingState.Address, MarkChanged);
                states = states.SetState(ShopState.Address, MarkChanged);
                states = TableSheets
                    .Aggregate(states, (current, pair) =>
                        current.SetState(Addresses.TableSheet.Derive(pair.Key), MarkChanged));
                states = RankingState.RankingMap
                    .Aggregate(states, (current, pair) =>
                        current.SetState(pair.Key, MarkChanged));
                states = states.SetState(weeklyArenaState.address, MarkChanged);
                states = states.SetState(GameConfigState.Address, MarkChanged);
                states = states.SetState(RedeemCodeState.Address, MarkChanged);
                states = states.SetState(AdminState.Address, MarkChanged);
                states = states.SetState(ActivatedAccountsState.Address, MarkChanged);
                states = states.SetState(GoldCurrencyState.Address, MarkChanged);
                states = states.SetState(Addresses.GoldDistribution, MarkChanged);
                foreach (var pendingActivationState in PendingActivationStates)
                {
                    states = states.SetState(pendingActivationState.address, MarkChanged);
                }
                return states;
            }

            if (ctx.BlockIndex != 0)
            {
                return states;
            }

            states = TableSheets
                .Aggregate(states, (current, pair) =>
                    current.SetState(Addresses.TableSheet.Derive(pair.Key), pair.Value.Serialize()));
            states = RankingState.RankingMap
                .Aggregate(states, (current, pair) =>
                    current.SetState(pair.Key, new RankingMapState(pair.Key).Serialize()));
            states = states
                .SetState(weeklyArenaState.address, weeklyArenaState.Serialize())
                .SetState(RankingState.Address, RankingState.Serialize())
                .SetState(ShopState.Address, ShopState.Serialize())
                .SetState(GameConfigState.Address, GameConfigState.Serialize())
                .SetState(RedeemCodeState.Address, RedeemCodeState.Serialize())
                .SetState(AdminState.Address, AdminAddressState.Serialize())
                .SetState(ActivatedAccountsState.Address, ActivatedAccountsState.Serialize())
                .SetState(GoldCurrencyState.Address, GoldCurrencyState.Serialize())
                .SetState(Addresses.GoldDistribution,
                    GoldDistributions.Select(v => v.Serialize()).Serialize());

            foreach (var pendingActivationState in PendingActivationStates)
            {
                states = states.SetState(pendingActivationState.address,
                    pendingActivationState.Serialize());
            }

            states = states.MintAsset(GoldCurrencyState.Address, GoldCurrencyState.Currency * 1000000000);
            return states;
        }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            ImmutableDictionary<string, IValue>.Empty
                .Add("ranking_state", RankingState.Serialize())
                .Add("shop_state", ShopState.Serialize())
                .Add("table_sheets",
                    new Dictionary(TableSheets.Select(pair =>
                        new KeyValuePair<IKey, IValue>((Text) pair.Key, (Text) pair.Value))))
                .Add("game_config_state", GameConfigState.Serialize())
                .Add("redeem_code_state", RedeemCodeState.Serialize())
                .Add("admin_address_state", AdminAddressState.Serialize())
                .Add("activated_accounts_state", ActivatedAccountsState.Serialize())
                .Add("gold_currency_state", GoldCurrencyState.Serialize())
                .Add("gold_distributions", GoldDistributions.Select(v => v.Serialize()).Serialize())
                .Add("pending_activation_states",
                    PendingActivationStates.Select(v => v.Serialize()).Serialize());

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            RankingState = new RankingState((Bencodex.Types.Dictionary) plainValue["ranking_state"]);
            ShopState = new ShopState((Bencodex.Types.Dictionary) plainValue["shop_state"]);
            TableSheets = ((Bencodex.Types.Dictionary) plainValue["table_sheets"])
                .ToDictionary(pair => (string) (Text) pair.Key, pair => (string) (Text) pair.Value);
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
            PendingActivationStates = ((Bencodex.Types.List) plainValue["pending_activation_states"])
                .Select(e => new PendingActivationState((Bencodex.Types.Dictionary)e))
                .ToArray();
        }
    }
}
