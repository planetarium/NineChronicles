namespace Lib9c.Tests.TableData.Pet
{
    using Nekoyume.TableData.Pet;
    using Xunit;

    public class PetCostSheetTest
    {
        [Fact]
        public void Set()
        {
            const string content = @"pet_id,_pet_name,pet_level,soul_stone_quantity,ncg_quantity
1,D:CC 블랙캣,1,10,0
1,D:CC 블랙캣,2,0,10";

            var sheet = new PetCostSheet();
            sheet.Set(content);

            Assert.Single(sheet);
            Assert.NotNull(sheet.First);
            var row = sheet.First;
            Assert.Equal(1, row.PetId);
            Assert.Equal(2, row.Cost.Count);
            var cost = row.Cost[0];
            Assert.Equal(1, cost.Level);
            Assert.Equal(10, cost.SoulStoneQuantity);
            Assert.Equal(0, cost.NcgQuantity);
            cost = row.Cost[1];
            Assert.Equal(2, cost.Level);
            Assert.Equal(0, cost.SoulStoneQuantity);
            Assert.Equal(10, cost.NcgQuantity);
        }

        [Fact]
        public void Row_TryGetCost()
        {
            const string content = @"pet_id,_pet_name,pet_level,soul_stone_quantity,ncg_quantity
1,D:CC 블랙캣,1,10,0
1,D:CC 블랙캣,2,0,10";

            var sheet = new PetCostSheet();
            sheet.Set(content);

            Assert.NotNull(sheet.First);
            var row = sheet.First;
            Assert.False(row.TryGetCost(0, out var cost));
            Assert.True(row.TryGetCost(1, out cost));
            Assert.Equal(1, cost.Level);
            Assert.Equal(10, cost.SoulStoneQuantity);
            Assert.Equal(0, cost.NcgQuantity);
            Assert.True(row.TryGetCost(2, out cost));
            Assert.Equal(2, cost.Level);
            Assert.Equal(0, cost.SoulStoneQuantity);
            Assert.Equal(10, cost.NcgQuantity);
            Assert.False(row.TryGetCost(3, out cost));
        }
    }
}
