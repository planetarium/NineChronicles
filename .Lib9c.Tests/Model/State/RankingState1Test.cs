namespace Lib9c.Tests.Model.State
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class RankingState1Test
    {
        [Theory]
        [InlineData(1, "8BFecEd8192e8D2fF05a5235eB39d9d724528e02")]
        [InlineData(2, "4C1E779dCb8B5f8291D5d8c21001eae919961DFa")]
        public void Derive(int index, string expected)
        {
            Assert.Equal(new Address(expected), RankingState1.Derive(index));
        }

        [Fact]
        public void Serialize()
        {
            var state = new RankingState1();
            var avatarAddress = new PrivateKey().ToAddress();
            state.UpdateRankingMap(avatarAddress);
            var serialized = state.Serialize();

            var des = new RankingState1((Dictionary)serialized);

            Assert.Equal(Addresses.Ranking, des.address);
            Assert.Contains(des.RankingMap, m => m.Value.Contains(avatarAddress));
        }

        [Fact]
        public void Deterministic_Between_SerializeV1_And_SerializeV1_With_Deterministic_Problem()
        {
            var state = new RankingState1();
            for (var i = 0; i < 1000; i++)
            {
                state.UpdateRankingMap(new PrivateKey().ToAddress());
            }

            var serializedV1 = state.Serialize();
            var serializedV1WithDeterministicProblem = SerializeV1_With_Deterministic_Problem(state);
            Assert.Equal(serializedV1WithDeterministicProblem, serializedV1);

            var deserialized = new RankingState1((Bencodex.Types.Dictionary)serializedV1);
            var deserializedV1 = new RankingState1((Bencodex.Types.Dictionary)serializedV1WithDeterministicProblem);
            serializedV1 = deserialized.Serialize();
            serializedV1WithDeterministicProblem = deserializedV1.Serialize();
            Assert.Equal(serializedV1WithDeterministicProblem, serializedV1);
        }

        [Fact]
        public void UpdateRankingMap()
        {
            var state = new RankingState1();
            state.UpdateRankingMap(default);
            state.UpdateRankingMap(default(Address).Derive("test"));
            Assert.Equal(2, state.RankingMap[RankingState1.Derive(0)].Count);
        }

        [Fact]
        public void UpdateRankingMapThrowRankingExceededException()
        {
            var state = new RankingState1();
            var address = default(Address);
            var max = RankingMapState.Capacity * RankingState1.RankingMapCapacity;
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

        private static IValue SerializeV1_With_Deterministic_Problem(RankingState1 rankingState)
        {
#pragma warning disable LAA1002
            return new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"ranking_map"] = new Dictionary(rankingState.RankingMap.Select(kv =>
                    new KeyValuePair<IKey, IValue>(
                        (Binary)kv.Key.Serialize(),
                        new List(kv.Value.Select(a => a.Serialize()))
                    )
                )),
            }.Union(new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)LegacyAddressKey] = rankingState.address.Serialize(),
            })));
#pragma warning restore LAA1002
        }
    }
}
