using System;
using System.Linq;
using Libplanet.Crypto;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State.Modifiers;
using NUnit.Framework;

namespace Tests.EditMode.State.Modifiers
{
    public class AvatarInventoryItemEquippedModifierTest
    {
        private TableSheets _tableSheets;
        private AvatarState _avatarState;

        [SetUp]
        public void SetUp()
        {
            _tableSheets = TableSheetsHelper.MakeTableSheets();
            _avatarState = new AvatarState(
                new PrivateKey().Address,
                new PrivateKey().Address,
                0,
                _tableSheets.GetAvatarSheets(),
                new PrivateKey().Address);
        }

        [TearDown]
        public void TearDown()
        {
            _avatarState = null;
            _tableSheets = null;
        }

        [Test]
        public void EquipmentTest()
        {
            var equipment = GetEquipment();
            _avatarState.inventory.AddItem(equipment);
            Assert.True(_avatarState.inventory.HasNonFungibleItem(equipment.ItemId));
            Assert.True(_avatarState.inventory.TryGetNonFungibleItem(
                equipment.ItemId,
                out _));
            Assert.False(equipment.Equipped);
            Assert.True(TryEquipModify(equipment));
            Assert.True(TryUnequipModify(equipment));
        }

        [Test]
        public void CostumeTest()
        {
            var costume = GetCostume();
            _avatarState.inventory.AddItem(costume);
            Assert.True(_avatarState.inventory.HasNonFungibleItem(costume.ItemId));
            Assert.True(_avatarState.inventory.TryGetNonFungibleItem(
                costume.ItemId,
                out _));
            Assert.False(costume.Equipped);
            Assert.True(TryEquipModify(costume));
            Assert.True(TryUnequipModify(costume));
        }

        [Test]
        public void ManyItemsTest()
        {
            var itemBases = new[]
            {
                (ItemBase) GetEquipment(),
                GetEquipment(),
                GetCostume(),
                GetCostume()
            };
            foreach (var itemBase in itemBases)
            {
                _avatarState.inventory.AddItem(itemBase);
                Assert.True(itemBase is INonFungibleItem);
                Assert.True(itemBase is IEquippableItem);
                var nonFungibleItem = (INonFungibleItem) itemBase;
                var equippableItem = (IEquippableItem) itemBase;
                Assert.True(_avatarState.inventory.HasNonFungibleItem(nonFungibleItem.NonFungibleId));
                Assert.True(_avatarState.inventory.TryGetNonFungibleItem(
                    nonFungibleItem.NonFungibleId,
                    out _));
                Assert.False(equippableItem.Equipped);
            }

            var nonFungibleItems = _avatarState.inventory.Items
                .Select(item => item.item)
                .OfType<INonFungibleItem>()
                .ToList();
            foreach (var nonFungibleItem in nonFungibleItems)
            {
                Assert.True(nonFungibleItem is ItemBase);
                var itemBase = (ItemBase) nonFungibleItem;
                Assert.True(TryEquipModify(itemBase));
            }

            foreach (var nonFungibleItem in nonFungibleItems)
            {
                Assert.True(nonFungibleItem is ItemBase);
                var itemBase = (ItemBase) nonFungibleItem;
                Assert.True(TryUnequipModify(itemBase));
            }
        }

        private Equipment GetEquipment() => new Equipment(
                _tableSheets.EquipmentItemSheet.First,
                Guid.NewGuid(),
                0);

        private Costume GetCostume() => new Costume(
            _tableSheets.CostumeItemSheet.First,
            Guid.NewGuid());

        private bool TryEquipModify(ItemBase itemBase)
        {
            if (!(itemBase is INonFungibleItem nonFungibleItem) ||
                !(itemBase is IEquippableItem equippableItem))
            {
                return false;
            }

            var modifier =
                new AvatarInventoryItemEquippedModifier(nonFungibleItem.NonFungibleId, true);
            modifier.Modify(_avatarState);
            Assert.True(equippableItem.Equipped);

            return true;
        }

        private bool TryUnequipModify(ItemBase itemBase)
        {
            if (!(itemBase is INonFungibleItem nonFungibleItem) ||
                !(itemBase is IEquippableItem equippableItem))
            {
                return false;
            }

            var modifier =
                new AvatarInventoryItemEquippedModifier(nonFungibleItem.NonFungibleId, false);
            modifier.Modify(_avatarState);
            Assert.False(equippableItem.Equipped);

            return true;
        }
    }
}
