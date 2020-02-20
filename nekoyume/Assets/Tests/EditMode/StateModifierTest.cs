using System;
using Libplanet;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State.Modifiers;
using Nekoyume.TableData;
using NUnit.Framework;
using UnityEngine;
using Material = Nekoyume.Model.Item.Material;

namespace Tests.EditMode
{
    public class StateModifierTest
    {
        private TableSheets _tableSheets;
        private AgentState _agentState;
        private AvatarState _avatarState;

        [SetUp]
        public void SetUp()
        {
            _tableSheets = new TableSheets();
            _agentState = new AgentState(new Address());
            _avatarState = new AvatarState(new Address(), _agentState.address, 0, null, null);
        }

        [TearDown]
        public void TearDown()
        {
            _avatarState = null;
            _agentState = null;
            _tableSheets = null;
        }

        [Test]
        public void AgentGoldModifier()
        {
            var gold = _agentState.gold;
            var modifier = JsonTest(new AgentGoldModifier(100));
            modifier.Modify(_agentState);
            Assert.AreEqual(gold + 100, _agentState.gold);
        }

        [Test]
        public void AvatarActionPointModifier()
        {
            var actionPoint = _avatarState.actionPoint;
            var modifier = JsonTest(new AvatarActionPointModifier(100));
            modifier.Modify(_avatarState);
            Assert.AreEqual(actionPoint + 100, _avatarState.actionPoint);
        }

        [Test]
        public void AvatarInventoryFungibleItemRemover()
        {
            if (_tableSheets.MaterialItemSheet.First is null)
                return;

            var material = GetFirstMaterial();
            _avatarState.inventory.AddItem(material);
            Assert.True(_avatarState.inventory.HasItem(material.Data.ItemId));
            var modifier =
                JsonTest(new AvatarInventoryFungibleItemRemover(material.Data.ItemId, 1));
            modifier.Modify(_avatarState);
            Assert.False(_avatarState.inventory.HasItem(material.Data.ItemId));
        }

        [Test]
        public void AvatarInventoryNonFungibleItemRemover()
        {
            if (_tableSheets.MaterialItemSheet.First is null)
                return;

            var equipment = GetFirstEquipment();
            _avatarState.inventory.AddItem(equipment);
            Assert.True(_avatarState.inventory.HasItem(equipment.ItemId));
            var modifier =
                JsonTest(new AvatarInventoryNonFungibleItemRemover(equipment.ItemId));
            modifier.Modify(_avatarState);
            Assert.False(_avatarState.inventory.HasItem(equipment.ItemId));
        }

        [Test]
        public void AvatarNewAttachmentMailSetter()
        {
            var equipment = GetFirstEquipment();
            var combinationResult = new Combination.ResultModel
            {
                itemUsable = equipment
            };
            var attachmentMail = new CombinationMail(combinationResult, 0, new Guid()) {New = false};
            Assert.False(attachmentMail.New);
            _avatarState.mailBox.Add(attachmentMail);
            var modifier =
                JsonTest(new AvatarAttachmentMailNewSetter(attachmentMail.id));
            modifier.Modify(_avatarState);
            Assert.True(attachmentMail.New);
        }

        private static T JsonTest<T>(T modifier)
        {
            var jsonString = JsonUtility.ToJson(modifier);
            return JsonUtility.FromJson<T>(jsonString);
        }

        private Equipment GetFirstEquipment()
        {
            var equipmentRowFirst = _tableSheets.EquipmentItemSheet.First;
            return new Equipment(equipmentRowFirst, new Guid());
        }

        private Material GetFirstMaterial()
        {
            var materialRowFirst = _tableSheets.MaterialItemSheet.First;
            return new Material(materialRowFirst);
        }
    }
}
