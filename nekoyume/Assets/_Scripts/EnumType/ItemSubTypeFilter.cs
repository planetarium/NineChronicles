using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;

namespace Nekoyume.EnumType
{
    public enum ItemSubTypeFilter
    {
        All,
        Weapon,
        Armor,
        Belt,
        Necklace,
        Ring,

        Food_HP,
        Food_ATK,
        Food_DEF,
        Food_CRI,
        Food_HIT,

        FullCostume,
        HairCostume,
        EarCostume,
        EyeCostume,
        TailCostume,
        Title,
        Materials,

        Equipment,
        Food,
        Costume,
    }

    public static class ItemSubTypeFilterExtension
    {
        public static IEnumerable<ItemSubTypeFilter> ItemSubTypeFilters
        {
            get
            {
                return new[]
                {
                    ItemSubTypeFilter.All,
                    ItemSubTypeFilter.Weapon,
                    ItemSubTypeFilter.Armor,
                    ItemSubTypeFilter.Belt,
                    ItemSubTypeFilter.Necklace,
                    ItemSubTypeFilter.Ring,
                    ItemSubTypeFilter.Food_HP,
                    ItemSubTypeFilter.Food_ATK,
                    ItemSubTypeFilter.Food_DEF,
                    ItemSubTypeFilter.Food_CRI,
                    ItemSubTypeFilter.Food_HIT,
                    ItemSubTypeFilter.FullCostume,
                    ItemSubTypeFilter.HairCostume,
                    ItemSubTypeFilter.EarCostume,
                    ItemSubTypeFilter.EyeCostume,
                    ItemSubTypeFilter.TailCostume,
                    ItemSubTypeFilter.Title,
                    ItemSubTypeFilter.Materials,
                };
            }
        }
        public static string TypeToString(this ItemSubTypeFilter type, bool useSell = false)
        {
            switch (type)
            {
                case ItemSubTypeFilter.All:
                    return L10nManager.Localize("ALL");
                case ItemSubTypeFilter.Equipment:
                    return L10nManager.Localize("UI_EQUIPMENTS");
                case ItemSubTypeFilter.Costume:
                    return L10nManager.Localize("UI_COSTUME");
                case ItemSubTypeFilter.Food_HP:
                    return useSell
                        ? $"{StatType.HP.ToString()} {ItemSubType.Food.GetLocalizedString()}"
                        : StatType.HP.ToString();
                case ItemSubTypeFilter.Food_ATK:
                    return useSell
                        ? $"{StatType.ATK.ToString()} {ItemSubType.Food.GetLocalizedString()}"
                        : StatType.ATK.ToString();
                case ItemSubTypeFilter.Food_DEF:
                    return useSell
                        ? $"{StatType.DEF.ToString()} {ItemSubType.Food.GetLocalizedString()}"
                        : StatType.DEF.ToString();
                case ItemSubTypeFilter.Food_CRI:
                    return useSell
                        ? $"{StatType.CRI.ToString()} {ItemSubType.Food.GetLocalizedString()}"
                        : StatType.CRI.ToString();
                case ItemSubTypeFilter.Food_HIT:
                    return useSell
                        ? $"{StatType.HIT.ToString()} {ItemSubType.Food.GetLocalizedString()}"
                        : StatType.HIT.ToString();
                case ItemSubTypeFilter.Materials:
                    return L10nManager.Localize("UI_MATERIALS");

                default:
                    return ((ItemSubType) Enum.Parse(typeof(ItemSubType), type.ToString()))
                        .GetLocalizedString();
            }
        }

        public static ItemSubTypeFilter StatTypeToItemSubTypeFilter(StatType statType)
        {
            switch (statType)
            {
                case StatType.HP:
                    return ItemSubTypeFilter.Food_HP;
                case StatType.ATK:
                    return ItemSubTypeFilter.Food_ATK;
                case StatType.DEF:
                    return ItemSubTypeFilter.Food_DEF;
                case StatType.CRI:
                    return ItemSubTypeFilter.Food_CRI;
                case StatType.HIT:
                    return ItemSubTypeFilter.Food_HIT;
                case StatType.SPD:
                case StatType.NONE:
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }
        }

        public static ItemSubTypeFilter GetItemSubTypeFilter(ItemSheet sheet, int itemId)
        {
            var row = Game.Game.instance.TableSheets.ItemSheet[itemId];
            var itemSubType = row.ItemSubType;

            if (itemSubType == ItemSubType.Food)
            {
                var consumableRow = (ConsumableItemSheet.Row) row;
                var state = consumableRow.Stats.First();
                switch (state.StatType)
                {
                    case StatType.HP:
                        return ItemSubTypeFilter.Food_HP;
                    case StatType.ATK:
                        return ItemSubTypeFilter.Food_ATK;
                    case StatType.DEF:
                        return ItemSubTypeFilter.Food_DEF;
                    case StatType.CRI:
                        return ItemSubTypeFilter.Food_CRI;
                    case StatType.HIT:
                        return ItemSubTypeFilter.Food_HIT;
                    case StatType.SPD:
                    case StatType.NONE:
                    default:
                        throw new ArgumentOutOfRangeException(nameof(state.StatType), state.StatType, null);
                }
            }

            switch (row.ItemSubType)
            {
                case ItemSubType.Weapon:
                    return ItemSubTypeFilter.Weapon;
                    break;
                case ItemSubType.Armor:
                    return ItemSubTypeFilter.Armor;
                    break;
                case ItemSubType.Belt:
                    return ItemSubTypeFilter.Belt;
                    break;
                case ItemSubType.Necklace:
                    return ItemSubTypeFilter.Necklace;
                    break;
                case ItemSubType.Ring:
                    return ItemSubTypeFilter.Ring;
                    break;
                case ItemSubType.FullCostume:
                    return ItemSubTypeFilter.FullCostume;
                    break;
                case ItemSubType.HairCostume:
                    return ItemSubTypeFilter.HairCostume;
                    break;
                case ItemSubType.EarCostume:
                    return ItemSubTypeFilter.EarCostume;
                    break;
                case ItemSubType.EyeCostume:
                    return ItemSubTypeFilter.EyeCostume;
                    break;
                case ItemSubType.TailCostume:
                    return ItemSubTypeFilter.TailCostume;
                    break;
                case ItemSubType.Title:
                    return ItemSubTypeFilter.Title;
                    break;
                case ItemSubType.Hourglass:
                case ItemSubType.ApStone:
                    return ItemSubTypeFilter.Materials;
                    break;
                case ItemSubType.Food:
                case ItemSubType.EquipmentMaterial:
                case ItemSubType.FoodMaterial:
                case ItemSubType.MonsterPart:
                case ItemSubType.NormalMaterial:
                case ItemSubType.Chest:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
