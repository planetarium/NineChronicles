namespace Lib9c.Tests.Action.Factory
{
    using System;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Action.Factory;
    using Xunit;

    public class ClaimStakeRewardFactoryTest
    {
        [Theory]
        [InlineData(ClaimStakeReward2.ObsoletedIndex - 1, typeof(ClaimStakeReward2))]
        [InlineData(ClaimStakeReward2.ObsoletedIndex, typeof(ClaimStakeReward2))]
        [InlineData(ClaimStakeReward2.ObsoletedIndex + 1, typeof(ClaimStakeReward))]
        public void Create_ByBlockIndex_Success(
            long blockIndex,
            Type type)
        {
            var addr = new PrivateKey().ToAddress();
            var action = ClaimStakeRewardFactory.CreateByBlockIndex(blockIndex, addr);
            Assert.Equal(type, action.GetType());
        }

        [Theory]
        [InlineData(1, typeof(ClaimStakeReward1))]
        [InlineData(2, typeof(ClaimStakeReward2))]
        [InlineData(3, typeof(ClaimStakeReward))]
        public void Create_ByVersion_Success(
            int version,
            Type type)
        {
            var addr = new PrivateKey().ToAddress();
            var action = ClaimStakeRewardFactory.CreateByVersion(version, addr);
            Assert.Equal(type, action.GetType());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        public void Create_ByVersion_Failure(int version)
        {
            var addr = new PrivateKey().ToAddress();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ClaimStakeRewardFactory.CreateByVersion(version, addr));
        }
    }
}
