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
                (700_206, 2, 0),
                (700_208, 2, 0),
                // NOTE: Uncomment below after championship data extended.
                // (700_302, 3, 9),
                // (700_304, 3, 10),
                // (700_306, 3, 0),
                // (700_402, 4, 11),
                // (700_404, 4, 12),
                // (700_406, 4, 0),
                // (700_502, 5, 13),
                // (700_504, 5, 14),
                // (700_506, 5, 0),
                // (700_602, 6, 15),
                // (700_604, 6, 16),
                // (700_606, 6, 0),
                // (700_702, 7, 17),
                // (700_704, 7, 18),
                // (700_706, 7, 0),
                // (700_802, 8, 19),
                // (700_804, 8, 20),
                // (700_806, 8, 0),
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
                (2, 6, false, null),
                (2, 7, false, null),
                (2, 8, false, null),
                // NOTE: Uncomment below after championship data extended.
                // (3, 1, false, null),
                // (3, 2, true, 9),
                // (3, 3, false, null),
                // (3, 4, true, 10),
                // (3, 5, false, null),
                // (3, 6, false, null),
                // (4, 1, false, null),
                // (4, 2, true, 11),
                // (4, 3, false, null),
                // (4, 4, true, 12),
                // (4, 5, false, null),
                // (4, 6, false, null),
                // (5, 1, false, null),
                // (5, 2, true, 13),
                // (5, 3, false, null),
                // (5, 4, true, 14),
                // (5, 5, false, null),
                // (5, 6, false, null),
                // (6, 1, false, null),
                // (6, 2, true, 15),
                // (6, 3, false, null),
                // (6, 4, true, 16),
                // (6, 5, false, null),
                // (6, 6, false, null),
                // (7, 1, false, null),
                // (7, 2, true, 17),
                // (7, 3, false, null),
                // (7, 4, true, 18),
                // (7, 5, false, null),
                // (7, 6, false, null),
                // (8, 1, false, null),
                // (8, 2, true, 19),
                // (8, 3, false, null),
                // (8, 4, true, 20),
                // (8, 5, false, null),
                // (8, 6, false, null),
            };

        [DatapointSource]
        public (
            int championshipId,
            int round,
            bool expectResult,
            int expectMedalItemResourceId)[]
            valuesFor_TryGetMedalItemResourceIdTest =
            {
                (1, 1, false, 700000),
                (1, 2, true, 700202),
                (1, 3, false, 700000),
                (1, 4, true, 700204),
                (1, 5, false, 700000),
                (1, 6, true, 700206),
                (1, 7, false, 700000),
                (1, 8, true, 700108),
                (2, 1, false, 700000),
                (2, 2, true, 700302),
                (2, 3, false, 700000),
                (2, 4, true, 700304),
                (2, 5, false, 700000),
                (2, 6, true, 700206),
                (2, 7, false, 700000),
                (2, 8, false, 700000),
                // NOTE: Uncomment below after championship data extended.
                // (3, 1, false, 700000),
                // (3, 2, true, 700402),
                // (3, 3, false, 700000),
                // (3, 4, true, 700404),
                // (3, 5, false, 700000),
                // (3, 6, true, 700306),
                // (4, 1, false, 700000),
                // (4, 2, true, 700502),
                // (4, 3, false, 700000),
                // (4, 4, true, 700504),
                // (4, 5, false, 700000),
                // (4, 6, true, 700406),
                // (5, 1, false, 700000),
                // (5, 2, true, 700602),
                // (5, 3, false, 700000),
                // (5, 4, true, 700604),
                // (5, 5, false, 700000),
                // (5, 6, true, 700506),
                // (6, 1, false, 700000),
                // (6, 2, true, 700702),
                // (6, 3, false, 700000),
                // (6, 4, true, 700704),
                // (6, 5, false, 700000),
                // (6, 6, true, 700606),
                // (7, 1, false, 700000),
                // (7, 2, true, 700802),
                // (7, 3, false, 700000),
                // (7, 4, true, 700804),
                // (7, 5, false, 700000),
                // (7, 6, true, 700706),
                // (8, 1, false, 700000),
                // (8, 2, true, 700902),
                // (8, 3, false, 700000),
                // (8, 4, true, 700904),
                // (8, 5, false, 700000),
                // (8, 6, true, 700806),
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
                // (7, 2023),
                // (8, 2023),
                // (9, 2024),
                // (10, 2024),
                // (11, 2024),
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
