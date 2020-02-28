using System;
using System.Linq;
using Bencodex.Types;
using Nekoyume;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    public class ItemFactoryTest
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
        public void CreateMaterial()
        {
            var row = _tableSheets.MaterialItemSheet.Values.First();
            var material = ItemFactory.CreateMaterial(row);
            Assert.IsNotNull(material);
            var material2 = ItemFactory.CreateMaterial(_tableSheets.MaterialItemSheet, row.Id);
            Assert.AreEqual(material2, material);
        }

        [Test]
        public void SerializeMaterial()
        {
            var row = _tableSheets.MaterialItemSheet.Values.First();
            var material = ItemFactory.CreateMaterial(row);
            Assert.IsNotNull(material);
            var serialized = (Dictionary) material.Serialize();
            Assert.IsTrue(serialized.ContainsKey((Text)"data"));
            var deserialize = ItemFactory.Deserialize(serialized);
            Assert.AreEqual(deserialize, material);
        }

        [Test]
        public void CreateConsumable()
        {
            var row = _tableSheets.ConsumableItemSheet.Values.First();
            var guid = Guid.NewGuid();
            var consumable = ItemFactory.CreateItemUsable(row, guid, 1);
            Assert.IsNotNull(consumable);
            Assert.AreEqual(consumable.ItemId, guid);
            Assert.AreEqual(consumable.RequiredBlockIndex, 1);
        }

        [Test]
        public void SerializeConsumable()
        {
            var row = _tableSheets.ConsumableItemSheet.Values.First();
            var consumable = ItemFactory.CreateItemUsable(row, Guid.Empty, default);
            Assert.IsNotNull(consumable);
            var serialized = (Dictionary) consumable.Serialize();
            Assert.IsTrue(serialized.ContainsKey((Text)"data"));
            Assert.IsTrue(serialized.ContainsKey((Text)"itemId"));
            Assert.IsTrue(serialized.ContainsKey((Text)"statsMap"));
            Assert.IsTrue(serialized.ContainsKey((Text)"skills"));
            Assert.IsTrue(serialized.ContainsKey((Text)"buffSkills"));
            Assert.IsTrue(serialized.ContainsKey((Text)"requiredBlockIndex"));
            var deserialize = ItemFactory.Deserialize(serialized);
            Assert.AreEqual(deserialize, consumable);
        }

        [Test]
        public void CreateEquipment()
        {
            var row = _tableSheets.EquipmentItemSheet.Values.First();
            var guid = Guid.NewGuid();
            var equipment = ItemFactory.CreateItemUsable(row, guid, 1);
            Assert.IsNotNull(equipment);
            Assert.AreEqual(equipment.ItemId, guid);
            Assert.AreEqual(equipment.RequiredBlockIndex, 1);
        }

        [Test]
        public void SerializeEquipment()
        {
            var row = _tableSheets.EquipmentItemSheet.Values.First();
            var equipment = ItemFactory.CreateItemUsable(row, Guid.Empty, default);
            Assert.IsNotNull(equipment);
            var serialized = (Dictionary) equipment.Serialize();
            Assert.IsTrue(serialized.ContainsKey((Text)"data"));
            Assert.IsTrue(serialized.ContainsKey((Text)"itemId"));
            Assert.IsTrue(serialized.ContainsKey((Text)"statsMap"));
            Assert.IsTrue(serialized.ContainsKey((Text)"skills"));
            Assert.IsTrue(serialized.ContainsKey((Text)"buffSkills"));
            Assert.IsTrue(serialized.ContainsKey((Text)"requiredBlockIndex"));
            Assert.IsTrue(serialized.ContainsKey((Text)"equipped"));
            Assert.IsTrue(serialized.ContainsKey((Text)"level"));
            var deserialize = ItemFactory.Deserialize(serialized);
            Assert.AreEqual(deserialize, equipment);
        }
    }
}
