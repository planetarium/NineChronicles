using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using NUnit.Framework;

namespace Tests.EditMode
{
    public class CombinationSlotStateTest
    {
        private TableSheets _tableSheets;

        [OneTimeSetUp]
        public void Init()
        {
            _tableSheets = TableSheetsHelper.MakeTableSheets();
        }

        [Test]
        public void CombinationSlotState()
        {
            var address = new PrivateKey().PublicKey.Address;
            var state = new CombinationSlotState(address, 0);
            Assert.AreEqual(address, state.address);
            Assert.AreEqual(0, state.WorkCompleteBlockIndex);
        }

        [Test]
        public void SerializeCombinationSlotStateWithOutResult()
        {
            var address = new PrivateKey().PublicKey.Address;
            var state = new CombinationSlotState(address, 0);
            var serialized = (Dictionary) state.Serialize();
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "address"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "unlockBlockIndex"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "unlockStage"));
            Assert.IsFalse(serialized.ContainsKey((IKey)(Text) "result"));
            var deserialize = new CombinationSlotState(serialized);
            Assert.AreEqual(state.WorkCompleteBlockIndex, deserialize.WorkCompleteBlockIndex);
            Assert.AreEqual(state.address, deserialize.address);
        }

        [Test]
        public void CombinationSlotStateUpdate()
        {
            var address = new PrivateKey().PublicKey.Address;
            var state = new CombinationSlotState(address, 0);
            var result = new CombinationConsumable5.ResultModel();
            state.Update(result,1, 10);
            Assert.AreEqual(result,state.Result);
            Assert.AreEqual(10, state.WorkCompleteBlockIndex);
            Assert.AreEqual(1, state.WorkStartBlockIndex);
        }

        [Test]
        public void SerializeCombinationSlotStateWithResult()
        {
            var address = new PrivateKey().PublicKey.Address;
            var state = new CombinationSlotState(address, 0);
            var item = ItemFactory.CreateItemUsable(_tableSheets.EquipmentItemSheet.Values.First(), Guid.Empty,
                default);
            var result = new CombinationConsumable5.ResultModel
            {
                actionPoint = 1,
                gold = 1,
                materials = new Dictionary<Nekoyume.Model.Item.Material, int>(),
                itemUsable = item
            };
            state.Update(result, 1,10);
            var serialized = (Dictionary) state.Serialize();
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "address"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "unlockBlockIndex"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "unlockStage"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "result"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "startBlockIndex"));
            var deserialize = new CombinationSlotState(serialized);
            Assert.AreEqual(state.WorkCompleteBlockIndex, deserialize.WorkCompleteBlockIndex);
            Assert.AreEqual(state.address, deserialize.address);
            Assert.AreEqual(state.Result.itemUsable, deserialize.Result.itemUsable);
            Assert.AreEqual(state.WorkStartBlockIndex, deserialize.WorkStartBlockIndex);
        }
    }
}
