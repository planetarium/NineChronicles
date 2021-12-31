using System.Linq;
using System;
using System.Collections.Generic;
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
        public Bencodex.Types.Dictionary Ranking { get; set; } = Dictionary.Empty;
        public Bencodex.Types.Dictionary Shop { get; set; } = Dictionary.Empty;
        public Dictionary<string, string> TableSheets { get; set; }
        public Bencodex.Types.Dictionary GameConfig { get; set; } = Dictionary.Empty;
        public Bencodex.Types.Dictionary RedeemCode { get; set; } = Dictionary.Empty;

        public Bencodex.Types.Dictionary AdminAddress { get; set; } = Dictionary.Empty;

        public Bencodex.Types.Dictionary ActivatedAccounts { get; set; } = Dictionary.Empty;

        public Bencodex.Types.Dictionary GoldCurrency { get; set; } = Dictionary.Empty;

        public Bencodex.Types.List GoldDistributions { get; set; } = List.Empty;

        public Bencodex.Types.List PendingActivations { get; set; } = List.Empty;

        // This property can contain null:
        public Bencodex.Types.Dictionary AuthorizedMiners { get; set; } = null;

        // This property can contain null:
        public Bencodex.Types.Dictionary Credits { get; set; } = null;

        public InitializeStates()
        {
        }

        public InitializeStates(
            RankingState0 rankingState,
            ShopState shopState,
            Dictionary<string, string> tableSheets,
            GameConfigState gameConfigState,
            RedeemCodeState redeemCodeState,
            AdminState adminAddressState,
            ActivatedAccountsState activatedAccountsState,
            GoldCurrencyState goldCurrencyState,
            GoldDistribution[] goldDistributions,
            PendingActivationState[] pendingActivationStates,
            AuthorizedMinersState authorizedMinersState = null,
            CreditsState creditsState = null)
        {
            Ranking = (Bencodex.Types.Dictionary)rankingState.Serialize();
            Shop = (Bencodex.Types.Dictionary)shopState.Serialize();
            TableSheets = tableSheets;
            GameConfig = (Bencodex.Types.Dictionary)gameConfigState.Serialize();
            RedeemCode = (Bencodex.Types.Dictionary)redeemCodeState.Serialize();
            AdminAddress = (Bencodex.Types.Dictionary)adminAddressState.Serialize();
            ActivatedAccounts = (Bencodex.Types.Dictionary)activatedAccountsState.Serialize();
            GoldCurrency = (Bencodex.Types.Dictionary)goldCurrencyState.Serialize();
            GoldDistributions = new Bencodex.Types.List(
                goldDistributions.Select(d => d.Serialize()).Cast<Bencodex.Types.IValue>()
            );
            PendingActivations = new Bencodex.Types.List(pendingActivationStates.Select(p => p.Serialize()));

            if (!(authorizedMinersState is null))
            {
                AuthorizedMiners = (Bencodex.Types.Dictionary)authorizedMinersState.Serialize();
            }

            if (!(creditsState is null))
            {
                Credits = (Bencodex.Types.Dictionary)creditsState.Serialize();
            }
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            IActionContext ctx = context;
            var states = ctx.PreviousStates;
            var weeklyArenaState = new WeeklyArenaState(0);

            var rankingState = new RankingState0(Ranking);
            if (ctx.Rehearsal)
            {
                states = states.SetState(RankingState0.Address, MarkChanged);
                states = states.SetState(ShopState.Address, MarkChanged);
#pragma warning disable LAA1002
                states = TableSheets
                    .Aggregate(states, (current, pair) =>
                        current.SetState(Addresses.TableSheet.Derive(pair.Key), MarkChanged));
                states = rankingState.RankingMap
                    .Aggregate(states, (current, pair) =>
                        current.SetState(pair.Key, MarkChanged));
#pragma warning restore LAA1002
                states = states.SetState(weeklyArenaState.address, MarkChanged);
                states = states.SetState(GameConfigState.Address, MarkChanged);
                states = states.SetState(RedeemCodeState.Address, MarkChanged);
                states = states.SetState(AdminState.Address, MarkChanged);
                states = states.SetState(ActivatedAccountsState.Address, MarkChanged);
                states = states.SetState(GoldCurrencyState.Address, MarkChanged);
                states = states.SetState(Addresses.GoldDistribution, MarkChanged);
                foreach (var rawPending in PendingActivations)
                {
                    states = states.SetState(
                        new PendingActivationState((Bencodex.Types.Dictionary)rawPending).address,
                        MarkChanged
                    );
                }
                states = states.SetState(AuthorizedMinersState.Address, MarkChanged);
                states = states.SetState(CreditsState.Address, MarkChanged);
                return states;
            }

            if (ctx.BlockIndex != 0)
            {
                return states;
            }

#pragma warning disable LAA1002
            states = TableSheets
                .Aggregate(states, (current, pair) =>
                    current.SetState(Addresses.TableSheet.Derive(pair.Key), pair.Value.Serialize()));
            states = rankingState.RankingMap
                .Aggregate(states, (current, pair) =>
                    current.SetState(pair.Key, new RankingMapState(pair.Key).Serialize()));
#pragma warning restore LAA1002
            states = states
                .SetState(weeklyArenaState.address, weeklyArenaState.Serialize())
                .SetState(RankingState0.Address, Ranking)
                .SetState(ShopState.Address, Shop)
                .SetState(GameConfigState.Address, GameConfig)
                .SetState(RedeemCodeState.Address, RedeemCode)
                .SetState(AdminState.Address, AdminAddress)
                .SetState(ActivatedAccountsState.Address, ActivatedAccounts)
                .SetState(GoldCurrencyState.Address, GoldCurrency)
                .SetState(Addresses.GoldDistribution, GoldDistributions);

            if (!(AuthorizedMiners is null))
            {
                states = states.SetState(
                    AuthorizedMinersState.Address,
                    AuthorizedMiners
                );
            }

            foreach (var rawPending in PendingActivations)
            {
                states = states.SetState(
                    new PendingActivationState((Bencodex.Types.Dictionary)rawPending).address,
                    rawPending
                );
            }

            if (!(Credits is null))
            {
                states = states.SetState(CreditsState.Address, Credits);
            }

            var currency = new GoldCurrencyState(GoldCurrency).Currency;
            states = states.MintAsset(GoldCurrencyState.Address, currency * 1000000000);
            return states;
        }

        protected override IImmutableDictionary<string, Bencodex.Types.IValue> PlainValueInternal
        {
            get
            {
                var rv = ImmutableDictionary<string, Bencodex.Types.IValue>.Empty
                .Add("ranking_state", Ranking)
                .Add("shop_state", Shop)
                .Add("table_sheets",
#pragma warning disable LAA1002
                    new Bencodex.Types.Dictionary(TableSheets.Select(pair =>
                        new KeyValuePair<Bencodex.Types.IKey, Bencodex.Types.IValue>(
                            (Bencodex.Types.Text)pair.Key, (Bencodex.Types.Text)pair.Value))))
#pragma warning restore LAA1002
                .Add("game_config_state", GameConfig)
                .Add("redeem_code_state", RedeemCode)
                .Add("admin_address_state", AdminAddress)
                .Add("activated_accounts_state", ActivatedAccounts)
                .Add("gold_currency_state", GoldCurrency)
                .Add("gold_distributions", GoldDistributions)
                .Add("pending_activation_states", PendingActivations);

                if (!(AuthorizedMiners is null))
                {
                    rv = rv.Add("authorized_miners_state", AuthorizedMiners);
                }

                if (!(Credits is null))
                {
                    rv = rv.Add("credits_state", Credits);
                }

                return rv;
            }
        }

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, Bencodex.Types.IValue> plainValue)
        {
            Ranking = (Bencodex.Types.Dictionary) plainValue["ranking_state"];
            Shop = (Bencodex.Types.Dictionary) plainValue["shop_state"];
            TableSheets = ((Bencodex.Types.Dictionary) plainValue["table_sheets"])
                .ToDictionary(
                pair => (string)(Bencodex.Types.Text) pair.Key,
                pair => (string)(Bencodex.Types.Text) pair.Value
                );
            GameConfig = (Bencodex.Types.Dictionary) plainValue["game_config_state"];
            RedeemCode = (Bencodex.Types.Dictionary) plainValue["redeem_code_state"];
            AdminAddress = (Bencodex.Types.Dictionary)plainValue["admin_address_state"];
            ActivatedAccounts = (Bencodex.Types.Dictionary)plainValue["activated_accounts_state"];
            GoldCurrency = (Bencodex.Types.Dictionary)plainValue["gold_currency_state"];
            GoldDistributions = (Bencodex.Types.List)plainValue["gold_distributions"];
            PendingActivations = (Bencodex.Types.List)plainValue["pending_activation_states"];

            if (plainValue.TryGetValue("authorized_miners_state", out Bencodex.Types.IValue authorizedMiners))
            {
                AuthorizedMiners = (Bencodex.Types.Dictionary)authorizedMiners;
            }

            if (plainValue.TryGetValue("credits_state", out Bencodex.Types.IValue credits))
            {
                Credits = (Bencodex.Types.Dictionary)credits;
            }
        }
    }
}
