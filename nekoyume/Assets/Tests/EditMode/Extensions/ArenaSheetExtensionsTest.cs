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
            int medalItemId,
            int expectChampionshipNumber,
            int expectSeasonNumber)[] valuesFor_ToArenaNumbers =
            {
                (int.MaxValue, 0, 0),
                (int.MinValue, 0, 0),
                (700_000, 0, 0),
                (700_100, 1, 0),
                (700_101, 1, 0),
                (700_102, 1, 4),
                (700_103, 1, 0),
                (700_104, 1, 5),
                (700_105, 1, 0),
                (700_106, 1, 6),
                (700_107, 1, 0),
                (700_108, 1, 0),
                (700_202, 2, 7),
                (700_204, 2, 8),
                (700_206, 2, 9),
                (700_208, 2, 0),
                // NOTE: Uncomment below after championship data extended.
                // (700_302, 1, 1),
                // (700_304, 1, 2),
                // (700_306, 1, 3),
                // (700_308, 1, 0),
                // (700_402, 2, 4),
                // (700_404, 2, 5),
                // (700_406, 2, 6),
                // (700_408, 2, 0),
                // (700_502, 3, 7),
                // (700_504, 3, 8),
                // (700_506, 3, 9),
                // (700_508, 3, 0),
                // (700_602, 4, 10),
                // (700_604, 4, 11),
                // (700_606, 4, 12),
                // (700_608, 4, 0),
            };

        [DatapointSource]
        public (
            int championshipId,
            int round,
            bool expectResult,
            int? expectSeasonNumber)[] valuesFor_TryGetSeasonNumberTest =
            {
                (1, 1, false, null),
                (1, 2, true, 4),
                (1, 3, false, null),
                (1, 4, true, 5),
                (1, 5, false, null),
                (1, 6, true, 6),
                (1, 7, false, null),
                (1, 8, false, null),
                (2, 1, false, null),
                (2, 2, true, 7),
                (2, 3, false, null),
                (2, 4, true, 8),
                (2, 5, false, null),
                (2, 6, true, 9),
                (2, 7, false, null),
                (2, 8, false, null),
                // NOTE: Uncomment below after championship data extended.
                // (3, 1, false, null),
                // (3, 2, true, 1),
                // (3, 3, false, null),
                // (3, 4, true, 2),
                // (3, 5, false, null),
                // (3, 6, true, 3),
                // (3, 7, false, null),
                // (3, 8, false, null),
                // (4, 1, false, null),
                // (4, 2, true, 4),
                // (4, 3, false, null),
                // (4, 4, true, 5),
                // (4, 5, false, null),
                // (4, 6, true, 6),
                // (4, 7, false, null),
                // (4, 8, false, null),
                // (5, 1, false, null),
                // (5, 2, true, 7),
                // (5, 3, false, null),
                // (5, 4, true, 8),
                // (5, 5, false, null),
                // (5, 6, true, 9),
                // (5, 7, false, null),
                // (5, 8, false, null),
                // (6, 1, false, null),
                // (6, 2, true, 10),
                // (6, 3, false, null),
                // (6, 4, true, 11),
                // (6, 5, false, null),
                // (6, 6, true, 12),
                // (6, 7, false, null),
                // (6, 8, false, null),
                // (7, 1, false, null),
                // (7, 2, true, 1),
                // (7, 3, false, null),
                // (7, 4, true, 2),
                // (7, 5, false, null),
                // (7, 6, true, 3),
                // (7, 7, false, null),
                // (7, 8, false, null),
            };

        [DatapointSource]
        public (
            int championshipId,
            int round,
            bool expectResult,
            int expectMedalItemResourceId)[]
            valuesFor_TryGetMedalItemResourceIdTest =
            {
                (1, 1, false, 700101),
                (1, 2, true, 700202),
                (1, 3, false, 700103),
                (1, 4, true, 700204),
                (1, 5, false, 700105),
                (1, 6, true, 700206),
                (1, 7, false, 700107),
                (1, 8, true, 700108),
                (2, 1, false, 700101),
                (2, 2, true, 700302),
                (2, 3, false, 700103),
                (2, 4, true, 700304),
                (2, 5, false, 700105),
                (2, 6, true, 700306),
                (2, 7, false, 700107),
                (2, 8, true, 700208),
            };

        [DatapointSource]
        public (int championshipId, int year)[]
            valuesFor_GetChampionshipYearTest =
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
        public void ToArenaNumbers((
            int medalItemId,
            int expectChampionshipNumber,
            int expectSeasonNumber) tuple)
        {
            var (
                medalItemId,
                expectChampionshipNumber,
                expectSeasonNumber) = tuple;
            var (championshipNumber, seasonNumber) =
                medalItemId.ToArenaNumbers(_arenaSheet);
            Assert.AreEqual(expectChampionshipNumber, championshipNumber);
            Assert.AreEqual(expectSeasonNumber, seasonNumber);
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
