namespace Lib9c.Tests.Model
{
    using System.Linq;
    using Nekoyume.Model;
    using Xunit;

    public class WorldInformationTest
    {
        [Fact]
        public void TryGetFirstWorld()
        {
            var sheets = new TableSheets(TableSheetsImporter.ImportSheets());
            var worldSheet = sheets.WorldSheet;
            var wi = new WorldInformation(Bencodex.Types.Dictionary.Empty);

            Assert.False(wi.TryGetFirstWorld(out var _));

            foreach (var row in worldSheet.Values.OrderByDescending(r => r.Id))
            {
                wi.TryAddWorld(row, out var _);
            }

            wi.TryGetFirstWorld(out var world);

            Assert.Equal(1, world.Id);
        }
    }
}
