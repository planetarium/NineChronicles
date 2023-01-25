namespace Lib9c.Tests.TableData.Pet
{
    using System;
    using Nekoyume.Model.Pet;
    using Nekoyume.TableData.Pet;
    using Xunit;

    public class PetOptionSheetTest
    {
        private PetOptionSheet _petOptionSheet;

        public PetOptionSheetTest()
        {
            if (!TableSheetsImporter.TryGetCsv(nameof(PetOptionSheet), out var csv))
            {
                throw new Exception($"Not found sheet: {nameof(PetOptionSheet)}");
            }

            _petOptionSheet = new PetOptionSheet();
            _petOptionSheet.Set(csv);
        }

        [Fact]
        public void SetToSheet()
        {
            const string content =
                @"ID,_PET NAME,PetLevel,OptionType,OptionValue
1,D:CC 블랙캣,1,ReduceRequiredBlockByPercent,5.5
        ";

            var sheet = new PetOptionSheet();
            sheet.Set(content);

            Assert.Single(sheet);
            Assert.NotNull(sheet.First);
        }

        [Fact]
        public void GetRowTest()
        {
            const string content =
                @"ID,_PET NAME,PetLevel,OptionType,OptionValue
1,D:CC 블랙캣,1,ReduceRequiredBlockByPercent,5.5
        ";

            var sheet = new PetOptionSheet();
            sheet.Set(content);

            var expectRow = sheet.First;
            Assert.NotNull(expectRow);
            Assert.Equal(1, expectRow.PetId);
            Assert.NotNull(expectRow.LevelOptionMap[1]);
            Assert.Equal(PetOptionType.ReduceRequiredBlockByPercent, expectRow.LevelOptionMap[1].OptionType);
            Assert.Equal(5.5M, expectRow.LevelOptionMap[1].OptionValue);
        }
    }
}
