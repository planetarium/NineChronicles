namespace Lib9c.Tests.TableData.Pet
{
    using System;
    using System.Linq;
    using Lib9c.Tests.Action;
    using Nekoyume.TableData.Pet;
    using Xunit;

    public class PetCostSheetTest
    {
        private PetCostSheet _petCostSheet;

        public PetCostSheetTest()
        {
            if (!TableSheetsImporter.TryGetCsv(nameof(PetCostSheet), out var csv))
            {
                throw new Exception($"Not found sheet: {nameof(PetCostSheet)}");
            }

            _petCostSheet = new PetCostSheet();
            _petCostSheet.Set(csv);
        }

        [Fact]
        public void SetToSheet()
        {
            const string content =
                @"ID,_PET NAME,PetLevel,SoulStoneQuantity,NcgQuantity
1,D:CC 블랙캣,1,10,0
        ";

            var sheet = new PetCostSheet();
            sheet.Set(content);

            Assert.Single(sheet);
            Assert.NotNull(sheet.First);
        }

        [Fact]
        public void GetRowTest()
        {
            const string content =
                @"ID,_PET NAME,PetLevel,SoulStoneQuantity,NcgQuantity
1,D:CC 블랙캣,1,10,0
        ";

            var sheet = new PetCostSheet();
            sheet.Set(content);

            var expectRow = sheet.First;
            Assert.NotNull(expectRow);
            Assert.True(expectRow.TryGetCost(1, out var cost));
            Assert.Equal(1, cost.Level);
            Assert.Equal(10, cost.SoulStoneQuantity);
            Assert.Equal(0, cost.NcgQuantity);
        }
    }
}
