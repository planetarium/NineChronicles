using System;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State.Modifiers;
using NUnit.Framework;
using UnityEngine;
using Material = Nekoyume.Model.Item.Material;

namespace Tests.EditMode
{
    public class StateModifierTest
    {
        private TableSheets _tableSheets;
        private AgentState _agentState;
        private FungibleAssetValue _ncgBalance;
        private AvatarState _avatarState;

        [SetUp]
        public void SetUp()
        {
            _tableSheets = TableSheetsHelper.MakeTableSheets();
            _agentState = new AgentState(new Address());
            var currency = Currency.Legacy("NCG", 2, null);
            _ncgBalance = new FungibleAssetValue(currency, 0, 0);
            _avatarState = new AvatarState(
                new Address(),
                _agentState.address,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                new Address());
        }

        [Test]
        public void AgentGoldModifier()
        {
            var gold = _ncgBalance;
            var modifier = JsonTest(new AgentNCGModifier(gold.Currency * 100));
            _ncgBalance = modifier.Modify(_ncgBalance);
            Assert.AreEqual(gold + new FungibleAssetValue(gold.Currency, 100, 0), _ncgBalance);
        }

        [Test]
        public void AvatarActionPointModifier()
        {
            var actionPoint = _avatarState.actionPoint;
            var modifier = JsonTest(new AvatarActionPointModifier(100));
            _avatarState = modifier.Modify(_avatarState);
            Assert.AreEqual(actionPoint + 100, _avatarState.actionPoint);
        }

        [Test]
        public void AvatarInventoryFungibleItemRemover()
        {
            if (_tableSheets.MaterialItemSheet.First is null)
                return;

            var material = GetFirstMaterial();
            _avatarState.inventory.AddItem(material);
            Assert.True(_avatarState.inventory.HasFungibleItem(material.ItemId, Game.instance.Agent.BlockIndex));
            var modifier =
                JsonTest(new AvatarInventoryFungibleItemRemover(material.ItemId, 1));
            _avatarState = modifier.Modify(_avatarState);
            Assert.False(_avatarState.inventory.HasFungibleItem(material.ItemId, Game.instance.Agent.BlockIndex));
        }

        [Test]
        public void AvatarInventoryTradableItemRemover()
        {
            if (_tableSheets.MaterialItemSheet.First is null)
                return;

            var equipment = GetFirstEquipment();
            _avatarState.inventory.AddItem(equipment);
            Assert.True(_avatarState.inventory.HasTradableItem(equipment.ItemId, equipment.RequiredBlockIndex, 1));
            var modifier =
                JsonTest(new AvatarInventoryTradableItemRemover(equipment.ItemId,
                    equipment.RequiredBlockIndex,
                    1));
            _avatarState = modifier.Modify(_avatarState);
            Assert.False(_avatarState.inventory.HasNonFungibleItem(equipment.ItemId));
        }

        [Test]
        public void AvatarNewAttachmentMailSetter()
        {
            var equipment = GetFirstEquipment();
            var combinationResult = new CombinationConsumable5.ResultModel
            {
                itemUsable = equipment
            };
            var attachmentMail = new CombinationMail(combinationResult, 0, new Guid(), 0);
            Assert.False(attachmentMail.New);
            _avatarState.mailBox.Add(attachmentMail);
            var modifier =
                JsonTest(new AvatarAttachmentMailNewSetter(attachmentMail.id));
            _avatarState = modifier.Modify(_avatarState);
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
            return new Equipment(equipmentRowFirst, new Guid(), 0);
        }

        private Material GetFirstMaterial()
        {
            var materialRowFirst = _tableSheets.MaterialItemSheet.First;
            return new Material(materialRowFirst);
        }
    }
}
