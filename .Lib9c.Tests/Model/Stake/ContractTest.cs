namespace Lib9c.Tests.Model.Stake
{
    using System;
    using Lib9c.Tests.Fixtures.TableCSV.Stake;
    using Nekoyume.Model.Stake;
    using Nekoyume.TableData.Stake;
    using Xunit;

    public class ContractTest
    {
        [Theory]
        [InlineData(
            Contract.StakeRegularFixedRewardSheetPrefix,
            Contract.StakeRegularRewardSheetPrefix,
            1,
            1)]
        [InlineData(
            Contract.StakeRegularFixedRewardSheetPrefix + "test",
            Contract.StakeRegularRewardSheetPrefix + "test",
            long.MaxValue,
            long.MaxValue)]
        public void Constructor_Default(
            string stakeRegularFixedRewardSheetTableName,
            string stakeRegularRewardSheetTableName,
            long rewardInterval,
            long lockupInterval)
        {
            var contract = new Contract(
                stakeRegularFixedRewardSheetTableName,
                stakeRegularRewardSheetTableName,
                rewardInterval,
                lockupInterval);
            Assert.Equal(
                stakeRegularFixedRewardSheetTableName,
                contract.StakeRegularFixedRewardSheetTableName);
            Assert.Equal(
                stakeRegularRewardSheetTableName,
                contract.StakeRegularRewardSheetTableName);
            Assert.Equal(rewardInterval, contract.RewardInterval);
            Assert.Equal(lockupInterval, contract.LockupInterval);
        }

        [Theory]
        [InlineData(
            null,
            Contract.StakeRegularRewardSheetPrefix,
            1,
            1)]
        [InlineData(
            "",
            Contract.StakeRegularRewardSheetPrefix,
            1,
            1)]
        [InlineData(
            "failed",
            Contract.StakeRegularRewardSheetPrefix,
            1,
            1)]
        [InlineData(
            Contract.StakeRegularFixedRewardSheetPrefix,
            null,
            1,
            1)]
        [InlineData(
            Contract.StakeRegularFixedRewardSheetPrefix,
            "",
            1,
            1)]
        [InlineData(
            Contract.StakeRegularFixedRewardSheetPrefix,
            "failed",
            1,
            1)]
        public void Constructor_Throws_ArgumentException(
            string stakeRegularFixedRewardSheetTableName,
            string stakeRegularRewardSheetTableName,
            long rewardInterval,
            long lockupInterval)
        {
            Assert.Throws<ArgumentException>(() => new Contract(
                stakeRegularFixedRewardSheetTableName,
                stakeRegularRewardSheetTableName,
                rewardInterval,
                lockupInterval));
        }

        [Theory]
        [InlineData(
            Contract.StakeRegularFixedRewardSheetPrefix,
            Contract.StakeRegularRewardSheetPrefix,
            -1,
            1)]
        [InlineData(
            Contract.StakeRegularFixedRewardSheetPrefix,
            Contract.StakeRegularRewardSheetPrefix,
            0,
            1)]
        [InlineData(
            Contract.StakeRegularFixedRewardSheetPrefix,
            Contract.StakeRegularRewardSheetPrefix,
            1,
            -1)]
        [InlineData(
            Contract.StakeRegularFixedRewardSheetPrefix,
            Contract.StakeRegularRewardSheetPrefix,
            1,
            0)]
        public void Constructor_Throws_ArgumentOutOfRangeException(
            string stakeRegularFixedRewardSheetTableName,
            string stakeRegularRewardSheetTableName,
            long rewardInterval,
            long lockupInterval)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Contract(
                stakeRegularFixedRewardSheetTableName,
                stakeRegularRewardSheetTableName,
                rewardInterval,
                lockupInterval));
        }

        [Theory]
        [InlineData(StakePolicySheetFixtures.V1)]
        [InlineData(StakePolicySheetFixtures.V2)]
        public void Constructor_StakePolicySheet(string stakePolicySheetCsv)
        {
            var sheet = new StakePolicySheet();
            sheet.Set(stakePolicySheetCsv);
            var contract = new Contract(sheet);
            Assert.Equal(
                sheet.StakeRegularFixedRewardSheetValue,
                contract.StakeRegularFixedRewardSheetTableName);
            Assert.Equal(
                sheet.StakeRegularRewardSheetValue,
                contract.StakeRegularRewardSheetTableName);
            Assert.Equal(sheet.RewardIntervalValue, contract.RewardInterval);
            Assert.Equal(sheet.LockupIntervalValue, contract.LockupInterval);
        }

        [Fact]
        public void Serde()
        {
            var contract = new Contract(
                "StakeRegularFixedRewardSheet_V1",
                "StakeRegularRewardSheet_V1",
                1,
                1);
            var ser = contract.Serialize();
            var des = new Contract(ser);
            Assert.Equal(
                contract.StakeRegularFixedRewardSheetTableName,
                des.StakeRegularFixedRewardSheetTableName);
            Assert.Equal(
                contract.StakeRegularRewardSheetTableName,
                des.StakeRegularRewardSheetTableName);
            Assert.Equal(contract.RewardInterval, des.RewardInterval);
            Assert.Equal(contract.LockupInterval, des.LockupInterval);
            var ser2 = des.Serialize();
            Assert.Equal(ser, ser2);
        }

        [Fact]
        public void Compare()
        {
            var contractL = new Contract(
                "StakeRegularFixedRewardSheet_V1",
                "StakeRegularRewardSheet_V1",
                1,
                1);
            var contractR = new Contract(
                "StakeRegularFixedRewardSheet_V1",
                "StakeRegularRewardSheet_V1",
                1,
                1);
            Assert.Equal(contractL, contractR);
            contractR = new Contract(
                "StakeRegularFixedRewardSheet_V1",
                "StakeRegularRewardSheet_V1",
                1,
                2);
            Assert.NotEqual(contractL, contractR);
        }
    }
}
