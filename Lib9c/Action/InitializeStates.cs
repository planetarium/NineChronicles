using System.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    /// <summary>
    /// Introduced at Initial commit(2e645be18a4e2caea031c347f00777fbad5dbcc6)
    /// Updated at NCG distribution(7e4515b6e14cc5d6eb716d5ebb587ab04b4246f9)
    /// Updated at https://github.com/planetarium/lib9c/pull/19
    /// Updated at https://github.com/planetarium/lib9c/pull/36
    /// Updated at https://github.com/planetarium/lib9c/pull/42
    /// Updated at https://github.com/planetarium/lib9c/pull/55
    /// Updated at https://github.com/planetarium/lib9c/pull/57
    /// Updated at https://github.com/planetarium/lib9c/pull/60
    /// Updated at https://github.com/planetarium/lib9c/pull/102
    /// Updated at https://github.com/planetarium/lib9c/pull/128
    /// Updated at https://github.com/planetarium/lib9c/pull/167
    /// Updated at https://github.com/planetarium/lib9c/pull/422
    /// Updated at https://github.com/planetarium/lib9c/pull/747
    /// Updated at https://github.com/planetarium/lib9c/pull/798
    /// Updated at https://github.com/planetarium/lib9c/pull/957
    /// </summary>
    [Serializable]
    [ActionType("initialize_states")]
    public class InitializeStates : GameAction, IInitializeStatesV1
    {
        public Dictionary Ranking { get; set; } = Dictionary.Empty;
        public Dictionary Shop { get; set; } = Dictionary.Empty;
        public Dictionary<string, string> TableSheets { get; set; }
        public Dictionary GameConfig { get; set; } = Dictionary.Empty;
        public Dictionary RedeemCode { get; set; } = Dictionary.Empty;

        public Dictionary AdminAddressState { get; set; }

        public Dictionary ActivatedAccounts { get; set; } = Dictionary.Empty;

        public Dictionary GoldCurrency { get; set; } = Dictionary.Empty;

        public List GoldDistributions { get; set; } = List.Empty;

        public List PendingActivations { get; set; } = List.Empty;

        // This property can contain null:
        public Dictionary AuthorizedMiners { get; set; }

        // This property can contain null:
        public Dictionary Credits { get; set; }

        Dictionary IInitializeStatesV1.Ranking => Ranking;
        Dictionary IInitializeStatesV1.Shop => Shop;
        Dictionary<string, string> IInitializeStatesV1.TableSheets => TableSheets;
        Dictionary IInitializeStatesV1.GameConfig => GameConfig;
        Dictionary IInitializeStatesV1.RedeemCode => RedeemCode;
        Dictionary IInitializeStatesV1.AdminAddressState => AdminAddressState;
        Dictionary IInitializeStatesV1.ActivatedAccounts => ActivatedAccounts;
        Dictionary IInitializeStatesV1.GoldCurrency => GoldCurrency;
        List IInitializeStatesV1.GoldDistributions => GoldDistributions;
        List IInitializeStatesV1.PendingActivations => PendingActivations;
        Dictionary IInitializeStatesV1.AuthorizedMiners => AuthorizedMiners;
        Dictionary IInitializeStatesV1.Credits => Credits;

        public InitializeStates()
        {
        }

        public InitializeStates(
            RankingState0 rankingState,
            ShopState shopState,
            Dictionary<string, string> tableSheets,
            GameConfigState gameConfigState,
            RedeemCodeState redeemCodeState,
            ActivatedAccountsState activatedAccountsState,
            GoldCurrencyState goldCurrencyState,
            GoldDistribution[] goldDistributions,
            PendingActivationState[] pendingActivationStates,
            AdminState adminAddressState = null,
            AuthorizedMinersState authorizedMinersState = null,
            CreditsState creditsState = null)
        {
            Ranking = (Dictionary)rankingState.Serialize();
            Shop = (Dictionary)shopState.Serialize();
            TableSheets = tableSheets;
            GameConfig = (Dictionary)gameConfigState.Serialize();
            RedeemCode = (Dictionary)redeemCodeState.Serialize();
            AdminAddressState = (Dictionary)adminAddressState?.Serialize();
            ActivatedAccounts = (Dictionary)activatedAccountsState.Serialize();
            GoldCurrency = (Dictionary)goldCurrencyState.Serialize();
            GoldDistributions = new List(goldDistributions.Select(d => d.Serialize()));
            PendingActivations = new List(pendingActivationStates.Select(p => p.Serialize()));

            if (!(authorizedMinersState is null))
            {
                AuthorizedMiners = (Dictionary)authorizedMinersState.Serialize();
            }

            if (!(creditsState is null))
            {
                Credits = (Dictionary)creditsState.Serialize();
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
                        new PendingActivationState((Dictionary)rawPending).address,
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
                .SetState(ActivatedAccountsState.Address, ActivatedAccounts)
                .SetState(GoldCurrencyState.Address, GoldCurrency)
                .SetState(Addresses.GoldDistribution, GoldDistributions);

            if (!(AdminAddressState is null))
            {
                states = states.SetState(AdminState.Address, AdminAddressState);
            }

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
                    new PendingActivationState((Dictionary)rawPending).address,
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

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                var rv = ImmutableDictionary<string, IValue>.Empty
                .Add("ranking_state", Ranking)
                .Add("shop_state", Shop)
                .Add("table_sheets",
#pragma warning disable LAA1002
                    new Dictionary(TableSheets.Select(pair =>
                        new KeyValuePair<IKey, IValue>(
                            (Text)pair.Key, (Text)pair.Value))))
#pragma warning restore LAA1002
                .Add("game_config_state", GameConfig)
                .Add("redeem_code_state", RedeemCode)
                .Add("activated_accounts_state", ActivatedAccounts)
                .Add("gold_currency_state", GoldCurrency)
                .Add("gold_distributions", GoldDistributions)
                .Add("pending_activation_states", PendingActivations);

                if (!(AdminAddressState is null))
                {
                    rv = rv.Add("admin_address_state", AdminAddressState);
                }

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

        protected override void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue)
        {
            Ranking = (Dictionary) plainValue["ranking_state"];
            Shop = (Dictionary) plainValue["shop_state"];
            TableSheets = ((Dictionary) plainValue["table_sheets"])
                .ToDictionary(
                pair => (string)(Text) pair.Key,
                pair => (string)(Text) pair.Value
                );
            GameConfig = (Dictionary) plainValue["game_config_state"];
            RedeemCode = (Dictionary) plainValue["redeem_code_state"];
            ActivatedAccounts = (Dictionary)plainValue["activated_accounts_state"];
            GoldCurrency = (Dictionary)plainValue["gold_currency_state"];
            GoldDistributions = (List)plainValue["gold_distributions"];
            PendingActivations = (List)plainValue["pending_activation_states"];

            if (plainValue.TryGetValue("admin_address_state", out var adminAddressState))
            {
                AdminAddressState = (Dictionary)adminAddressState;
            }

            if (plainValue.TryGetValue("authorized_miners_state", out IValue authorizedMiners))
            {
                AuthorizedMiners = (Dictionary)authorizedMiners;
            }

            if (plainValue.TryGetValue("credits_state", out IValue credits))
            {
                Credits = (Dictionary)credits;
            }
        }
    }
}
