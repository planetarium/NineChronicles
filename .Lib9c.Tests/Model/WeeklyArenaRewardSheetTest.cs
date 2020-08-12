namespace Lib9c.Tests.Model
{
    using System;
    using Nekoyume.TableData;
    using Xunit;

    public class WeeklyArenaRewardSheetTest : IDisposable
    {
        private TableSheets _tableSheets;

        public WeeklyArenaRewardSheetTest()
        {
            _tableSheets = new TableSheets();
        }

        public void Dispose()
        {
            _tableSheets = null;
        }

        [Fact]
        public void SetToSheet()
        {
            _tableSheets.SetToSheet(nameof(WeeklyArenaRewardSheet), "id,item_id,ratio,min,max\n1,2,0.1,0,1");

            Assert.NotNull(_tableSheets.WeeklyArenaRewardSheet);

            var row = _tableSheets.WeeklyArenaRewardSheet[1];
            var reward = row.Reward;

            Assert.Equal(1, row.Id);
            Assert.Equal(2, reward.ItemId);
            Assert.Equal(0.1m, reward.Ratio);
            Assert.Equal(0, reward.Min);
            Assert.Equal(1, reward.Max);
        }
    }
}
