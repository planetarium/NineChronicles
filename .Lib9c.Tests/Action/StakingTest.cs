namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class StakingTest
    {
        private readonly TableSheets _tableSheets;
        private readonly Address _signer;
        private IAccountStateDelta _initialState;

        public StakingTest()
        {
            Dictionary<string, string> sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
            _signer = default;
            var currency = new Currency("NCG", 2, minters: null);
            var goldCurrencyState = new GoldCurrencyState(currency);
            _initialState = new State()
                .SetState(Addresses.GoldCurrency, goldCurrencyState.Serialize());
            foreach ((string key, string value) in sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize())
                    .SetState(_signer, new AgentState(_signer).Serialize());
            }
        }

        [Theory]
        [InlineData(true, 2, 1, 1, 1)]
        [InlineData(true, 5, 2, 2, 40000)]
        [InlineData(false, 1, 3, 0, 120000)]
        [InlineData(false, 3, 4, 0, 160000)]
        public void Execute(bool exist, int level, int stakingRound, int prevLevel, long blockIndex)
        {
            Address stakingAddress = StakingState.DeriveAddress(_signer, stakingRound);
            if (exist)
            {
                List<StakingRewardSheet.RewardInfo> rewards = _tableSheets.StakingRewardSheet[prevLevel].Rewards;
                StakingState prevStakingState = new StakingState(stakingAddress, prevLevel, 0, _tableSheets.StakingRewardSheet);
                _initialState = _initialState.SetState(stakingAddress, prevStakingState.Serialize());
                Assert.All(prevStakingState.RewardLevelMap, kv => Assert.Equal(rewards, kv.Value));
            }

            AgentState prevAgentState = _initialState.GetAgentState(_signer);
            while (prevAgentState.StakingRound < stakingRound)
            {
                prevAgentState.IncreaseStakingRound();
            }

            _initialState = _initialState.SetState(_signer, prevAgentState.Serialize());

            Currency currency = _initialState.GetGoldCurrency();

            for (int i = 1; i < level + 1; i++)
            {
                if (i > prevLevel)
                {
                    StakingSheet.Row row = _tableSheets.StakingSheet[i];
                    _initialState = _initialState.MintAsset(_signer, row.RequiredGold * currency);
                }
            }

            Staking action = new Staking
            {
                level = level,
                stakingRound = stakingRound,
            };

            IAccountStateDelta nextState = action.Execute(new ActionContext
            {
                PreviousStates = _initialState,
                Signer = _signer,
                BlockIndex = blockIndex,
            });

            StakingState nextStakingState = new StakingState((Dictionary)nextState.GetState(stakingAddress));
            AgentState nextAgentState = nextState.GetAgentState(_signer);
            Assert.Equal(level, nextStakingState.Level);
            Assert.Equal(0 * currency, nextState.GetBalance(_signer, currency));
            Assert.Equal(stakingRound, nextAgentState.StakingRound);
            long rewardLevel = nextStakingState.GetRewardLevel(blockIndex);
            for (long i = rewardLevel; i < 4; i++)
            {
                List<StakingRewardSheet.RewardInfo> expected = _tableSheets.StakingRewardSheet[level].Rewards;
                Assert.Equal(expected, nextStakingState.RewardLevelMap[i + 1]);
            }
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException()
        {
            Staking action = new Staking
            {
                level = 1,
                stakingRound = 1,
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext
            {
                PreviousStates = new State(),
                Signer = _signer,
                BlockIndex = 1,
            }));
        }

        [Theory]
        [InlineData(2, 1)]
        [InlineData(1, 2)]
        public void Execute_Throw_InvalidStakingRoundException(int agentStakingRound, int stakingRound)
        {
            AgentState prevAgentState = _initialState.GetAgentState(_signer);
            while (prevAgentState.StakingRound < agentStakingRound)
            {
                prevAgentState.IncreaseStakingRound();
            }

            _initialState = _initialState.SetState(_signer, prevAgentState.Serialize());

            Staking action = new Staking
            {
                level = 1,
                stakingRound = stakingRound,
            };

            Assert.Throws<InvalidStakingRoundException>(() => action.Execute(new ActionContext
            {
                PreviousStates = _initialState,
                Signer = _signer,
                BlockIndex = 1,
            }));
        }

        [Fact]
        public void Execute_Throw_SheetRowNotFoundException()
        {
            int level = 100;

            Assert.False(_tableSheets.StakingSheet.Keys.Contains(level));

            Staking action = new Staking
            {
                level = level,
                stakingRound = 0,
            };

            Assert.Throws<SheetRowNotFoundException>(() => action.Execute(new ActionContext
            {
                PreviousStates = _initialState,
                Signer = _signer,
                BlockIndex = 1,
            }));
        }

        [Fact]
        public void Execute_Throw_InsufficientBalanceException()
        {
            Staking action = new Staking
            {
                level = 1,
            };

            Assert.Throws<InsufficientBalanceException>(() => action.Execute(new ActionContext
            {
                PreviousStates = _initialState,
                Signer = _signer,
                BlockIndex = 1,
            }));
        }

        [Fact]
        public void Execute_Throw_StakingExpiredException()
        {
            Address stakingAddress = StakingState.DeriveAddress(_signer, 0);
            StakingState prevStakingState = new StakingState(stakingAddress, 1, 0, _tableSheets.StakingRewardSheet);
            Assert.Equal(StakingState.ExpirationIndex, prevStakingState.ExpiredBlockIndex);

            _initialState = _initialState.SetState(stakingAddress, prevStakingState.Serialize());

            Staking action = new Staking
            {
                level = 2,
                stakingRound = 0,
            };

            Assert.Throws<StakingExpiredException>(() => action.Execute(new ActionContext
            {
                PreviousStates = _initialState,
                Signer = _signer,
                BlockIndex = prevStakingState.ExpiredBlockIndex + 1,
            }));
        }

        [Theory]
        [InlineData(2, 1)]
        [InlineData(2, 2)]
        public void Execute_Throw_InvalidLevelException(int prevLevel, int level)
        {
            Address stakingAddress = StakingState.DeriveAddress(_signer, 0);
            StakingState prevStakingState = new StakingState(stakingAddress, prevLevel, 0, _tableSheets.StakingRewardSheet);
            _initialState = _initialState.SetState(stakingAddress, prevStakingState.Serialize());

            Staking action = new Staking
            {
                level = level,
                stakingRound = 0,
            };

            Assert.Throws<InvalidLevelException>(() => action.Execute(new ActionContext
            {
                PreviousStates = _initialState,
                Signer = _signer,
                BlockIndex = 1,
            }));
        }

        [Fact]
        public void Rehearsal()
        {
            Address stakingAddress = StakingState.DeriveAddress(_signer, 1);
            Staking action = new Staking
            {
                level = 1,
                stakingRound = 1,
            };
            IAccountStateDelta nextState = action.Execute(new ActionContext
            {
                PreviousStates = new State(),
                Signer = _signer,
                Rehearsal = true,
            });

            List<Address> updatedAddresses = new List<Address>()
            {
                _signer,
                stakingAddress,
            };

            Assert.Equal(updatedAddresses.ToImmutableHashSet(), nextState.UpdatedAddresses);
        }
    }
}
