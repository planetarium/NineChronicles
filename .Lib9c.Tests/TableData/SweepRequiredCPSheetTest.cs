namespace Lib9c.Tests.TableData
{
    using Nekoyume.TableData;
    using Xunit;

    public class SweepRequiredCPSheetTest
    {
        [Fact]
        public void Set()
        {
            const string csv = @"stage_id,required_cp
1,0";
            var sheet = new SweepRequiredCPSheet();
            sheet.Set(csv);
            Assert.Single(sheet);
            Assert.NotNull(sheet.First);

            var row = sheet.First;
            Assert.Equal(1, row.StageId);
            Assert.Equal(0, row.RequiredCP);
        }
    }
}
