namespace Lib9c.Tests.TestHelper
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Lib9c.DevExtensions.Action;
    using Lib9c.Renderers;
    using Lib9c.Tests.Action;
    using Libplanet;
    using Libplanet.Assets;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Blocks;
    using Libplanet.Crypto;
    using Libplanet.Store;
    using Libplanet.Store.Trie;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
#pragma warning disable SA1135
    using NCAction = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;
#pragma warning restore SA1135

    public static class BlockChainHelper
    {
        public static BlockChain<NCAction> MakeBlockChain(
            BlockRenderer[] blockRenderers,
            IBlockPolicy<NCAction> policy = null,
            IStagePolicy<NCAction> stagePolicy = null,
            IStore store = null,
            IStateStore stateStore = null)
        {
            PrivateKey adminPrivateKey = new PrivateKey();

            policy ??= new BlockPolicy<NCAction>();
            stagePolicy ??= new VolatileStagePolicy<NCAction>();
            store ??= new DefaultStore(null);
            stateStore ??= new TrieStateStore(new DefaultKeyValueStore(null));
            Block<NCAction> genesis = MakeGenesisBlock(adminPrivateKey.ToAddress(), ImmutableHashSet<Address>.Empty);
            return new BlockChain<NCAction>(policy, stagePolicy, store, stateStore, genesis, renderers: blockRenderers );
        }

        public static Block<NCAction> MakeGenesisBlock(
            Address adminAddress,
            IImmutableSet<Address> activatedAddresses,
            AuthorizedMinersState authorizedMinersState = null,
            DateTimeOffset? timestamp = null,
            PendingActivationState[] pendingActivations = null
        )
        {
            PrivateKey privateKey = new PrivateKey();
            if (pendingActivations is null)
            {
                var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
                (ActivationKey activationKey, PendingActivationState pendingActivation) =
                    ActivationKey.Create(privateKey, nonce);
                pendingActivations = new[] { pendingActivation };
            }

            var sheets = TableSheetsImporter.ImportSheets();
            return BlockHelper.MineGenesisBlock(
                sheets,
                new GoldDistribution[0],
                pendingActivations,
                new AdminState(adminAddress, 1500000),
                authorizedMinersState: authorizedMinersState,
                activatedAccounts: activatedAddresses,
                isActivateAdminAddress: false,
                credits: null,
                privateKey: privateKey,
                timestamp: timestamp ?? DateTimeOffset.MinValue);
        }

        public static MakeInitialStateResult MakeInitialState()
        {
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var goldCurrencyState = new GoldCurrencyState(Currency.Legacy("NCG", 2, null));
#pragma warning restore CS0618
            var ranking = new RankingState1();
            for (var i = 0; i < RankingState1.RankingMapCapacity; i++)
            {
                ranking.RankingMap[RankingState1.Derive(i)] = new HashSet<Address>().ToImmutableHashSet();
            }

            var sheets = TableSheetsImporter.ImportSheets();
            var weeklyArenaAddress = WeeklyArenaState.DeriveAddress(0);
            var initialState = new Tests.Action.State()
                .SetState(GoldCurrencyState.Address, goldCurrencyState.Serialize())
                .SetState(
                    Addresses.GoldDistribution,
                    GoldDistributionTest.Fixture.Select(v => v.Serialize()).Serialize()
                )
                .SetState(
                    Addresses.GameConfig,
                    new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize()
                )
                .SetState(Addresses.Ranking, ranking.Serialize())
                .SetState(weeklyArenaAddress, new WeeklyArenaState(0).Serialize());

            foreach (var (key, value) in sheets)
            {
                initialState = initialState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var tableSheets = new TableSheets(sheets);
            var rankingMapAddress = new PrivateKey().ToAddress();

            var agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(agentAddress);

            var avatarAddress = new PrivateKey().ToAddress();
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            agentState.avatarAddresses[0] = avatarAddress;

            var initCurrencyGold = goldCurrencyState.Currency * 100000000000;
            var agentCurrencyGold = goldCurrencyState.Currency * 1000;
            var remainCurrencyGold = initCurrencyGold - agentCurrencyGold;
            initialState = initialState
                .SetState(GoldCurrencyState.Address, goldCurrencyState.Serialize())
                .SetState(agentAddress, agentState.Serialize())
                .SetState(avatarAddress, avatarState.Serialize())
                .SetState(Addresses.Shop, new ShopState().Serialize())
                .MintAsset(GoldCurrencyState.Address, initCurrencyGold)
                .TransferAsset(Addresses.GoldCurrency, agentAddress,  agentCurrencyGold);

            var action = new CreateTestbed
            {
                weeklyArenaAddress = weeklyArenaAddress,
            };
            var nextState = action.Execute(new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = initialState,
                Random = new TestRandom(),
                Rehearsal = false,
            });

            return new MakeInitialStateResult(
                nextState,
                action,
                agentState,
                avatarState,
                goldCurrencyState,
                rankingMapAddress,
                tableSheets,
                remainCurrencyGold,
                agentCurrencyGold);
        }
    }
}
