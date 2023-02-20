namespace Lib9c.Tests.TableData.Pet
{
    using System;
    using Nekoyume.TableData.Pet;
    using Xunit;

    public class PetSheetTest
    {
        private PetSheet _petSheet;

        public PetSheetTest()
        {
            if (!TableSheetsImporter.TryGetCsv(nameof(PetSheet), out var csv))
            {
                throw new Exception($"Not found sheet: {nameof(PetSheet)}");
            }

            _petSheet = new PetSheet();
            _petSheet.Set(csv);
        }

        [Fact]
        public void SetToSheet()
        {
            const string content =
                @"id,grade,SoulStoneTicker,_petName
1,1,Soulstone_001,D:CC 블랙캣
        ";

            var sheet = new PetSheet();
            sheet.Set(content);

            Assert.Single(sheet);
            Assert.NotNull(sheet.First);
            Assert.Equal(1, sheet.First.Id);
            Assert.Equal(1, sheet.First.Grade);
            Assert.Equal("Soulstone_001", sheet.First.SoulStoneTicker);
        }
    }
}
