using System;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class InventoryItem
    {
        public ItemBase ItemBase { get; }
        public RuneState RuneState { get; }
        public FungibleAssetValue FungibleAssetValue { get; }

        public readonly ReactiveProperty<int> Count;
        public readonly ReactiveProperty<bool> LevelLimited;
        public readonly ReactiveProperty<bool> Equipped = new();
        public readonly ReactiveProperty<bool> Tradable;
        public readonly ReactiveProperty<bool> DimObjectEnabled;
        public readonly ReactiveProperty<bool> Selected;
        public readonly ReactiveProperty<bool> Focused;
        public readonly ReactiveProperty<bool> HasNotification;
        public readonly ReactiveProperty<int> GrindingCount;
        public readonly ReactiveProperty<bool> Disabled;
        public readonly Subject<bool> GrindingCountEnabled;

        public InventoryItem(ItemBase itemBase, int count, bool limited, bool tradable)
        {
            ItemBase = itemBase;
            Count = new ReactiveProperty<int>(count);
            // Equipped = new ReactiveProperty<bool>(false);
            LevelLimited = new ReactiveProperty<bool>(limited);
            Tradable = new ReactiveProperty<bool>(tradable);
            DimObjectEnabled = new ReactiveProperty<bool>(false);
            Selected = new ReactiveProperty<bool>(false);
            Focused = new ReactiveProperty<bool>(false);
            HasNotification = new ReactiveProperty<bool>(false);
            GrindingCount = new ReactiveProperty<int>(0);
            Disabled = new ReactiveProperty<bool>(false);
            GrindingCountEnabled = new Subject<bool>();
        }

        public InventoryItem(RuneState runeState)
        {
            RuneState = runeState;
            Count = new ReactiveProperty<int>(1);
            // Equipped = new ReactiveProperty<bool>(false);
            LevelLimited = new ReactiveProperty<bool>(false);
            Tradable = new ReactiveProperty<bool>(false);
            DimObjectEnabled = new ReactiveProperty<bool>(false);
            Selected = new ReactiveProperty<bool>(false);
            Focused = new ReactiveProperty<bool>(false);
            HasNotification = new ReactiveProperty<bool>(false);
            GrindingCount = new ReactiveProperty<int>(0);
            Disabled = new ReactiveProperty<bool>(false);
            GrindingCountEnabled = new Subject<bool>();
        }

        public InventoryItem(FungibleAssetValue fungibleAssetValue)
        {
            FungibleAssetValue = fungibleAssetValue;
            var count = Convert.ToInt32(fungibleAssetValue.GetQuantityString());
            Count = new ReactiveProperty<int>(count);
            Equipped = new ReactiveProperty<bool>(false);
            LevelLimited = new ReactiveProperty<bool>(false);
            Tradable = new ReactiveProperty<bool>(false);
            DimObjectEnabled = new ReactiveProperty<bool>(count <= 0);
            Selected = new ReactiveProperty<bool>(false);
            Focused = new ReactiveProperty<bool>(false);
            HasNotification = new ReactiveProperty<bool>(false);
            GrindingCount = new ReactiveProperty<int>(0);
            Disabled = new ReactiveProperty<bool>(false);
            GrindingCountEnabled = new Subject<bool>();
        }
    }

    public static class InventoryItemExtensions
    {
        public static bool IsValid(this InventoryItem inventoryItem, int level)
        {
            switch (inventoryItem.ItemBase.ItemType)
            {
                case ItemType.Costume:
                    switch (inventoryItem.ItemBase.ItemSubType)
                    {
                        case ItemSubType.FullCostume:
                            return level >= GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot;
                        case ItemSubType.Title:
                            return level >= GameConfig.RequireCharacterLevel.CharacterTitleSlot;
                            break;
                    }
                    break;
                case ItemType.Equipment:
                    switch (inventoryItem.ItemBase.ItemSubType)
                    {
                        case ItemSubType.Weapon:
                            return level >= GameConfig.RequireCharacterLevel.CharacterEquipmentSlotWeapon;
                        case ItemSubType.Armor:
                            return level >= GameConfig.RequireCharacterLevel.CharacterEquipmentSlotArmor;
                        case ItemSubType.Belt:
                            return level >= GameConfig.RequireCharacterLevel.CharacterEquipmentSlotBelt;
                        case ItemSubType.Necklace:
                            return level >= GameConfig.RequireCharacterLevel.CharacterEquipmentSlotNecklace;
                        case ItemSubType.Ring:
                            return level >= GameConfig.RequireCharacterLevel.CharacterEquipmentSlotRing1;
                    }
                    break;
                case ItemType.Consumable:
                    return level >= GameConfig.RequireCharacterLevel.CharacterConsumableSlot1;

            }

            return false;
        }
    }
}
