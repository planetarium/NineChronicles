namespace Lib9c.Tests.TableData.Pet
{
    using System.Linq;
    using Nekoyume.TableData.Pet;
    using Xunit;

    public class PetSheetTest
    {
        [Fact]
        public void Set()
        {
            const string content = @"id,grade,soul_stone_ticker,_pet_name
1,1,Soulstone_001,D:CC 블랙캣";

            var sheet = new PetSheet();
            sheet.Set(content);

            Assert.Single(sheet);
            Assert.NotNull(sheet.First);
            var row = sheet.First;
            Assert.Equal(1, row.Id);
            Assert.Equal(1, row.Grade);
            Assert.Equal("Soulstone_001", row.SoulStoneTicker);
        }
    }
}
