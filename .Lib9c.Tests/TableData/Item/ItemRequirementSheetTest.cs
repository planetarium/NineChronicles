namespace Lib9c.Tests.TableData.Item
{
    using Nekoyume.TableData;
    using Xunit;

    public class ItemRequirementSheetTest
    {
        private const string _csv =
            @"item_id,level
10100000,1";

        [Theory]
        [InlineData(_csv)]
        public void Set(string csv)
        {
            var sheet = new ItemRequirementSheet();
            sheet.Set(csv);
            Assert.Single(sheet);
            Assert.NotNull(sheet.First);

            var first = sheet.First;
            Assert.Equal(first.ItemId, first.Key);
            Assert.Equal(10100000, first.ItemId);
            Assert.Equal(1, first.Level);
        }
    }
}
