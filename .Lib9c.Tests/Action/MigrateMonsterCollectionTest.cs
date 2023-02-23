namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.BlockChain.Policy;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class MigrateMonsterCollectionTest
    {
        private readonly Address _signer;
        private readonly Address _avatarAddress;
        private readonly IAccountStateDelta _state;

        public MigrateMonsterCollectionTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _signer = default;
            _avatarAddress = _signer.Derive("avatar");
            _state = new State();
            Dictionary<string, string> sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var rankingMapAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_signer);
            var avatarState = new AvatarState(
                _avatarAddress,
                _signer,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress);
            agentState.avatarAddresses[0] = _avatarAddress;

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(currency);

            _state = _state
                .SetState(_signer, agentState.Serialize())
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

            foreach ((string key, string value) in sheets)
            {
                _state = _state
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Fact]
        public void Execute_ThrowsIfAlreadyStakeStateExists()
        {
            Address monsterCollectionAddress = MonsterCollectionState.DeriveAddress(_signer, 0);
            var monsterCollectionState = new MonsterCollectionState(
                monsterCollectionAddress, 1, 0);
            Address stakeStateAddress = StakeState.DeriveAddress(_signer);
            var states = _state.SetState(
                    stakeStateAddress, new StakeState(stakeStateAddress, 0).SerializeV2())
                .SetState(monsterCollectionAddress, monsterCollectionState.SerializeV2());
            MigrateMonsterCollection action = new MigrateMonsterCollection(_avatarAddress);
            Assert.Throws<InvalidOperationException>(() => action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signer,
                BlockIndex = 0,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_After_V100220ObsoleteIndex()
        {
            Address monsterCollectionAddress = MonsterCollectionState.DeriveAddress(_signer, 0);
            var monsterCollectionState = new MonsterCollectionState(
                monsterCollectionAddress, 1, ActionObsoleteConfig.V100220ObsoleteIndex - MonsterCollectionState.RewardInterval);
            var currency = _state.GetGoldCurrency();
            var states = _state
                .SetState(monsterCollectionAddress, monsterCollectionState.Serialize())
                .MintAsset(monsterCollectionAddress, currency * 100);
            MigrateMonsterCollection action = new MigrateMonsterCollection(_avatarAddress);
            states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signer,
                BlockIndex = ActionObsoleteConfig.V100220ObsoleteIndex + 1,
                Random = new TestRandom(),
            });

            Assert.True(states.TryGetAvatarStateV2(
                _signer,
                _avatarAddress,
                out AvatarState avatarState,
                out bool _));

            Assert.Equal(80, avatarState.inventory.Items.First(item => item.item.Id == 400000).count);
            Assert.Equal(1, avatarState.inventory.Items.First(item => item.item.Id == 500000).count);
        }

        [Theory]
        [ClassData(typeof(ExecuteFixture))]
        public void Execute(int collectionLevel, long claimBlockIndex, long receivedBlockIndex, long stakedAmount, (int ItemId, int Quantity)[] expectedItems)
        {
            Address collectionAddress = MonsterCollectionState.DeriveAddress(_signer, 0);
            var monsterCollectionState = new MonsterCollectionState(collectionAddress, collectionLevel, 0);
            Currency currency = _state.GetGoldCurrency();

            var states = _state
                .SetState(collectionAddress, monsterCollectionState.Serialize())
                .MintAsset(monsterCollectionState.address, stakedAmount * currency);

            Assert.Equal(0, states.GetAgentState(_signer).MonsterCollectionRound);
            Assert.Equal(0 * currency, states.GetBalance(_signer, currency));
            Assert.Equal(stakedAmount * currency, states.GetBalance(collectionAddress, currency));

            MigrateMonsterCollection action = new MigrateMonsterCollection(_avatarAddress);
            states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = _signer,
                BlockIndex = claimBlockIndex,
                Random = new TestRandom(),
            });

            Assert.True(states.TryGetStakeState(_signer, out StakeState stakeState));
            Assert.Equal(
                0 * currency,
                states.GetBalance(monsterCollectionState.address, currency));
            Assert.Equal(stakedAmount * currency, states.GetBalance(stakeState.address, currency));
            Assert.Equal(receivedBlockIndex, stakeState.ReceivedBlockIndex);
            Assert.Equal(monsterCollectionState.StartedBlockIndex, stakeState.StartedBlockIndex);
            Assert.True(
                states.TryGetAvatarStateV2(
                    _signer,
                    _avatarAddress,
                    out AvatarState avatarState,
                    out bool _));
            foreach (var (itemId, quantity) in expectedItems)
            {
                Assert.True(avatarState.inventory.HasItem(itemId, quantity));
            }
        }

        [Fact]
        public void Serialization()
        {
            var action = new MigrateMonsterCollection(_avatarAddress);
            var deserialized = new MigrateMonsterCollection();
            deserialized.LoadPlainValue(action.PlainValue);
            Assert.Equal(action.PlainValue, deserialized.PlainValue);
        }

        private class ExecuteFixture : IEnumerable<object[]>
        {
            private readonly List<object[]> _data = new List<object[]>
            {
                new object[]
                {
                    1,
                    MonsterCollectionState.RewardInterval,
                    MonsterCollectionState.RewardInterval,
                    500,
                    new (int, int)[]
                    {
                        (400000, 80),
                        (500000, 1),
                    },
                },
                new object[]
                {
                    2,
                    MonsterCollectionState.RewardInterval,
                    MonsterCollectionState.RewardInterval,
                    2300,
                    new (int, int)[]
                    {
                        (400000, 265),
                        (500000, 2),
                    },
                },
                new object[]
                {
                    3,
                    MonsterCollectionState.RewardInterval,
                    MonsterCollectionState.RewardInterval,
                    9500,
                    new (int, int)[]
                    {
                        (400000, 1265),
                        (500000, 5),
                    },
                },
                new object[]
                {
                    4,
                    MonsterCollectionState.RewardInterval,
                    MonsterCollectionState.RewardInterval,
                    63500,
                    new (int, int)[]
                    {
                        (400000, 8465),
                        (500000, 31),
                    },
                },
                new object[]
                {
                    5,
                    MonsterCollectionState.RewardInterval,
                    MonsterCollectionState.RewardInterval,
                    333500,
                    new (int, int)[]
                    {
                        (400000, 45965),
                        (500000, 161),
                    },
                },
                new object[]
                {
                    6,
                    MonsterCollectionState.RewardInterval,
                    MonsterCollectionState.RewardInterval,
                    813500,
                    new (int, int)[]
                    {
                        (400000, 120965),
                        (500000, 361),
                    },
                },
                new object[]
                {
                    7,
                    MonsterCollectionState.RewardInterval,
                    MonsterCollectionState.RewardInterval,
                    2313500,
                    new (int, int)[]
                    {
                        (400000, 350965),
                        (500000, 1121),
                    },
                },
                new object[]
                {
                    7,
                    MonsterCollectionState.RewardInterval - 1,
                    0,  // Because it cannot claim rewards.
                    2313500,
                    new (int, int)[]
                    {
                        (400000, 0),
                        (500000, 0),
                    },
                },
            };

            public IEnumerator<object[]> GetEnumerator() => _data.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => _data.GetEnumerator();
        }
    }
}
