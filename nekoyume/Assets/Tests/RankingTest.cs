using System;
using System.Linq;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume.State;
using NUnit.Framework;

namespace Tests
{
    public class RankingTest : PlayModeTest
    {
        [Test]
        public void GetAgentAddressesEmpty()
        {
            var state = new RankingState();
            var result = state.GetAgentAddresses(1, null);
            Assert.IsEmpty(result);
        }

        [Test]
        public void GetAgentAddressesFilterDuplicated()
        {
            var state = new RankingState();
            var agentAddress = GetNewAddress();
            var avatar1 = new AvatarState(GetNewAddress(), agentAddress, 1);
            var avatar2 = new AvatarState(GetNewAddress(), agentAddress, 1);
            state.Update(avatar1);
            state.Update(avatar2);
            var result = state.GetAgentAddresses(2, null);
            Assert.AreEqual(1, result.Length);
            Assert.AreEqual(agentAddress, result.First());
        }

        [Test]
        public void GetAgentAddresses()
        {
            var state = new RankingState();
            var avatar1 = new AvatarState(GetNewAddress(), GetNewAddress(), 1);
            var avatar2 = new AvatarState(GetNewAddress(), GetNewAddress(), 1);
            state.Update(avatar1);
            state.Update(avatar2);
            var result = state.GetAgentAddresses(3, null);
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(avatar1.agentAddress, result.First());
            Assert.AreEqual(avatar2.agentAddress, result.Last());
        }

        private static Address GetNewAddress()
        {
            return new PrivateKey().PublicKey.ToAddress();
        }

        [Test]
        public void GetAvatars()
        {
            var state = new RankingState();
            var avatar1 = new AvatarState(GetNewAddress(), GetNewAddress(), 1)
            {
                worldStage = 2, clearedAt = DateTimeOffset.UtcNow
            };
            var avatar2 = new AvatarState(GetNewAddress(), GetNewAddress(), 1)
            {
                worldStage = 2, clearedAt = DateTimeOffset.UtcNow
            };
            Assert.Greater(avatar2.clearedAt, avatar1.clearedAt);
            state.Update(avatar2);
            state.Update(avatar1);
            var result = state.GetAvatars(null);
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(avatar1.clearedAt, result.First().clearedAt);
            Assert.AreEqual(avatar2.clearedAt, result.Last().clearedAt);

        }

        [Test]
        public void GetAvatarsWithTimeStamp()
        {
            var state = new RankingState();
            var avatar1 = new AvatarState(GetNewAddress(), GetNewAddress(), 1)
            {
                worldStage = 2, clearedAt = DateTimeOffset.UtcNow
            };
            var avatar2 = new AvatarState(GetNewAddress(), GetNewAddress(), 1)
            {
                worldStage = 2, clearedAt = DateTimeOffset.UtcNow
            };
            var avatar3 = new AvatarState(GetNewAddress(), GetNewAddress(), 1)
            {
                worldStage = 3, clearedAt = DateTimeOffset.UtcNow, updatedAt =  DateTimeOffset.UtcNow.AddDays(-2)
            };

            Assert.Greater(avatar2.clearedAt, avatar1.clearedAt);
            state.Update(avatar2);
            state.Update(avatar1);
            state.Update(avatar3);
            Assert.AreEqual(3, state.GetAvatars(null).First().worldStage);
            var result = state.GetAvatars(DateTimeOffset.UtcNow);
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(avatar1.clearedAt, result.First().clearedAt);
            Assert.AreEqual(avatar2.clearedAt, result.Last().clearedAt);

        }

        [Test]
        public void Update()
        {
            var state = new RankingState();
            var avatar1 = new AvatarState(GetNewAddress(), GetNewAddress(), 1)
            {
                worldStage = 2, clearedAt = DateTimeOffset.UtcNow
            };

            state.Update(avatar1);
            var avatar2 = avatar1;
            Assert.AreEqual(2, state.GetAvatars(null).First().worldStage);

            avatar1.worldStage = 1;
            state.Update(avatar1);
            Assert.AreEqual(2, state.GetAvatars(null).First().worldStage);

            avatar1.worldStage = 3;
            state.Update(avatar1);
            Assert.AreEqual(3, state.GetAvatars(null).First().worldStage);
        }
    }
}
