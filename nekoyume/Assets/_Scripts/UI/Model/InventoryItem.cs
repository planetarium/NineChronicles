using System;
using System.Linq;
using System.Numerics;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using UniRx;

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
        public readonly ReactiveProperty<bool> Disabled;
        public readonly ReactiveProperty<bool> GrindingCountEnabled;
        public readonly ReactiveProperty<bool> CollectionSelected;

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
            Disabled = new ReactiveProperty<bool>(false);
            GrindingCountEnabled = new ReactiveProperty<bool>(false);
            CollectionSelected = new ReactiveProperty<bool>(false);
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
            Disabled = new ReactiveProperty<bool>(false);
            GrindingCountEnabled = new ReactiveProperty<bool>(false);
            CollectionSelected = new ReactiveProperty<bool>(false);
        }

        public InventoryItem(FungibleAssetValue fungibleAssetValue)
        {
            FungibleAssetValue = fungibleAssetValue;
            var count = MathematicsExtensions.ConvertToInt32(fungibleAssetValue.GetQuantityString());
            Count = new ReactiveProperty<int>(count);
            Equipped = new ReactiveProperty<bool>(false);
            LevelLimited = new ReactiveProperty<bool>(false);
            Tradable = new ReactiveProperty<bool>(!RegisterProduct.NonTradableTickerCurrencies.Contains(fungibleAssetValue.Currency));
            DimObjectEnabled = new ReactiveProperty<bool>(count <= 0);
            Selected = new ReactiveProperty<bool>(false);
            Focused = new ReactiveProperty<bool>(false);
            HasNotification = new ReactiveProperty<bool>(false);
            Disabled = new ReactiveProperty<bool>(false);
            GrindingCountEnabled = new ReactiveProperty<bool>(false);
            CollectionSelected = new ReactiveProperty<bool>(false);
        }
    }

    public static class InventoryItemExtensions
    {
        public static bool IsValid(this InventoryItem inventoryItem, int level, GameConfigState gameConfig)
        {
            switch (inventoryItem.ItemBase.ItemType)
            {
                case ItemType.Costume:
                    switch (inventoryItem.ItemBase.ItemSubType)
                    {
                        case ItemSubType.FullCostume:
                            return level >= gameConfig.RequireCharacterLevel_FullCostumeSlot;
                        case ItemSubType.Title:
                            return level >= gameConfig.RequireCharacterLevel_TitleSlot;
                    }
                    break;
                case ItemType.Equipment:
                    switch (inventoryItem.ItemBase.ItemSubType)
                    {
                        case ItemSubType.Weapon:
                            return level >= gameConfig.RequireCharacterLevel_EquipmentSlotWeapon;
                        case ItemSubType.Armor:
                            return level >= gameConfig.RequireCharacterLevel_EquipmentSlotArmor;
                        case ItemSubType.Belt:
                            return level >= gameConfig.RequireCharacterLevel_EquipmentSlotBelt;
                        case ItemSubType.Necklace:
                            return level >= gameConfig.RequireCharacterLevel_EquipmentSlotNecklace;
                        case ItemSubType.Ring:
                            return level >= gameConfig.RequireCharacterLevel_EquipmentSlotRing1;
                        case ItemSubType.Aura:
                            return level >= gameConfig.RequireCharacterLevel_EquipmentSlotAura;
                    }
                    break;
                case ItemType.Consumable:
                    return level >= gameConfig.RequireCharacterLevel_ConsumableSlot1;

            }

            return false;
        }
    }
}
