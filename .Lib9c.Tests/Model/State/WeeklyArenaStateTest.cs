namespace Lib9c.Tests.Model.State
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class WeeklyArenaStateTest
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;

        public WeeklyArenaStateTest()
        {
            _sheets = TableSheetsImporter.ImportSheets();
            _sheets[nameof(CharacterSheet)] =
                "id,_name,size_type,elemental_type,hp,atk,def,cri,hit,spd,lv_hp,lv_atk,lv_def,lv_cri,lv_hit,lv_spd,attack_range,run_speed\n100010,전사,S,0,300,20,10,10,90,70,12,0.8,0.4,0,3.6,2.8,2,3";
            _tableSheets = new TableSheets(_sheets);
        }

        [Theory]
        [InlineData(1, "e0c15f3CEF3FCdCb02e181b0077D2813Ebc925CA")]
        [InlineData(2, "93C3EA9EFB1edFE6106E047579964bfCF72B6000")]
        public void DeriveAddress(int index, string expected)
        {
            var state = new WeeklyArenaState(index);
            Assert.Equal(new Address(expected), state.address);
        }

        [Fact]
        public void Serialize()
        {
            var address = default(Address);
            var state = new WeeklyArenaState(address);

            var serialized = (Dictionary)state.Serialize();
            var deserialized = new WeeklyArenaState(serialized);

            Assert.Equal(state.address, deserialized.address);
        }

        [Fact]
        public void SerializeWithDotNetAPI()
        {
            var address = default(Address);
            var state = new WeeklyArenaState(address);

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, state);
            ms.Seek(0, SeekOrigin.Begin);

            var deserialized = (WeeklyArenaState)formatter.Deserialize(ms);
            Assert.Equal(state.address, deserialized.address);
        }

        [Theory]
        [InlineData(1000, 1001, 1)]
        [InlineData(1001, 1100, 2)]
        [InlineData(1100, 1200, 3)]
        [InlineData(1200, 1400, 4)]
        [InlineData(1400, 1800, 5)]
        [InlineData(1800, 10000, 6)]
        public void ArenaInfoGetRewardCount(int minScore, int maxScore, int expected)
        {
            var score = new Random().Next(minScore, maxScore);
            var serialized = new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"score"] = score.Serialize(),
                [(Text)"receive"] = false.Serialize(),
            });
            var info = new ArenaInfo(serialized);
            Assert.Equal(expected, info.GetRewardCount());
        }

        [Theory]
        [InlineData(100, 1, 100, 100)]
        [InlineData(100, 51, 50, 50)]
        [InlineData(10, 1, 50, 10)]
        [InlineData(10, 1, 1, 1)]
        [InlineData(10, 6, 50, 5)]
        [InlineData(10, 6, 1, 1)]
        [InlineData(0, 1, 1, 0)]
        public void GetArenaInfosByFirstRankAndCount(
            int infoCount,
            int firstRank,
            int count,
            int expectedCount)
        {
            var weeklyArenaState = new WeeklyArenaState(new PrivateKey().ToAddress());
            var characterSheet = new CharacterSheet();
            characterSheet.Set(_sheets[nameof(CharacterSheet)]);

            for (var i = 0; i < infoCount; i++)
            {
                var avatarState = new AvatarState(
                    new PrivateKey().ToAddress(),
                    new PrivateKey().ToAddress(),
                    0L,
                    _tableSheets.GetAvatarSheets(),
                    new GameConfigState(),
                    default,
                    i.ToString());
                weeklyArenaState.Add(
                    new PrivateKey().ToAddress(),
                    new ArenaInfo(avatarState, characterSheet, new CostumeStatSheet(), true));
            }

            var arenaInfos = weeklyArenaState.GetArenaInfos(firstRank, count);
            Assert.Equal(expectedCount, arenaInfos.Count);

            var expectedRank = firstRank;
            foreach (var arenaInfo in arenaInfos)
            {
                Assert.Equal(expectedRank++, arenaInfo.rank);
            }
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(10, 11)]
        public void GetArenaInfosByFirstRankAndCountThrow(int infoCount, int firstRank)
        {
            var weeklyArenaState = new WeeklyArenaState(new PrivateKey().ToAddress());
            var characterSheet = new CharacterSheet();
            characterSheet.Set(_sheets[nameof(CharacterSheet)]);

            for (var i = 0; i < infoCount; i++)
            {
                var avatarState = new AvatarState(
                    new PrivateKey().ToAddress(),
                    new PrivateKey().ToAddress(),
                    0L,
                    _tableSheets.GetAvatarSheets(),
                    new GameConfigState(),
                    default,
                    i.ToString());
                weeklyArenaState.Add(
                    new PrivateKey().ToAddress(),
                    new ArenaInfo(avatarState, characterSheet, new CostumeStatSheet(), true));
            }

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                weeklyArenaState.GetArenaInfos(firstRank, 100));
        }

        [Theory]
        [InlineData(100, 1, 10, 10, 11)]
        [InlineData(100, 50, 10, 10, 21)]
        [InlineData(100, 100, 10, 10, 11)]
        public void GetArenaInfosByUpperAndLowerRange(
            int infoCount,
            int targetRank,
            int upperRange,
            int lowerRange,
            int expectedCount)
        {
            var weeklyArenaState = new WeeklyArenaState(new PrivateKey().ToAddress());
            Address targetAddress = default;
            var characterSheet = new CharacterSheet();
            characterSheet.Set(_sheets[nameof(CharacterSheet)]);
            for (var i = 0; i < infoCount; i++)
            {
                var avatarAddress = new PrivateKey().ToAddress();
                if (i + 1 == targetRank)
                {
                    targetAddress = avatarAddress;
                }

                var avatarState = new AvatarState(
                    avatarAddress,
                    new PrivateKey().ToAddress(),
                    0L,
                    _tableSheets.GetAvatarSheets(),
                    new GameConfigState(),
                    default,
                    i.ToString());
                weeklyArenaState.Add(
                    new PrivateKey().ToAddress(),
                    new ArenaInfo(avatarState, characterSheet, new CostumeStatSheet(), true));
            }

            var arenaInfos = weeklyArenaState.GetArenaInfos(targetAddress, upperRange, lowerRange);
            Assert.Equal(expectedCount, arenaInfos.Count);

            var expectedRank = Math.Max(1, targetRank - upperRange);
            foreach (var arenaInfo in arenaInfos)
            {
                Assert.Equal(expectedRank++, arenaInfo.rank);
            }
        }
    }
}
