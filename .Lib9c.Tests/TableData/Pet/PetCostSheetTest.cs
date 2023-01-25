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
                @"ID,_PET NAME,PetLevel,SoulStoneQuantity,NcgQuantity,PetFeedQuantity
1,D:CC 블랙캣,1,10,0,0
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
                @"ID,_PET NAME,PetLevel,SoulStoneQuantity,NcgQuantity,PetFeedQuantity
1,D:CC 블랙캣,1,10,100,100
        ";

            var sheet = new PetCostSheet();
            sheet.Set(content);

            var random = new TestRandom();
            var expectRow = sheet.OrderedList[random.Next(0, sheet.Count)];
            Assert.Equal(1, expectRow.Cost.First().Level);
            Assert.Equal(10, expectRow.Cost.First().SoulStoneQuantity);
            Assert.Equal(100, expectRow.Cost.First().NcgQuantity);
            Assert.Equal(100, expectRow.Cost.First().PetFeedQuantity);
        }
    }
}
