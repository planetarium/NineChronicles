using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
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
                throw new FailedToLoadResourceException<AddressableAssetsContainer>(
                    Game.AddressableAssetsContainerPath);

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
        public void CreateChest()
        {
            var row = _tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Chest);
            var chest = ItemFactory.CreateChest(row, null);
            Assert.IsNotNull(chest);
            Assert.AreEqual(chest.Rewards, new List<RedeemRewardSheet.RewardInfo>());

            var rewards = _tableSheets.RedeemRewardSheet.Values.First().Rewards;
            var chest2 = ItemFactory.CreateChest(row, rewards);
            Assert.IsNotNull(chest2);
            Assert.AreEqual(chest2.Rewards, rewards);
            Assert.AreNotEqual(chest, chest2);
        }

        [Test]
        public void SerializeMaterial()
        {
            var row = _tableSheets.MaterialItemSheet.Values.First();
            var material = ItemFactory.CreateMaterial(row);
            Assert.IsNotNull(material);
            var serialized = (Dictionary) material.Serialize();
            Assert.IsFalse(serialized.ContainsKey((IKey)(Text) "data"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "item_id"));
            var deserialize = ItemFactory.Deserialize(serialized);
            Assert.AreEqual(deserialize, material);
        }

        [Test]
        public void SerializeMaterialLegacy()
        {
            var row = _tableSheets.MaterialItemSheet.Values.First();
            var material = ItemFactory.CreateMaterial(row);
            Assert.IsNotNull(material);
            var serialized = (Dictionary) material.Serialize();
            var legacy = new Dictionary(
                Dictionary.Empty
                    .Add("data", row.Serialize())
            );
            Assert.IsTrue(legacy.ContainsKey((IKey)(Text) "data"));
            var deserialize = ItemFactory.Deserialize(legacy);
            Assert.AreEqual(material, deserialize);
            Assert.AreEqual(material, ItemFactory.Deserialize(serialized));
        }

        [Test]
        public void SerializeConsumable()
        {
            var row = _tableSheets.ConsumableItemSheet.Values.First();
            var consumable = ItemFactory.CreateItemUsable(row, Guid.Empty, default);
            Assert.IsNotNull(consumable);
            var serialized = (Dictionary) consumable.Serialize();
            Assert.IsFalse(serialized.ContainsKey((IKey)(Text) "data"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "itemId"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "statsMap"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "skills"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "buffSkills"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "requiredBlockIndex"));
            var deserialize = ItemFactory.Deserialize(serialized);
            Assert.AreEqual(deserialize, consumable);
        }

        [Test]
        public void SerializeConsumableLegacy()
        {
            var row = _tableSheets.ConsumableItemSheet.Values.First();
            var consumable = ItemFactory.CreateItemUsable(row, Guid.Empty, default);
            Assert.IsNotNull(consumable);
            var serialized = (Dictionary) consumable.Serialize();
            var legacy = new Dictionary(
                Dictionary.Empty
                    .Add("data", row.Serialize())
            );
            Assert.IsTrue(legacy.ContainsKey((IKey)(Text) "data"));
            var deserialize = ItemFactory.Deserialize(legacy);
            Assert.AreEqual(consumable, deserialize);
            Assert.AreEqual(consumable, ItemFactory.Deserialize(serialized));
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
        public void SerializeEquipment([Values(true, false)] bool equip)
        {
            var row = _tableSheets.EquipmentItemSheet.Values.First();
            var equipment = (Weapon) ItemFactory.CreateItemUsable(row, Guid.Empty, default);
            Assert.IsNotNull(equipment);
            equipment.equipped = equip;
            var serialized = (Dictionary) equipment.Serialize();
            Assert.IsFalse(serialized.ContainsKey((IKey)(Text) "data"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "itemId"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "statsMap"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "skills"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "buffSkills"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "requiredBlockIndex"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "equipped"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "level"));
            var deserialize = ItemFactory.Deserialize(serialized);
            Assert.AreEqual(equipment, deserialize);
        }

        [Test]
        public void SerializeEquipmentLegacy([Values(true, false)] bool equip)
        {
            var row = _tableSheets.EquipmentItemSheet.Values.First();
            var equipment = (Weapon) ItemFactory.CreateItemUsable(row, Guid.Empty, default);
            equipment.equipped = equip;
            Assert.IsNotNull(equipment);
            var legacy = new Dictionary(
                Dictionary.Empty
                    .Add("data", row.Serialize())
                    .Add("equipped", equipment.equipped.Serialize())
                    .Add("level", equipment.level)
                    .Add("statsMap", equipment.StatsMap.Serialize())
                    .Add("skills", new Bencodex.Types.List(equipment.Skills.Select(s => s.Serialize())).Value)
                    .Add("buffSkills", new Bencodex.Types.List(equipment.BuffSkills.Select(s => s.Serialize())).Value)
                    .Add("requiredBlockIndex", equipment.RequiredBlockIndex.Serialize())
            );
            var serialized = (Dictionary) equipment.Serialize();
            Assert.IsTrue(legacy.ContainsKey((IKey)(Text) "data"));
            Assert.IsTrue(legacy.ContainsKey((IKey)(Text) "equipped"));
            Assert.IsTrue(legacy.ContainsKey((IKey)(Text) "level"));
            Assert.IsTrue(legacy.ContainsKey((IKey)(Text) "statsMap"));
            Assert.IsTrue(legacy.ContainsKey((IKey)(Text) "skills"));
            Assert.IsTrue(legacy.ContainsKey((IKey)(Text) "buffSkills"));
            Assert.IsTrue(legacy.ContainsKey((IKey)(Text) "requiredBlockIndex"));
            var deserialize = ItemFactory.Deserialize(legacy);
            Assert.AreEqual(equipment, deserialize);
            Assert.AreEqual(equipment, ItemFactory.Deserialize(serialized));
        }

        [Test]
        public void SerializeCostume([Values(true, false)] bool equip)
        {
            var row = _tableSheets.CostumeItemSheet.Values.First();
            var costume = ItemFactory.CreateCostume(row);
            Assert.IsNotNull(costume);
            costume.equipped = equip;
            var serialized = (Dictionary) costume.Serialize();
            Assert.IsFalse(serialized.ContainsKey((IKey)(Text) "data"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "id"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "item_type"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "item_sub_type"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "grade"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "elemental_type"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "equipped"));
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "spine_resource_path"));
            var deserialize = ItemFactory.Deserialize(serialized);
            Assert.AreEqual(costume, deserialize);
        }

        [Test]
        public void SerializeCostumeLegacy([Values(true, false)] bool equip)
        {
            var row = _tableSheets.CostumeItemSheet.Values.First();
            var costume = ItemFactory.CreateCostume(row);
            costume.equipped = equip;
            Assert.IsNotNull(costume);
            var serialized = (Dictionary) costume.Serialize();
            var legacy = new Dictionary(
                Dictionary.Empty
                    .Add("data", row.Serialize())
                    .Add("equipped", costume.equipped.Serialize())
            );
            Assert.IsTrue(legacy.ContainsKey((IKey)(Text) "data"));
            Assert.IsTrue(legacy.ContainsKey((IKey)(Text) "equipped"));
            var deserialize = ItemFactory.Deserialize(legacy);
            Assert.AreEqual(costume, deserialize);
            Assert.AreEqual(costume, ItemFactory.Deserialize(serialized));
        }

        [Test]
        public void SerializeChest()
        {
            var row = _tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Chest);
            var chest = ItemFactory.CreateChest(row, null);
            Assert.IsNotNull(chest);
            Assert.AreEqual(chest.Rewards, new List<RedeemRewardSheet.RewardInfo>());
            var serialized = (Dictionary) chest.Serialize();
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "rewards"));
            Assert.AreEqual(chest, ItemFactory.Deserialize(serialized));

            var rewards = _tableSheets.RedeemRewardSheet.Values.First().Rewards;
            var chest2 = ItemFactory.CreateChest(row, rewards);
            Assert.IsNotNull(chest2);
            Assert.AreEqual(chest2.Rewards, rewards);
            var serialized2 = (Dictionary) chest2.Serialize();
            Assert.IsTrue(serialized2.ContainsKey((IKey)(Text) "rewards"));
            Assert.AreEqual(chest2, ItemFactory.Deserialize(serialized2));
            Assert.AreNotEqual(serialized, serialized2);
            Assert.AreNotEqual(chest, chest2);
            Assert.AreNotEqual(chest.ItemId, chest2.ItemId);
        }

        [Test]
        public void SerializeChestDoesNotThrowArgumentNullException()
        {
            var row = _tableSheets.MaterialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.Chest);
            var chestMaterial = ItemFactory.CreateMaterial(row);
            var chest = ItemFactory.CreateChest(row, null);
            Assert.IsNotNull(chestMaterial);
            Assert.DoesNotThrow(() => chestMaterial.Serialize());
            var serialized = (Dictionary) chestMaterial.Serialize();
            Assert.IsTrue(serialized.ContainsKey((IKey)(Text) "rewards"));
            Assert.AreEqual(chest, ItemFactory.Deserialize(serialized));
        }
    }
}
