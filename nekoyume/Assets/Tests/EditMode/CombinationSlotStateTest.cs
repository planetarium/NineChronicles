using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Crypto;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class CombinationSlotStateTest
    {
        private TableSheets _tableSheets;

        [OneTimeSetUp]
        public void Init()
        {
            _tableSheets = new TableSheets();
            var request = Resources.Load<AddressableAssetsContainer>(Game.AddressableAssetsContainerPath);
            if (!(request is AddressableAssetsContainer addressableAssetsContainer))
                throw new FailedToLoadResourceException<AddressableAssetsContainer>(Game.AddressableAssetsContainerPath);

            var csvAssets = addressableAssetsContainer.tableCsvAssets;
            foreach (var asset in csvAssets)
            {
                _tableSheets.SetToSheet(asset.name, asset.text);
            }

            _tableSheets.ItemSheetInitialize();
            _tableSheets.QuestSheetInitialize();

        }

        [Test]
        public void CombinationSlotState()
        {
            var address = new PrivateKey().PublicKey.ToAddress();
            var state = new CombinationSlotState(address, 1);
            Assert.AreEqual(address, state.address);
            Assert.AreEqual(1, state.UnlockStage);
            Assert.AreEqual(0, state.UnlockBlockIndex);
        }

        [Test]
        public void SerializeCombinationSlotStateWithOutResult()
        {
            var address = new PrivateKey().PublicKey.ToAddress();
            var state = new CombinationSlotState(address, 1);
            var serialized = (Dictionary) state.Serialize();
            Assert.IsTrue(serialized.ContainsKey((Text) "address"));
            Assert.IsTrue(serialized.ContainsKey((Text) "unlockBlockIndex"));
            Assert.IsTrue(serialized.ContainsKey((Text) "unlockStage"));
            Assert.IsFalse(serialized.ContainsKey((Text) "result"));
            var deserialize = new CombinationSlotState(serialized);
            Assert.AreEqual(state.UnlockStage, deserialize.UnlockStage);
            Assert.AreEqual(state.UnlockBlockIndex, deserialize.UnlockBlockIndex);
            Assert.AreEqual(state.address, deserialize.address);
        }

        [Test]
        public void CombinationSlotStateUpdate()
        {
            var address = new PrivateKey().PublicKey.ToAddress();
            var state = new CombinationSlotState(address, 1);
            var result = new CombinationConsumable.ResultModel();
            state.Update(result, 10);
            Assert.AreEqual(result,state.Result);
            Assert.AreEqual(10, state.UnlockBlockIndex);
        }

        [Test]
        public void SerializeCombinationSlotStateWithResult()
        {
            var address = new PrivateKey().PublicKey.ToAddress();
            var state = new CombinationSlotState(address, 1);
            var item = ItemFactory.CreateItemUsable(_tableSheets.EquipmentItemSheet.Values.First(), Guid.Empty,
                default);
            var result = new CombinationConsumable.ResultModel
            {
                actionPoint = 1,
                gold = 1,
                materials = new Dictionary<Nekoyume.Model.Item.Material, int>(),
                itemUsable = item
            };
            state.Update(result, 10);
            var serialized = (Dictionary) state.Serialize();
            Assert.IsTrue(serialized.ContainsKey((Text) "address"));
            Assert.IsTrue(serialized.ContainsKey((Text) "unlockBlockIndex"));
            Assert.IsTrue(serialized.ContainsKey((Text) "unlockStage"));
            Assert.IsTrue(serialized.ContainsKey((Text) "result"));
            var deserialize = new CombinationSlotState(serialized);
            Assert.AreEqual(state.UnlockStage, deserialize.UnlockStage);
            Assert.AreEqual(state.UnlockBlockIndex, deserialize.UnlockBlockIndex);
            Assert.AreEqual(state.address, deserialize.address);
            Assert.AreEqual(state.Result.itemUsable, deserialize.Result.itemUsable);
        }
    }
}
