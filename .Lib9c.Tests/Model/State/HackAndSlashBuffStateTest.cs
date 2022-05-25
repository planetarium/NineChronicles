namespace Lib9c.Tests.Model.State
{
    using System.Collections.Generic;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Xunit;

    public class HackAndSlashBuffStateTest
    {
        private readonly TableSheets _tableSheets;

        public HackAndSlashBuffStateTest()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
        }

        [Fact]
        public void Serialize()
        {
            var address = new PrivateKey().ToAddress();
            var state = new HackAndSlashBuffState(address, 1);
            state.Update(100_000_000, _tableSheets.CrystalStageBuffGachaSheet);
            state.Update(new List<int> { 1, 2, 3 });
            var serialized = state.Serialize();
            var deserialized = new HackAndSlashBuffState(address, (List)serialized);

            Assert.Equal(state.Address, deserialized.Address);
            Assert.Equal(state.BuffIds, deserialized.BuffIds);
            Assert.Equal(state.StageId, deserialized.StageId);
            Assert.Equal(state.StarCount, deserialized.StarCount);
        }
    }
}
