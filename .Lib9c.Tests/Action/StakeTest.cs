namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using Bencodex.Types;
    using Lib9c.Tests.Fixtures.TableCSV.Stake;
    using Lib9c.Tests.Util;
    using Libplanet.Action;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Exceptions;
    using Nekoyume.Model.Stake;
    using Nekoyume.Model.State;
    using Nekoyume.TableData.Stake;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class StakeTest
    {
        private readonly IAccount _initialState;
        private readonly Currency _ncg;
        private readonly Address _agentAddr;
        private readonly StakePolicySheet _stakePolicySheet;

        public StakeTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            var sheetsOverride = new Dictionary<string, string>
            {
                {
                    "StakeRegularFixedRewardSheet_V1",
                    StakeRegularFixedRewardSheetFixtures.V1
                },
                {
                    "StakeRegularFixedRewardSheet_V2",
                    StakeRegularFixedRewardSheetFixtures.V2
                },
                {
                    "StakeRegularRewardSheet_V1",
                    StakeRegularRewardSheetFixtures.V1
                },
                {
                    "StakeRegularRewardSheet_V2",
                    StakeRegularRewardSheetFixtures.V2
                },
                {
                    nameof(StakePolicySheet),
                    StakePolicySheetFixtures.V2
                },
            };
            (
                _,
                _agentAddr,
                _,
                _,
                _initialState
            ) = InitializeUtil.InitializeStates(sheetsOverride: sheetsOverride);
            _ncg = _initialState.GetGoldCurrency();
            _stakePolicySheet = _initialState.GetSheet<StakePolicySheet>();
        }

        [Theory]
        [InlineData(long.MinValue, false)]
        [InlineData(0, true)]
        [InlineData(long.MaxValue, true)]
        public void Constructor(long amount, bool success)
        {
            if (success)
            {
                var stake = new Stake(amount);
            }
            else
            {
                Assert.Throws<ArgumentOutOfRangeException>(() => new Stake(amount));
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(long.MaxValue)]
        public void Serialization(long amount)
        {
            var action = new Stake(amount);
            var ser = action.PlainValue;
            var de = new Stake();
            de.LoadPlainValue(ser);
            Assert.Equal(action.Amount, de.Amount);
            Assert.Equal(ser, de.PlainValue);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(9)] // NOTE: 9 is just a random number.
        public void Execute_Throw_MonsterCollectionExistingException(
            int monsterCollectionRound)
        {
            var previousState = _initialState;
            var agentState = previousState.GetAgentState(_agentAddr);
            if (monsterCollectionRound > 0)
            {
                for (var i = 0; i < monsterCollectionRound; i++)
                {
                    agentState.IncreaseCollectionRound();
                }

                previousState = previousState
                    .SetState(_agentAddr, agentState.Serialize());
            }

            var monsterCollectionAddr =
                MonsterCollectionState.DeriveAddress(_agentAddr, monsterCollectionRound);
            var monsterCollectionState = new MonsterCollectionState(
                monsterCollectionAddr,
                1,
                0);
            previousState = previousState
                .SetState(monsterCollectionAddr, monsterCollectionState.SerializeV2());
            Assert.Throws<MonsterCollectionExistingException>(() =>
                Execute(
                    0,
                    previousState,
                    new TestRandom(),
                    _agentAddr,
                    0));
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(long.MinValue)]
        public void Execute_Throw_ArgumentOutOfRangeException_Via_Negative_Amount(long amount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Execute(
                    0,
                    _initialState,
                    new TestRandom(),
                    _agentAddr,
                    amount));
        }

        [Theory]
        [InlineData(nameof(StakePolicySheet))]
        // NOTE: StakePolicySheet in _initialState has V2.
        [InlineData("StakeRegularFixedRewardSheet_V2")]
        // NOTE: StakePolicySheet in _initialState has V2.
        [InlineData("StakeRegularRewardSheet_V2")]
        public void Execute_Throw_StateNullException_Via_Sheet(string sheetName)
        {
            var previousState = _initialState.SetState(
                Addresses.GetSheetAddress(sheetName),
                Null.Value);
            Assert.Throws<StateNullException>(() =>
                Execute(
                    0,
                    previousState,
                    new TestRandom(),
                    _agentAddr,
                    0));
        }

        [Theory]
        // NOTE: minimum required_gold of StakeRegularRewardSheetFixtures.V2 is 50.
        [InlineData(49)]
        [InlineData(1)]
        public void Execute_Throw_ArgumentOutOfRangeException_Via_Minimum_Amount(long amount)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                Execute(
                    0,
                    _initialState,
                    new TestRandom(),
                    _agentAddr,
                    amount));
        }

        [Theory]
        // NOTE: minimum required_gold of StakeRegularRewardSheetFixtures.V2 is 50.
        [InlineData(0, 50)]
        [InlineData(49, 50)]
        [InlineData(long.MaxValue - 1, long.MaxValue)]
        public void Execute_Throws_NotEnoughFungibleAssetValueException(
            long balance,
            long amount)
        {
            var previousState = _initialState;
            if (balance > 0)
            {
                previousState = _initialState.MintAsset(
                    new ActionContext { Signer = Addresses.Admin },
                    _agentAddr,
                    _ncg * balance);
            }

            Assert.Throws<NotEnoughFungibleAssetValueException>(() =>
                Execute(
                    0,
                    previousState,
                    new TestRandom(),
                    _agentAddr,
                    amount));
        }

        [Fact]
        public void Execute_Throw_StateNullException_Via_0_Amount()
        {
            Assert.Throws<StateNullException>(() =>
                Execute(
                    0,
                    _initialState,
                    new TestRandom(),
                    _agentAddr,
                    0));
        }

        [Theory]
        // NOTE: minimum required_gold of StakeRegularRewardSheetFixtures.V2 is 50.
        [InlineData(0, 50, StakeState.RewardInterval)]
        [InlineData(
            long.MaxValue - StakeState.RewardInterval,
            long.MaxValue,
            long.MaxValue)]
        public void Execute_Throw_StakeExistingClaimableException_With_StakeState(
            long previousStartedBlockIndex,
            long previousAmount,
            long blockIndex)
        {
            var stakeStateAddr = StakeState.DeriveAddress(_agentAddr);
            var stakeState = new StakeState(
                address: stakeStateAddr,
                startedBlockIndex: previousStartedBlockIndex);
            Assert.True(stakeState.IsClaimable(blockIndex));
            var previousState = _initialState
                .MintAsset(
                    new ActionContext { Signer = Addresses.Admin },
                    stakeStateAddr,
                    _ncg * previousAmount)
                .SetState(stakeStateAddr, stakeState.Serialize());
            Assert.Throws<StakeExistingClaimableException>(() =>
                Execute(
                    blockIndex,
                    previousState,
                    new TestRandom(),
                    _agentAddr,
                    previousAmount));
        }

        [Theory]
        // NOTE: minimum required_gold of StakeRegularRewardSheetFixtures.V2 is 50.
        // NOTE: RewardInterval of StakePolicySheetFixtures.V2 is 50,400.
        [InlineData(0, 50, 50400)]
        [InlineData(
            long.MaxValue - 50400,
            long.MaxValue,
            long.MaxValue)]
        public void Execute_Throw_StakeExistingClaimableException_With_StakeStateV2(
            long previousStartedBlockIndex,
            long previousAmount,
            long blockIndex)
        {
            var stakeStateAddr = StakeStateV2.DeriveAddress(_agentAddr);
            var stakeStateV2 = new StakeStateV2(
                contract: new Contract(_stakePolicySheet),
                startedBlockIndex: previousStartedBlockIndex);
            var previousState = _initialState
                .MintAsset(
                    new ActionContext { Signer = Addresses.Admin },
                    stakeStateAddr,
                    _ncg * previousAmount)
                .SetState(stakeStateAddr, stakeStateV2.Serialize());
            Assert.Throws<StakeExistingClaimableException>(() =>
                Execute(
                    blockIndex,
                    previousState,
                    new TestRandom(),
                    _agentAddr,
                    previousAmount));
        }

        [Theory]
        // NOTE: minimum required_gold of StakeRegularRewardSheetFixtures.V2 is 50.
        // NOTE: LockupInterval of StakePolicySheetFixtures.V2 is 201,600.
        [InlineData(0, 50 + 1, 201_600 - 1, 50)]
        [InlineData(
            long.MaxValue - 201_600,
            50 + 1,
            long.MaxValue - 1,
            50)]
        public void
            Execute_Throw_RequiredBlockIndexException_Via_Reduced_Amount_When_Lucked_Up_With_StakeState(
                long previousStartedBlockIndex,
                long previousAmount,
                long blockIndex,
                long reducedAmount)
        {
            var stakeStateAddr = StakeState.DeriveAddress(_agentAddr);
            var stakeState = new StakeState(
                address: stakeStateAddr,
                startedBlockIndex: previousStartedBlockIndex);
            Assert.False(stakeState.IsCancellable(blockIndex));
            stakeState.Claim(blockIndex);
            Assert.False(stakeState.IsClaimable(blockIndex));
            var previousState = _initialState
                .MintAsset(
                    new ActionContext { Signer = Addresses.Admin },
                    stakeStateAddr,
                    _ncg * previousAmount)
                .SetState(stakeStateAddr, stakeState.Serialize());
            Assert.Throws<RequiredBlockIndexException>(() =>
                Execute(
                    blockIndex,
                    previousState,
                    new TestRandom(),
                    _agentAddr,
                    reducedAmount));
        }

        [Theory]
        // NOTE: minimum required_gold of StakeRegularRewardSheetFixtures.V2 is 50.
        // NOTE: LockupInterval of StakePolicySheetFixtures.V2 is 201,600.
        [InlineData(0, 50 + 1, 201_600 - 1, 50)]
        [InlineData(
            long.MaxValue - 201_600,
            50 + 1,
            long.MaxValue - 1,
            50)]
        public void
            Execute_Throw_RequiredBlockIndexException_Via_Reduced_Amount_When_Locked_Up_With_StakeStateV2(
                long previousStartedBlockIndex,
                long previousAmount,
                long blockIndex,
                long reducedAmount)
        {
            var stakeStateAddr = StakeStateV2.DeriveAddress(_agentAddr);
            var stakeStateV2 = new StakeStateV2(
                contract: new Contract(_stakePolicySheet),
                startedBlockIndex: previousStartedBlockIndex,
                receivedBlockIndex: blockIndex);
            var previousState = _initialState
                .MintAsset(
                    new ActionContext { Signer = Addresses.Admin },
                    stakeStateAddr,
                    _ncg * previousAmount)
                .SetState(stakeStateAddr, stakeStateV2.Serialize());
            Assert.Throws<RequiredBlockIndexException>(() =>
                Execute(
                    blockIndex,
                    previousState,
                    new TestRandom(),
                    _agentAddr,
                    reducedAmount));
        }

        [Theory]
        // NOTE: minimum required_gold of StakeRegularRewardSheetFixtures.V2 is 50.
        [InlineData(50)]
        [InlineData(long.MaxValue)]
        public void Execute_Success_When_Staking_State_Null(long amount)
        {
            var previousState = _initialState.MintAsset(
                new ActionContext { Signer = Addresses.Admin },
                _agentAddr,
                _ncg * amount);
            Execute(
                0,
                previousState,
                new TestRandom(),
                _agentAddr,
                amount);
        }

        [Theory]
        // NOTE: minimum required_gold of StakeRegularRewardSheetFixtures.V2 is 50.
        // NOTE: non claimable, locked up, same amount.
        [InlineData(0, 50, 0, 50)]
        [InlineData(0, 50, StakeState.LockupInterval - 1, 50)]
        [InlineData(0, long.MaxValue, 0, long.MaxValue)]
        [InlineData(0, long.MaxValue, StakeState.LockupInterval - 1, long.MaxValue)]
        // NOTE: non claimable, locked up, increased amount.
        [InlineData(0, 50, 0, 500)]
        [InlineData(0, 50, StakeState.LockupInterval - 1, 500)]
        // NOTE: non claimable, unlocked, same amount.
        [InlineData(0, 50, StakeState.LockupInterval, 50)]
        [InlineData(0, long.MaxValue, StakeState.LockupInterval, long.MaxValue)]
        // NOTE: non claimable, unlocked, increased amount.
        [InlineData(0, 50, StakeState.LockupInterval, 500)]
        // NOTE: non claimable, unlocked, decreased amount.
        [InlineData(0, 50, StakeState.LockupInterval, 0)]
        [InlineData(0, long.MaxValue, StakeState.LockupInterval, 50)]
        public void Execute_Success_When_Exist_StakeState(
            long previousStartedBlockIndex,
            long previousAmount,
            long blockIndex,
            long amount)
        {
            var stakeStateAddr = StakeState.DeriveAddress(_agentAddr);
            var stakeState = new StakeState(
                address: stakeStateAddr,
                startedBlockIndex: previousStartedBlockIndex);
            stakeState.Claim(blockIndex);
            var previousState = _initialState
                .MintAsset(
                    new ActionContext(),
                    _agentAddr,
                    _ncg * Math.Max(previousAmount, amount))
                .TransferAsset(
                    new ActionContext(),
                    _agentAddr,
                    stakeStateAddr,
                    _ncg * previousAmount)
                .SetState(stakeStateAddr, stakeState.Serialize());
            Execute(
                blockIndex,
                previousState,
                new TestRandom(),
                _agentAddr,
                amount);
        }

        [Theory]
        // NOTE: minimum required_gold of StakeRegularRewardSheetFixtures.V2 is 50.
        // NOTE: LockupInterval of StakePolicySheetFixtures.V2 is 201,600.
        // NOTE: non claimable, locked up, same amount.
        [InlineData(0, 50, 0, 50)]
        [InlineData(0, 50, 201_599, 50)]
        [InlineData(0, long.MaxValue, 0, long.MaxValue)]
        [InlineData(0, long.MaxValue, 201_599, long.MaxValue)]
        // NOTE: non claimable, locked up, increased amount.
        [InlineData(0, 50, 0, 500)]
        [InlineData(0, 50, 201_599, 500)]
        // NOTE: non claimable, unlocked, same amount.
        [InlineData(0, 50, 201_600, 50)]
        [InlineData(0, long.MaxValue, 201_600, long.MaxValue)]
        // NOTE: non claimable, unlocked, increased amount.
        [InlineData(0, 50, 201_600, 500)]
        // NOTE: non claimable, unlocked, decreased amount.
        [InlineData(0, 50, 201_600, 0)]
        [InlineData(0, long.MaxValue, StakeState.LockupInterval, 50)]
        public void Execute_Success_When_Exist_StakeStateV2(
            long previousStartedBlockIndex,
            long previousAmount,
            long blockIndex,
            long amount)
        {
            var stakeStateAddr = StakeStateV2.DeriveAddress(_agentAddr);
            var stakeStateV2 = new StakeStateV2(
                contract: new Contract(_stakePolicySheet),
                startedBlockIndex: previousStartedBlockIndex,
                receivedBlockIndex: blockIndex);
            var previousState = _initialState
                .MintAsset(
                    new ActionContext(),
                    _agentAddr,
                    _ncg * Math.Max(previousAmount, amount))
                .TransferAsset(
                    new ActionContext(),
                    _agentAddr,
                    stakeStateAddr,
                    _ncg * previousAmount)
                .SetState(stakeStateAddr, stakeStateV2.Serialize());
            Execute(
                blockIndex,
                previousState,
                new TestRandom(),
                _agentAddr,
                amount);
        }

        private IAccount Execute(
            long blockIndex,
            IAccount previousState,
            IRandom random,
            Address signer,
            long amount)
        {
            var previousBalance = previousState.GetBalance(signer, _ncg);
            var previousStakeBalance = previousState.GetBalance(
                StakeState.DeriveAddress(signer),
                _ncg);
            var previousTotalBalance = previousBalance + previousStakeBalance;
            var action = new Stake(amount);
            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousState = previousState,
                RandomSeed = random.Seed,
                Rehearsal = false,
                Signer = signer,
            });

            var amountNCG = _ncg * amount;
            var nextBalance = nextState.GetBalance(signer, _ncg);
            var nextStakeBalance = nextState.GetBalance(
                StakeState.DeriveAddress(signer),
                _ncg);
            Assert.Equal(previousTotalBalance - amountNCG, nextBalance);
            Assert.Equal(amountNCG, nextStakeBalance);

            if (amount == 0)
            {
                Assert.False(nextState.TryGetStakeStateV2(_agentAddr, out _));
            }
            else if (amount > 0)
            {
                Assert.True(nextState.TryGetStakeStateV2(_agentAddr, out var stakeStateV2));
                Assert.Equal(
                    _stakePolicySheet.StakeRegularFixedRewardSheetValue,
                    stakeStateV2.Contract.StakeRegularFixedRewardSheetTableName);
                Assert.Equal(
                    _stakePolicySheet.StakeRegularRewardSheetValue,
                    stakeStateV2.Contract.StakeRegularRewardSheetTableName);
                Assert.Equal(
                    _stakePolicySheet.RewardIntervalValue,
                    stakeStateV2.Contract.RewardInterval);
                Assert.Equal(
                    _stakePolicySheet.LockupIntervalValue,
                    stakeStateV2.Contract.LockupInterval);
                Assert.Equal(blockIndex, stakeStateV2.StartedBlockIndex);
                Assert.Equal(0, stakeStateV2.ReceivedBlockIndex);
                Assert.Equal(
                    blockIndex + stakeStateV2.Contract.LockupInterval,
                    stakeStateV2.CancellableBlockIndex);
                Assert.Equal(blockIndex, stakeStateV2.ClaimedBlockIndex);
                Assert.Equal(
                    blockIndex + stakeStateV2.Contract.RewardInterval,
                    stakeStateV2.ClaimableBlockIndex);
            }

            return nextState;
        }
    }
}
