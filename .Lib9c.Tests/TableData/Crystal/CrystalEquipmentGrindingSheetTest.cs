namespace Lib9c.Tests.TableData.Crystal
{
    using System.Linq;
    using Nekoyume.TableData.Crystal;
    using Xunit;

    public class CrystalEquipmentGrindingSheetTest
    {
        [Fact]
        public void Set()
        {
            var csv = @"id,enchant_base_id,gain_crystal
10100000,10100000,10";
            var sheet = new CrystalEquipmentGrindingSheet();
            sheet.Set(csv);
            var row = sheet.First().Value;

            Assert.Equal(10100000, row.ItemId);
            Assert.Equal(10100000, row.EnchantBaseId);
            Assert.Equal(10, row.CRYSTAL);
        }
    }
}
