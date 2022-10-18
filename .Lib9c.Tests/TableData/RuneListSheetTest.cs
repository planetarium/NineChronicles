namespace Lib9c.Tests.TableData
{
    using System;
    using Nekoyume.TableData;
    using Xunit;

    public class RuneListSheetTest
    {
        private RuneListSheet _runeListSheet;

        public RuneListSheetTest()
        {
            if (!TableSheetsImporter.TryGetCsv(nameof(RuneListSheet), out var csv))
            {
                throw new Exception($"Not found sheet: {nameof(RuneListSheet)}");
            }

            _runeListSheet = new RuneListSheet();
            _runeListSheet.Set(csv);
        }

        [Fact]
        public void SetToSheet()
        {
            const string content =
                @"id,grade,rune_type,required_level,use_place
250010001,1,1,1,7
        ";

            var sheet = new RuneListSheet();
            sheet.Set(content);

            Assert.Single(sheet);
            Assert.NotNull(sheet.First);
            Assert.Equal(250010001, sheet.First.Id);
            Assert.Equal(1, sheet.First.Grade);
            Assert.Equal(1, sheet.First.RuneType);
            Assert.Equal(1, sheet.First.RequiredLevel);
            Assert.Equal(7, sheet.First.UsePlace);
        }
    }
}
