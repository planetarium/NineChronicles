namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class StakingRewardTest
    {
        private readonly Address _signer;
        private readonly Address _avatarAddress;
        private readonly TableSheets _tableSheets;
        private IAccountStateDelta _state;

        public StakingRewardTest()
        {
            _signer = default;
            _avatarAddress = _signer.Derive("avatar");
            _state = new State();
            Dictionary<string, string> sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
            var rankingMapAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_signer);
            var avatarState = new AvatarState(
                _avatarAddress,
                _signer,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress);
            agentState.avatarAddresses[0] = _avatarAddress;

            var currency = new Currency("NCG", 2, minters: null);
            var goldCurrencyState = new GoldCurrencyState(currency);

            _state = _state
                .SetState(_signer, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

            foreach ((string key, string value) in sheets)
            {
                _state = _state
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        [InlineData(1, 0, 1)]
        [InlineData(2, 0, 2)]
        [InlineData(2, 1, 3)]
        [InlineData(3, 0, 4)]
        [InlineData(3, 1, 5)]
        [InlineData(3, 2, 6)]
        [InlineData(4, 0, 7)]
        [InlineData(4, 1, 4)]
        [InlineData(4, 2, 5)]
        [InlineData(4, 3, 6)]
        public void Execute(int rewardLevel, int prevRewardLevel, int stakingLevel)
        {
            Address stakingAddress = StakingState.DeriveAddress(_signer, 0);
            StakingState stakingState = new StakingState(stakingAddress, stakingLevel, 0);
            for (int i = 0; i < prevRewardLevel; i++)
            {
                int level = i + 1;
                List<StakingRewardSheet.RewardInfo> rewards = _tableSheets.StakingRewardSheet[level].Rewards;
                StakingState.Result result = new StakingState.Result(_avatarAddress, rewards);
                stakingState.UpdateRewardMap(i + 1, result, 0);
            }

            Currency currency = _state.GetGoldCurrency();
            int stakingRound = _state.GetAgentState(_signer).StakingRound;

            _state = _state
                .SetState(stakingAddress, stakingState.Serialize());

            FungibleAssetValue balance = 0 * currency;
            if (rewardLevel == 4)
            {
                foreach (var row in _tableSheets.StakingSheet)
                {
                    if (row.Level <= stakingLevel)
                    {
                        balance += row.RequiredGold * currency;
                    }
                }

                stakingRound += 1;
                _state = _state
                    .MintAsset(stakingAddress, balance);
            }

            Assert.Equal(prevRewardLevel, stakingState.RewardLevel);
            Assert.Equal(0, _state.GetAgentState(_signer).StakingRound);

            StakingReward action = new StakingReward
            {
                avatarAddress = _avatarAddress,
                stakingRound = 0,
            };

            IAccountStateDelta nextState = action.Execute(new ActionContext
            {
                PreviousStates = _state,
                Signer = _signer,
                BlockIndex = rewardLevel * StakingState.RewardInterval,
            });

            StakingState nextStakingState = new StakingState((Dictionary)nextState.GetState(stakingAddress));
            Assert.Equal(rewardLevel, nextStakingState.RewardLevel);

            AvatarState nextAvatarState = nextState.GetAvatarState(_avatarAddress);

            for (int i = 0; i < nextStakingState.RewardLevel; i++)
            {
                int level = i + 1;
                List<StakingRewardSheet.RewardInfo> rewardInfos = _tableSheets.StakingRewardSheet[level].Rewards;
                if (level > stakingLevel)
                {
                    foreach (var rewardInfo in rewardInfos)
                    {
                        Assert.True(nextAvatarState.inventory.HasItem(rewardInfo.ItemId, rewardInfo.Quantity * rewardLevel));
                    }
                }

                Assert.Contains(level, nextStakingState.RewardMap.Keys);
                Assert.Equal(_avatarAddress, nextStakingState.RewardMap[level].avatarAddress);
                Assert.Equal(rewardInfos, nextStakingState.RewardMap[level].rewards);
            }

            Assert.Equal(0 * currency, nextState.GetBalance(stakingAddress, currency));
            Assert.Equal(balance, nextState.GetBalance(_signer, currency));
            Assert.Equal(stakingRound, nextState.GetAgentState(_signer).StakingRound);
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException_AgentState()
        {
            StakingReward action = new StakingReward
            {
                avatarAddress = _avatarAddress,
                stakingRound = 0,
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext
                {
                    PreviousStates = _state,
                    Signer = new PrivateKey().ToAddress(),
                    BlockIndex = 0,
                })
            );
        }

        [Fact]
        public void Execute_Throw_FailedLoadStateException_StakingState()
        {
            StakingReward action = new StakingReward
            {
                avatarAddress = _avatarAddress,
                stakingRound = 1,
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext
                {
                    PreviousStates = _state,
                    Signer = _signer,
                    BlockIndex = 0,
                })
            );
        }

        [Fact]
        public void Execute_Throw_StakingExpiredException()
        {
            Address stakingAddress = StakingState.DeriveAddress(_signer, 0);
            StakingState stakingState = new StakingState(stakingAddress, 1, 0);
            List<StakingRewardSheet.RewardInfo> rewards = _tableSheets.StakingRewardSheet[4].Rewards;
            StakingState.Result result = new StakingState.Result(_avatarAddress, rewards);
            stakingState.UpdateRewardMap(4, result, 0);
            _state = _state.SetState(stakingAddress, stakingState.Serialize());

            StakingReward action = new StakingReward
            {
                avatarAddress = _avatarAddress,
                stakingRound = 0,
            };

            Assert.Throws<StakingExpiredException>(() => action.Execute(new ActionContext
                {
                    PreviousStates = _state,
                    Signer = _signer,
                    BlockIndex = 0,
                })
            );
        }

        [Theory]
        [InlineData(0, -1)]
        [InlineData(0, StakingState.RewardInterval - 1)]
        public void Execute_Throw_RequiredBlockIndexException(long startedBlockIndex, long blockIndex)
        {
            Address stakingAddress = StakingState.DeriveAddress(_signer, 0);
            StakingState stakingState = new StakingState(stakingAddress, 1, startedBlockIndex);
            List<StakingRewardSheet.RewardInfo> rewards = _tableSheets.StakingRewardSheet[1].Rewards;
            StakingState.Result result = new StakingState.Result(_avatarAddress, rewards);
            stakingState.UpdateRewardMap(1, result, 0);

            _state = _state.SetState(stakingAddress, stakingState.Serialize());

            StakingReward action = new StakingReward
            {
                avatarAddress = _avatarAddress,
                stakingRound = 0,
            };

            Assert.Throws<RequiredBlockIndexException>(() => action.Execute(new ActionContext
                {
                    PreviousStates = _state,
                    Signer = _signer,
                    BlockIndex = blockIndex,
                })
            );
        }

        [Fact]
        public void Execute_Throw_InsufficientBalanceException()
        {
            Address stakingAddress = StakingState.DeriveAddress(_signer, 0);
            StakingState stakingState = new StakingState(stakingAddress, 1, 0);

            _state = _state.SetState(stakingAddress, stakingState.Serialize());

            StakingReward action = new StakingReward
            {
                avatarAddress = _avatarAddress,
                stakingRound = 0,
            };

            Assert.Throws<InsufficientBalanceException>(() => action.Execute(new ActionContext
                {
                    PreviousStates = _state,
                    Signer = _signer,
                    BlockIndex = StakingState.ExpirationIndex,
                })
            );
        }
    }
}
