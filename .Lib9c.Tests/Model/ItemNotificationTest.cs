namespace Lib9c.Tests.Model
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Battle;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Xunit;

    public class ItemNotificationTest
    {
        private readonly Address _agentAddress;
        private readonly TableSheets _tableSheets;
        private readonly ItemSubType[] _itemSubTypesForEquipments
            = new ItemSubType[]
            {
                ItemSubType.Weapon,
                ItemSubType.Armor,
                ItemSubType.Belt,
                ItemSubType.Necklace,
                ItemSubType.Ring,
            };

        public ItemNotificationTest()
        {
            var sheets = TableSheetsImporter.ImportSheets();

            var privateKey = new PrivateKey();
            _agentAddress = privateKey.PublicKey.ToAddress();
            _tableSheets = new TableSheets(sheets);
        }

        [Fact]
        public void EquipItems()
        {
            var avatarAddress = _agentAddress.Derive("avatar_1");
            var avatarState = new AvatarState(
                avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            avatarState.level = GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing1;
            var equipmentList = new List<Guid>();
            avatarState.EquipEquipments(equipmentList);

            var inventory = avatarState.inventory;
            var hasNotification = inventory.HasNotification(
                avatarState.level,
                0,
                _tableSheets.ItemRequirementSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheetV2,
                _tableSheets.EquipmentItemOptionSheet
            );
            // When inventory is empty.
            Assert.False(hasNotification);

            foreach (var type in _itemSubTypesForEquipments)
            {
                var rows = _tableSheets.EquipmentItemSheet.Values
                    .Where(r => r.ItemSubType == type && r.Grade == 1);
                foreach (var row in rows)
                {
                    var guid = Guid.NewGuid();
                    var equipment = (Equipment)ItemFactory.CreateItemUsable(row, guid, 0, 0);
                    inventory.AddItem(equipment);
                }

                hasNotification = inventory.HasNotification(
                    avatarState.level,
                    0,
                    _tableSheets.ItemRequirementSheet,
                    _tableSheets.EquipmentItemRecipeSheet,
                    _tableSheets.EquipmentItemSubRecipeSheetV2,
                    _tableSheets.EquipmentItemOptionSheet
                );
                // When all of the items are unequipped.
                Assert.True(hasNotification);

                var ordered = inventory.Equipments
                    .Where(i => i.ItemSubType == type)
                    .OrderBy(i => CPHelper.GetCP(i));

                var weakest = ordered.First();
                equipmentList.Add(weakest.ItemId);
                avatarState.EquipEquipments(equipmentList);
                hasNotification = inventory.HasNotification(
                    avatarState.level,
                    0,
                    _tableSheets.ItemRequirementSheet,
                    _tableSheets.EquipmentItemRecipeSheet,
                    _tableSheets.EquipmentItemSubRecipeSheetV2,
                    _tableSheets.EquipmentItemOptionSheet
                );
                // When weakest item is equipped.
                Assert.True(hasNotification);

                equipmentList.Remove(weakest.ItemId);

                var strongest = ordered.Last();
                equipmentList.Add(strongest.ItemId);
                avatarState.EquipEquipments(equipmentList);
                hasNotification = inventory.HasNotification(
                    avatarState.level,
                    0,
                    _tableSheets.ItemRequirementSheet,
                    _tableSheets.EquipmentItemRecipeSheet,
                    _tableSheets.EquipmentItemSubRecipeSheetV2,
                    _tableSheets.EquipmentItemOptionSheet
                );
                // When strongest item is equipped.
                Assert.False(hasNotification);
            }
        }

        [Fact]
        public void EquipTwoRings()
        {
            var avatarAddress = _agentAddress.Derive("avatar_2");
            var avatarState = new AvatarState(
                avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            avatarState.level = GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing2;
            var equipmentList = new List<Guid>();
            avatarState.EquipEquipments(equipmentList);

            var inventory = avatarState.inventory;
            var hasNotification = inventory.HasNotification(
                avatarState.level,
                0,
                _tableSheets.ItemRequirementSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheetV2,
                _tableSheets.EquipmentItemOptionSheet
            );
            // When inventory is empty.
            Assert.False(hasNotification);

            var rows = _tableSheets.EquipmentItemSheet.Values
                .Where(r => r.ItemSubType == ItemSubType.Ring && r.Grade == 1);
            foreach (var row in rows)
            {
                var guid = Guid.NewGuid();
                var equipment = (Equipment)ItemFactory.CreateItemUsable(row, guid, 0, 0);
                inventory.AddItem(equipment);
            }

            hasNotification = inventory.HasNotification(
                avatarState.level,
                0,
                _tableSheets.ItemRequirementSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheetV2,
                _tableSheets.EquipmentItemOptionSheet
            );
            // When all of the items are unequipped.
            Assert.True(hasNotification);

            var ordered = inventory.Equipments.OrderBy(i => CPHelper.GetCP(i));

            var strongest = ordered.Last();
            equipmentList.Add(strongest.ItemId);
            avatarState.EquipEquipments(equipmentList);
            hasNotification = inventory.HasNotification(
                avatarState.level,
                0,
                _tableSheets.ItemRequirementSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheetV2,
                _tableSheets.EquipmentItemOptionSheet
            );
            // When one strongest ring is equipped.
            Assert.True(hasNotification);

            equipmentList.Clear();

            var strongests = ordered.TakeLast(2).Select(i => i.ItemId);
            equipmentList.AddRange(strongests);
            avatarState.EquipEquipments(equipmentList);
            hasNotification = inventory.HasNotification(
                avatarState.level,
                0,
                _tableSheets.ItemRequirementSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheetV2,
                _tableSheets.EquipmentItemOptionSheet
            );
            // When the 1st strongest, the 2nd strongest items are equipped.
            Assert.False(hasNotification);
        }

        [Theory]
        [InlineData(0, 0, true)]
        [InlineData(0, 1, false)]
        public void RequiredBlockIndexTest(long blockIndex, long requiredBlockIndex, bool expected)
        {
            var avatarAddress = _agentAddress.Derive("avatar_2");
            var avatarState = new AvatarState(
                avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );

            avatarState.level = GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing2;
            var equipmentList = new List<Guid>();
            avatarState.EquipEquipments(equipmentList);

            var inventory = avatarState.inventory;
            var hasNotification = inventory.HasNotification(
                avatarState.level,
                blockIndex,
                _tableSheets.ItemRequirementSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheetV2,
                _tableSheets.EquipmentItemOptionSheet
            );
            // When inventory is empty.
            Assert.False(false);

            var rows = _tableSheets.EquipmentItemSheet.Values
                .Where(r => r.ItemSubType == ItemSubType.Ring && r.Grade == 1);
            foreach (var row in rows)
            {
                var guid = Guid.NewGuid();
                var equipment = (Equipment)ItemFactory.CreateItemUsable(row, guid, requiredBlockIndex, 0);
                inventory.AddItem(equipment);
            }

            hasNotification = inventory.HasNotification(
                avatarState.level,
                0,
                _tableSheets.ItemRequirementSheet,
                _tableSheets.EquipmentItemRecipeSheet,
                _tableSheets.EquipmentItemSubRecipeSheetV2,
                _tableSheets.EquipmentItemOptionSheet
            );
            Assert.Equal(blockIndex >= requiredBlockIndex, expected);
        }
    }
}
