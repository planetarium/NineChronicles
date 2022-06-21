using Nekoyume;
using Nekoyume.Helper;
using Nekoyume.TableData;
using NUnit.Framework;

namespace Tests.EditMode.Extensions
{
    public class ArenaSheetExtensionsTest
    {
        [DatapointSource]
        public (
            int championshipId,
            int round,
            bool expectResult,
            int? expectSeasonNumber)[] valuesForTryGetSeasonNumberTest =
            {
                (1, 1, false, null),
                (1, 2, true, 1),
                (1, 3, false, null),
                (1, 4, true, 2),
                (1, 5, false, null),
                (1, 6, true, 3),
                (1, 7, false, null),
                (1, 8, false, null),
                (2, 1, false, null),
                (2, 2, true, 4),
                (2, 3, false, null),
                (2, 4, true, 5),
                (2, 5, false, null),
                (2, 6, true, 6),
                (2, 7, false, null),
                (2, 8, false, null),
                // NOTE: Uncomment below after championship data extended.
                // (3, 1, false, null),
                // (3, 2, true, 7),
                // (3, 3, false, null),
                // (3, 4, true, 8),
                // (3, 5, false, null),
                // (3, 6, true, 9),
                // (3, 7, false, null),
                // (3, 8, false, null),
                // (4, 1, false, null),
                // (4, 2, true, 10),
                // (4, 3, false, null),
                // (4, 4, true, 11),
                // (4, 5, false, null),
                // (4, 6, true, 12),
                // (4, 7, false, null),
                // (4, 8, false, null),
                // (5, 1, false, null),
                // (5, 2, true, 1),
                // (5, 3, false, null),
                // (5, 4, true, 2),
                // (5, 5, false, null),
                // (5, 6, true, 3),
                // (5, 7, false, null),
                // (5, 8, false, null),
            };

        [DatapointSource]
        public (
            int championshipId,
            int round,
            bool expectResult,
            int expectMedalItemResourceId)[] valuesForTryGetMedalItemResourceIdTest =
            {
                (1, 1, false, 700101),
                (1, 2, true, 700102),
                (1, 3, false, 700103),
                (1, 4, true, 700104),
                (1, 5, false, 700105),
                (1, 6, true, 700106),
                (1, 7, false, 700107),
                (1, 8, true, 700108),
                (2, 1, false, 700101),
                (2, 2, true, 700102),
                (2, 3, false, 700103),
                (2, 4, true, 700104),
                (2, 5, false, 700105),
                (2, 6, true, 700106),
                (2, 7, false, 700107),
                (2, 8, true, 700108),
            };

        [DatapointSource]
        public (int championshipId, int year)[]
            valuesForGetChampionshipYearTest =
            {
                (1, 2022),
                (2, 2022),
                // NOTE: Uncomment below after championship data extended.
                // (3, 2023),
                // (4, 2023),
                // (5, 2023),
                // (6, 2023),
                // (7, 2024),
            };

        private ArenaSheet _arenaSheet;

        [SetUp]
        public void SetUp()
        {
            _arenaSheet = TableSheetsHelper.MakeTableSheets().ArenaSheet;
        }

        [Theory]
        public void TryGetSeasonNumber((
            int championshipId,
            int round,
            bool expectResult,
            int? expectSeasonNumber) tuple)
        {
            var (
                championshipId,
                round,
                expectResult,
                expectSeasonNumber) = tuple;
            Assert.True(_arenaSheet.TryGetValue(championshipId, out var row));
            if (expectResult)
            {
                Assert.True(row.Round.TryGetSeasonNumber(round, out var seasonNumber));
                Assert.AreEqual(expectSeasonNumber.Value, seasonNumber);
            }
            else
            {
                Assert.False(row.Round.TryGetSeasonNumber(round, out _));
            }
        }

        [Theory]
        public void TryGetMedalItemResourceId((
            int championshipId,
            int round,
            bool expectResult,
            int expectMedalItemResourceId) tuple)
        {
            var (
                championshipId,
                round,
                expectResult,
                expectMedalItemResourceId) = tuple;
            Assert.True(_arenaSheet.TryGetValue(championshipId, out var row));
            Assert.True(row.TryGetRound(round, out var roundData));
            if (expectResult)
            {
                Assert.True(roundData.TryGetMedalItemResourceId(out var medalItemResourceId));
                Assert.AreEqual(expectMedalItemResourceId, medalItemResourceId);    
            }
            else
            {
                Assert.False(roundData.TryGetMedalItemResourceId(out _));
            }
        }
        
        [Theory]
        public void GetChampionshipYear((int championshipId, int year) tuple)
        {
            var (championshipId, year) = tuple;
            Assert.True(_arenaSheet.TryGetValue(championshipId, out var row));
            Assert.AreEqual(year, row.GetChampionshipYear());
        }
    }
}
