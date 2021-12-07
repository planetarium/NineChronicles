namespace Lib9c.Tests.Model.State
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;

    public class RankingStateTest
    {
        [Theory]
        [InlineData(1, "8BFecEd8192e8D2fF05a5235eB39d9d724528e02")]
        [InlineData(2, "4C1E779dCb8B5f8291D5d8c21001eae919961DFa")]
        public void Derive(int index, string expected)
        {
            Assert.Equal(new Address(expected), RankingState.Derive(index));
        }

        [Fact]
        public void Serialize()
        {
            var state = new RankingState();
            var avatarAddress = new PrivateKey().ToAddress();
            state.UpdateRankingMap(avatarAddress);
            var serialized = state.Serialize();

            var des = new RankingState((Dictionary)serialized);

            Assert.Equal(Addresses.Ranking, des.address);
            Assert.Contains(des.RankingMap, m => m.Value.Contains(avatarAddress));
        }

        [Fact]
        public void Deterministic_Between_Serialize_And_SerializeV1()
        {
            var state = new RankingState();
            for (var i = 0; i < 1000; i++)
            {
                state.UpdateRankingMap(new PrivateKey().ToAddress());
            }

            var serialized = state.Serialize();
            var serializedV1 = state.SerializeV1();
            Assert.Equal(serializedV1, serialized);

            var deserialized = new RankingState((Bencodex.Types.Dictionary)serialized);
            var deserializedV1 = new RankingState((Bencodex.Types.Dictionary)serializedV1);
            serialized = deserialized.Serialize();
            serializedV1 = deserializedV1.Serialize();
            Assert.Equal(serializedV1, serialized);
        }

        [Fact]
        public void UpdateRankingMap()
        {
            var state = new RankingState();
            state.UpdateRankingMap(default);
            state.UpdateRankingMap(default(Address).Derive("test"));
            Assert.Equal(2, state.RankingMap[RankingState.Derive(0)].Count);
        }

        [Fact]
        public void UpdateRankingMapThrowRankingExceededException()
        {
            var state = new RankingState();
            var address = default(Address);
            var max = RankingMapState.Capacity * RankingState.RankingMapCapacity;
            for (var i = 0; i < max; i++)
            {
                state.UpdateRankingMap(address.Derive(i.ToString()));
            }

            var exec = Assert.Throws<RankingExceededException>(() =>
                state.UpdateRankingMap(address.Derive((max + 1).ToString())));

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, exec);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (RankingExceededException)formatter.Deserialize(ms);
            Assert.Equal(exec.Message, deserialized.Message);
        }
    }
}
