namespace Lib9c.Tests.Model
{
    using System.Linq;
    using Nekoyume;
    using Nekoyume.Model;
    using Xunit;

    public class WorldInformationTest
    {
        private readonly TableSheets _tableSheets;

        public WorldInformationTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
        }

        [Fact]
        public void TryGetFirstWorld()
        {
            var worldSheet = _tableSheets.WorldSheet;
            var wi = new WorldInformation(Bencodex.Types.Dictionary.Empty);

            Assert.False(wi.TryGetFirstWorld(out _));

            foreach (var row in worldSheet.Values.OrderByDescending(r => r.Id))
            {
                wi.TryAddWorld(row, out _);
            }

            wi.TryGetFirstWorld(out var world);

            Assert.Equal(1, world.Id);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(2, 99)]
        public void TryGetLastClearedStageId(int worldId, int stageId)
        {
            var wi = new WorldInformation(0, _tableSheets.WorldSheet, stageId - 1);

            Assert.False(wi.IsStageCleared(stageId));
            Assert.NotEqual(
                stageId,
                wi.TryGetLastClearedStageId(out var clearedStageId) ? clearedStageId : 0);

            wi.ClearStage(worldId, stageId, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);

            Assert.True(wi.IsStageCleared(stageId));
            Assert.True(wi.TryGetLastClearedStageId(out clearedStageId));
            Assert.Equal(stageId, clearedStageId);
        }

        [Theory]
        [InlineData(GameConfig.MimisbrunnrStartStageId)]
        [InlineData(GameConfig.MimisbrunnrStartStageId + 9)]
        public void TryGetLastClearedMimirbrunnrStageId(int stageId)
        {
            var wi = new WorldInformation(0, _tableSheets.WorldSheet, stageId - 1);

            Assert.False(wi.IsStageCleared(stageId));
            Assert.NotEqual(
                stageId,
                wi.TryGetLastClearedMimisbrunnrStageId(out var clearedStageId) ? clearedStageId : 0);

            wi.ClearStage(
                GameConfig.MimisbrunnrWorldId,
                stageId,
                0,
                _tableSheets.WorldSheet,
                _tableSheets.WorldUnlockSheet);

            Assert.True(wi.IsStageCleared(stageId));
            Assert.True(wi.TryGetLastClearedMimisbrunnrStageId(out clearedStageId));
            Assert.Equal(stageId, clearedStageId);
        }
    }
}
