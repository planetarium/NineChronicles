namespace Lib9c.Tests.Model.State
{
    using System.Collections.Generic;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume.Model.State;
    using Xunit;

    public class HammerPointStateTest
    {
        private readonly TableSheets _tableSheets;

        public HammerPointStateTest()
        {
            var sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(sheets);
        }

        [Fact]
        public void Serialize()
        {
            var address = new PrivateKey().ToAddress();
            var state = new HammerPointState(address, 1);
            state.UpdateHammerPoint(3);
            var serialized = state.Serialize();
            var deserialized = new HammerPointState(address, (List)serialized);

            Assert.Equal(state.Address, deserialized.Address);
            Assert.Equal(state.HammerPoint, deserialized.HammerPoint);
            Assert.Equal(state.ItemId, deserialized.ItemId);
        }

        [Fact]
        public void ResetHammerPoint()
        {
            var address = new PrivateKey().ToAddress();
            var state = new HammerPointState(address, 1);
            state.UpdateHammerPoint(3);
            var serialized = state.Serialize();
            var deserialized = new HammerPointState(address, (List)serialized);

            Assert.Equal(state.Address, deserialized.Address);
            Assert.Equal(state.HammerPoint, deserialized.HammerPoint);
            Assert.Equal(state.ItemId, deserialized.ItemId);

            state.ResetHammerPoint();
            var newSerialized = state.Serialize();
            deserialized = new HammerPointState(address, (List)newSerialized);
            Assert.Equal(state.HammerPoint, deserialized.HammerPoint);
        }
    }
}
