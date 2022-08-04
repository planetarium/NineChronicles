namespace Lib9c.Tests.TableData.Crystal
{
    using System.Linq;
    using Nekoyume.TableData.Crystal;
    using Xunit;

    public class CrystalHammerPointSheetTest
    {
        [Fact]
        public void Set()
        {
            var csv = @"recipe_ID,max_hammer_point,crystal_cost
1,3,10";
            var sheet = new CrystalHammerPointSheet();
            sheet.Set(csv);
            var row = sheet.First().Value;

            Assert.Equal(1, row.RecipeId);
            Assert.Equal(3, row.MaxPoint);
            Assert.Equal(10, row.CRYSTAL);
        }
    }
}
