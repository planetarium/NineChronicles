namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using Bencodex;
    using Bencodex.Types;
    using Lib9c.Tests.TestHelper;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Blockchain;
    using Libplanet.Blockchain.Policies;
    using Libplanet.Blocks;
    using Libplanet.Crypto;
    using Libplanet.Store;
    using Libplanet.Store.Trie;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.BlockChain;
    using Nekoyume.BlockChain.Policy;
    using Nekoyume.Model;
    using Nekoyume.Model.BattleStatus;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog.Core;
    using Xunit;

    public class RewardGoldTest
    {
        private readonly AvatarState _avatarState;
        private readonly AvatarState _avatarState2;
        private readonly State _baseState;
        private readonly TableSheets _tableSheets;

        public RewardGoldTest()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            sheets[nameof(CharacterSheet)] = string.Join(
                    Environment.NewLine,
                    "id,_name,size_type,elemental_type,hp,atk,def,cri,hit,spd,lv_hp,lv_atk,lv_def,lv_cri,lv_hit,lv_spd,attack_range,run_speed",
                    "100010,전사,S,0,300,20,10,10,90,70,12,0.8,0.4,0,3.6,2.8,2,3");

            var privateKey = new PrivateKey();
            var agentAddress = privateKey.PublicKey.ToAddress();

            var avatarAddress = agentAddress.Derive("avatar");
            _tableSheets = new TableSheets(sheets);

            _avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            _avatarState2 = new AvatarState(
                new PrivateKey().ToAddress(),
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var gold = new GoldCurrencyState(Currency.Legacy("NCG", 2, null));
#pragma warning restore CS0618
            _baseState = (State)new State()
                .SetState(GoldCurrencyState.Address, gold.Serialize())
                .SetState(Addresses.GoldDistribution, GoldDistributionTest.Fixture.Select(v => v.Serialize()).Serialize())
                .MintAsset(GoldCurrencyState.Address, gold.Currency * 100000000000);
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void WeeklyArenaRankingBoard(bool resetCount, bool updateNext)
        {
            var weekly = new WeeklyArenaState(0);
            weekly.Set(_avatarState, _tableSheets.CharacterSheet);
            weekly[_avatarState.address].Update(
                weekly[_avatarState.address],
                BattleLog.Result.Lose,
                ArenaScoreHelper.GetScore);
            var gameConfigState = new GameConfigState();
            gameConfigState.Set(_tableSheets.GameConfigSheet);
            var state = _baseState
                .SetState(weekly.address, weekly.Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize());
            var blockIndex = 0;

            if (resetCount)
            {
                blockIndex = gameConfigState.DailyArenaInterval;
            }

            if (updateNext)
            {
                weekly[_avatarState.address].Activate();
                blockIndex = gameConfigState.WeeklyArenaInterval;
                // Avoid NRE in test case.
                var nextWeekly = new WeeklyArenaState(1);
                state = state
                    .SetState(weekly.address, weekly.Serialize())
                    .SetState(nextWeekly.address, nextWeekly.Serialize());
            }

            Assert.False(weekly.Ended);
            Assert.Equal(4, weekly[_avatarState.address].DailyChallengeCount);

            var action = new RewardGold();

            var ctx = new ActionContext()
            {
                BlockIndex = blockIndex,
                PreviousStates = _baseState,
                Miner = default,
            };

            var states = new[]
            {
                action.WeeklyArenaRankingBoard2(ctx, state),
                action.WeeklyArenaRankingBoard(ctx, state),
            };

            foreach (var nextState in states)
            {
                var currentWeeklyState = nextState.GetWeeklyArenaState(0);
                var nextWeeklyState = nextState.GetWeeklyArenaState(1);

                Assert.Contains(WeeklyArenaState.DeriveAddress(0), nextState.UpdatedAddresses);
                Assert.Contains(WeeklyArenaState.DeriveAddress(1), nextState.UpdatedAddresses);

                if (updateNext)
                {
                    Assert.Contains(WeeklyArenaState.DeriveAddress(2), nextState.UpdatedAddresses);
                    Assert.Equal(blockIndex, nextWeeklyState.ResetIndex);
                }

                if (resetCount)
                {
                    var expectedCount = updateNext ? 4 : 5;
                    var expectedIndex = updateNext ? 0 : blockIndex;
                    Assert.Equal(expectedCount, currentWeeklyState[_avatarState.address].DailyChallengeCount);
                    Assert.Equal(expectedIndex, currentWeeklyState.ResetIndex);
                }

                Assert.Equal(updateNext, currentWeeklyState.Ended);
                Assert.Contains(_avatarState.address, currentWeeklyState);
                Assert.Equal(updateNext, nextWeeklyState.ContainsKey(_avatarState.address));
            }
        }

        [Theory]
        // Migration from WeeklyArenaState.Map
        [InlineData(67, 68, 3_808_000L, 2)]
        // Update from WeeklyArenaList
        [InlineData(68, 69, 3_864_000L, 2)]
        // Filter deactivated ArenaInfo
        [InlineData(70, 71, 3_976_000L, 1)]
        public void PrepareNextArena(int prevWeeklyIndex, int targetWeeklyIndex, long blockIndex, int expectedCount)
        {
            var prevWeekly = new WeeklyArenaState(prevWeeklyIndex);
            var avatarAddress = _avatarState.address;
            var inactiveAvatarAddress = _avatarState2.address;
            bool afterUpdate = prevWeeklyIndex >= 68;
            bool filterInactive = blockIndex >= 3_976_000L;
            if (!afterUpdate)
            {
                prevWeekly.Set(_avatarState, _tableSheets.CharacterSheet);
                prevWeekly[avatarAddress].Update(
                    prevWeekly[avatarAddress],
                    BattleLog.Result.Lose,
                    ArenaScoreHelper.GetScore);
                prevWeekly.Set(_avatarState2, _tableSheets.CharacterSheet);

                Assert.Equal(4, prevWeekly[avatarAddress].DailyChallengeCount);
                Assert.False(prevWeekly[avatarAddress].Active);
                Assert.False(prevWeekly[inactiveAvatarAddress].Active);
            }

            var gameConfigState = new GameConfigState();
            gameConfigState.Set(_tableSheets.GameConfigSheet);
            var targetWeekly = new WeeklyArenaState(targetWeeklyIndex);
            var state = _baseState
                .SetState(prevWeekly.address, prevWeekly.Serialize())
                .SetState(targetWeekly.address, targetWeekly.Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize());

            if (afterUpdate)
            {
                var prevInfo = new ArenaInfo(_avatarState, _tableSheets.CharacterSheet, true);
                prevInfo.Update(
                    prevInfo,
                    BattleLog.Result.Lose,
                    ArenaScoreHelper.GetScore);

                Assert.Equal(4, prevInfo.DailyChallengeCount);

                var inactiveInfo = new ArenaInfo(_avatarState2, _tableSheets.CharacterSheet, true);
                state = state
                    .SetState(
                        prevWeekly.address.Derive(avatarAddress.ToByteArray()),
                        prevInfo.Serialize())
                    .SetState(
                        prevWeekly.address.Derive(inactiveAvatarAddress.ToByteArray()),
                        inactiveInfo.Serialize())
                    .SetState(
                        prevWeekly.address.Derive("address_list"),
                        List.Empty
                            .Add(avatarAddress.Serialize())
                            .Add(inactiveAvatarAddress.Serialize()));
            }

            Assert.False(prevWeekly.Ended);

            var action = new RewardGold();

            var ctx = new ActionContext()
            {
                BlockIndex = blockIndex,
                PreviousStates = _baseState,
                Miner = default,
            };

            var nextState = action.PrepareNextArena(ctx, state);
            var currentWeeklyState = nextState.GetWeeklyArenaState(prevWeeklyIndex);
            var preparedWeeklyState = nextState.GetWeeklyArenaState(targetWeeklyIndex);

            Assert.True(currentWeeklyState.Ended);
            Assert.True(
                nextState.TryGetState(
                    preparedWeeklyState.address.Derive(avatarAddress.ToByteArray()),
                    out Dictionary rawInfo
                )
            );

            var info = new ArenaInfo(rawInfo);

            Assert.Equal(GameConfig.ArenaChallengeCountMax, info.DailyChallengeCount);
            Assert.Equal(1000, info.Score);

            Assert.Equal(
                !filterInactive,
                nextState.TryGetState(
                    preparedWeeklyState.address.Derive(inactiveAvatarAddress.ToByteArray()),
                    out Dictionary inactiveRawInfo
                )
            );

            if (!filterInactive)
            {
                var inactiveInfo = new ArenaInfo(inactiveRawInfo);

                Assert.Equal(GameConfig.ArenaChallengeCountMax, inactiveInfo.DailyChallengeCount);
                Assert.Equal(1000, inactiveInfo.Score);
            }

            Assert.Empty(preparedWeeklyState.Map);
            Assert.True(
                nextState.TryGetState(
                    targetWeekly.address.Derive("address_list"),
                    out List rawList
                )
            );

            List<Address> addressList = rawList.ToList(StateExtensions.ToAddress);

            Assert.Contains(avatarAddress, addressList);
            Assert.Equal(!filterInactive, addressList.Contains(inactiveAvatarAddress));
            Assert.Equal(expectedCount, addressList.Count);
        }

        [Fact]
        public void ResetChallengeCount()
        {
            var legacyWeeklyIndex = RankingBattle11.UpdateTargetWeeklyArenaIndex - 1;
            var legacyWeekly = new WeeklyArenaState(legacyWeeklyIndex);
            legacyWeekly.Set(_avatarState, _tableSheets.CharacterSheet);
            legacyWeekly[_avatarState.address].Update(
                legacyWeekly[_avatarState.address],
                BattleLog.Result.Lose,
                ArenaScoreHelper.GetScore);

            Assert.Equal(4, legacyWeekly[_avatarState.address].DailyChallengeCount);

            var gameConfigState = new GameConfigState();
            gameConfigState.Set(_tableSheets.GameConfigSheet);
            var migratedWeekly = new WeeklyArenaState(legacyWeeklyIndex + 1);
            var state = _baseState
                .SetState(legacyWeekly.address, legacyWeekly.Serialize())
                .SetState(migratedWeekly.address, migratedWeekly.Serialize())
                .SetState(gameConfigState.address, gameConfigState.Serialize());

            Assert.False(legacyWeekly.Ended);

            var action = new RewardGold();

            var migrationCtx = new ActionContext
            {
                BlockIndex = RankingBattle11.UpdateTargetBlockIndex,
                PreviousStates = _baseState,
                Miner = default,
            };

            var arenaInfoAddress = migratedWeekly.address.Derive(_avatarState.address.ToByteArray());
            var addressListAddress = migratedWeekly.address.Derive("address_list");

            Assert.False(state.TryGetState(arenaInfoAddress, out Dictionary _));
            Assert.False(state.TryGetState(addressListAddress, out List _));

            // Ready to address list, ArenaInfo state.
            state = action.PrepareNextArena(migrationCtx, state);

            Assert.True(state.TryGetState(arenaInfoAddress, out Dictionary prevRawInfo));
            Assert.True(state.TryGetState(addressListAddress, out List _));

            var prevInfo = new ArenaInfo(prevRawInfo);
            prevInfo.Update(
                prevInfo,
                BattleLog.Result.Lose,
                ArenaScoreHelper.GetScore);

            Assert.Equal(4, prevInfo.DailyChallengeCount);

            var blockIndex = RankingBattle11.UpdateTargetBlockIndex + gameConfigState.DailyArenaInterval;

            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = state,
                Miner = default,
            };

            var nextState = action.ResetChallengeCount(ctx, state);

            Assert.True(state.TryGetState(arenaInfoAddress, out Dictionary rawInfo));
            Assert.True(state.TryGetState(addressListAddress, out List rawList));

            var updatedWeekly = nextState.GetWeeklyArenaState(migratedWeekly.address);
            var info = new ArenaInfo(rawInfo);
            List<Address> addressList = rawList.ToList(StateExtensions.ToAddress);

            Assert.Empty(updatedWeekly.Map);
            Assert.Equal(blockIndex, updatedWeekly.ResetIndex);
            Assert.Equal(5, info.DailyChallengeCount);
            Assert.Contains(_avatarState.address, addressList);
        }

        [Fact]
        public void GoldDistributedEachAccount()
        {
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            Currency currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            Address fund = GoldCurrencyState.Address;
            Address address1 = new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9");
            Address address2 = new Address("Fb90278C67f9b266eA309E6AE8463042f5461449");
            var action = new RewardGold();

            var ctx = new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = _baseState,
            };

            IAccountStateDelta delta;

            // 제너시스에 받아야 할 돈들 검사
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(currency * 99999000000, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 1000000, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 0, delta.GetBalance(address2, currency));

            // 1번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 1;
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(currency * 99999999900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 0, delta.GetBalance(address2, currency));

            // 3599번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 3599;
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(currency * 99999999900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 0, delta.GetBalance(address2, currency));

            // 3600번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 3600;
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(currency * 99999996900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 3000, delta.GetBalance(address2, currency));

            // 13600번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 13600;
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(currency * 99999996900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 3000, delta.GetBalance(address2, currency));

            // 13601번 블록에 받아야 할 것들 검사
            ctx.BlockIndex = 13601;
            delta = action.GenesisGoldDistribution(ctx, _baseState);
            Assert.Equal(currency * 99999999900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 0, delta.GetBalance(address2, currency));

            // Fund 잔액을 초과해서 송금하는 경우
            // EndBlock이 긴 순서대로 송금을 진행하기 때문에, 100이 송금 성공하고 10억이 송금 실패한다.
            ctx.BlockIndex = 2;
            Assert.Throws<InsufficientBalanceException>(() =>
            {
                delta = action.GenesisGoldDistribution(ctx, _baseState);
            });
            Assert.Equal(currency * 99999999900, delta.GetBalance(fund, currency));
            Assert.Equal(currency * 100, delta.GetBalance(address1, currency));
            Assert.Equal(currency * 0, delta.GetBalance(address2, currency));
        }

        [Fact]
        public void MiningReward()
        {
            Address miner = new Address("F9A15F870701268Bd7bBeA6502eB15F4997f32f9");
            Currency currency = _baseState.GetGoldCurrency();
            var ctx = new ActionContext()
            {
                BlockIndex = 0,
                PreviousStates = _baseState,
                Miner = miner,
            };

            var action = new RewardGold();

            void AssertMinerReward(int blockIndex, string expected)
            {
                ctx.BlockIndex = blockIndex;
                IAccountStateDelta delta = action.MinerReward(ctx, _baseState);
                Assert.Equal(FungibleAssetValue.Parse(currency, expected), delta.GetBalance(miner, currency));
            }

            // Before halving (10 / 2^0 = 10)
            AssertMinerReward(0, "10");
            AssertMinerReward(1, "10");
            AssertMinerReward(12614400, "10");

            // First halving (10 / 2^1 = 5)
            AssertMinerReward(12614401, "5");
            AssertMinerReward(25228800, "5");

            // Second halving (10 / 2^2 = 2.5)
            AssertMinerReward(25228801, "2.5");
            AssertMinerReward(37843200, "2.5");

            // Third halving (10 / 2^3 = 1.25)
            AssertMinerReward(37843201, "1.25");
            AssertMinerReward(50457600, "1.25");

            // Rewardless era
            AssertMinerReward(50457601, "0");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Genesis_StateRootHash(bool mainnet)
        {
            BlockPolicySource blockPolicySource = new BlockPolicySource(Logger.None);
            StagePolicy stagePolicy = new StagePolicy(default, 2);
            IBlockPolicy<PolymorphicAction<ActionBase>> policy = blockPolicySource.GetPolicy();
            Block<PolymorphicAction<ActionBase>> genesis;
            if (mainnet)
            {
                const string genesisBlockPath = "https://release.nine-chronicles.com/genesis-block-9c-main";
                var uri = new Uri(genesisBlockPath);
                using var client = new HttpClient();
                var rawBlock = await client.GetByteArrayAsync(uri);
                var blockDict = (Bencodex.Types.Dictionary)new Codec().Decode(rawBlock);
                genesis = BlockMarshaler.UnmarshalBlock<PolymorphicAction<ActionBase>>(blockDict);
            }
            else
            {
                var adminPrivateKey = new PrivateKey();
                var adminAddress = adminPrivateKey.ToAddress();
                var activatedAccounts = ImmutableHashSet<Address>.Empty;
                var nonce = new byte[] { 0x00, 0x01, 0x02, 0x03 };
                var privateKey = new PrivateKey();
                (ActivationKey activationKey, PendingActivationState pendingActivation) =
                    ActivationKey.Create(privateKey, nonce);
                var pendingActivationStates = new List<PendingActivationState>
                {
                    pendingActivation,
                };
                var initializeStates = new InitializeStates(
                    rankingState: new RankingState0(),
                    shopState: new ShopState(),
                    gameConfigState: new GameConfigState(),
                    redeemCodeState: new RedeemCodeState(Bencodex.Types.Dictionary.Empty
                        .Add("address", RedeemCodeState.Address.Serialize())
                        .Add("map", Bencodex.Types.Dictionary.Empty)
                    ),
                    adminAddressState: new AdminState(adminAddress, 1500000),
                    activatedAccountsState: new ActivatedAccountsState(activatedAccounts),
#pragma warning disable CS0618
                    // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
                    goldCurrencyState: new GoldCurrencyState(Currency.Legacy("NCG", 2, null)),
#pragma warning restore CS0618
                    goldDistributions: new GoldDistribution[0],
                    tableSheets: TableSheetsImporter.ImportSheets(),
                    pendingActivationStates: pendingActivationStates.ToArray()
                );
                genesis = BlockChain<PolymorphicAction<ActionBase>>.MakeGenesisBlock(
                    new PolymorphicAction<ActionBase>[] { initializeStates }
                );
            }

            var store = new DefaultStore(null);
            var stateStore = new TrieStateStore(new DefaultKeyValueStore(null));
            var blockChain = new BlockChain<PolymorphicAction<ActionBase>>(
                policy: policy,
                store: store,
                stagePolicy: stagePolicy,
                stateStore: stateStore,
                genesisBlock: genesis,
                renderers: blockPolicySource.GetRenderers()
            );
            Assert.Equal(genesis.StateRootHash, blockChain.Genesis.StateRootHash);
        }
    }
}
