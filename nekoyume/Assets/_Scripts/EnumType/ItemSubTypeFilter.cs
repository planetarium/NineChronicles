using System;
using System.Collections.Generic;
using System.Linq;
using Libplanet.Types.Assets;
using Nekoyume.Game;
using Nekoyume.Helper;
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
        Food_SPD,

        FullCostume,
        HairCostume,
        EarCostume,
        EyeCostume,
        TailCostume,
        Title,

        Hourglass,
        ApStone,

        RuneStone,
        PetSoulStone,

        Materials,
        Equipment,
        Food,
        Costume,
        Stones,
    }

    public static class ItemSubTypeFilterExtension
    {
        public static IEnumerable<ItemSubTypeFilter> Filters
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
                    ItemSubTypeFilter.Food_CRI,
                    ItemSubTypeFilter.Food_DEF,
                    ItemSubTypeFilter.Food_SPD,
                    ItemSubTypeFilter.Food_HIT,
                    ItemSubTypeFilter.FullCostume,
                    ItemSubTypeFilter.Title,
                    ItemSubTypeFilter.Hourglass,
                    ItemSubTypeFilter.ApStone,
                    ItemSubTypeFilter.RuneStone,
                    ItemSubTypeFilter.PetSoulStone,
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
                case ItemSubTypeFilter.Food_SPD:
                    return useSell
                        ? $"{StatType.SPD.ToString()} {ItemSubType.Food.GetLocalizedString()}"
                        : StatType.SPD.ToString();
                case ItemSubTypeFilter.Materials:
                    return L10nManager.Localize("UI_MATERIALS");

                case ItemSubTypeFilter.Stones:
                    return "Stones";

                case ItemSubTypeFilter.RuneStone:
                    return "RuneStone";

                case ItemSubTypeFilter.PetSoulStone:
                    return "PetSoulStone";

                default:
                    return ((ItemSubType) Enum.Parse(typeof(ItemSubType), type.ToString()))
                        .GetLocalizedString();
            }
        }

        public static ItemSubType ToItemSubType(this ItemSubTypeFilter type)
        {
            switch (type)
            {
                case ItemSubTypeFilter.All:
                case ItemSubTypeFilter.Equipment:
                case ItemSubTypeFilter.Weapon:
                    return ItemSubType.Weapon;

                case ItemSubTypeFilter.Armor:
                    return ItemSubType.Armor;

                case ItemSubTypeFilter.Belt:
                    return ItemSubType.Belt;

                case ItemSubTypeFilter.Necklace:
                    return ItemSubType.Necklace;

                case ItemSubTypeFilter.Ring:
                    return ItemSubType.Ring;

                case ItemSubTypeFilter.Food:
                case ItemSubTypeFilter.Food_HP:
                case ItemSubTypeFilter.Food_ATK:
                case ItemSubTypeFilter.Food_DEF:
                case ItemSubTypeFilter.Food_CRI:
                case ItemSubTypeFilter.Food_HIT:
                case ItemSubTypeFilter.Food_SPD:
                    return ItemSubType.Food;

                case ItemSubTypeFilter.Materials:
                case ItemSubTypeFilter.Hourglass:
                    return ItemSubType.Hourglass;

                case ItemSubTypeFilter.ApStone:
                    return ItemSubType.ApStone;

                case ItemSubTypeFilter.Costume:
                case ItemSubTypeFilter.FullCostume:
                    return ItemSubType.FullCostume;

                case ItemSubTypeFilter.Title:
                    return ItemSubType.Title;
            }

            return ItemSubType.Weapon;
        }

        public static StatType ToItemStatType(this ItemSubTypeFilter type)
        {
            switch (type)
            {
                case ItemSubTypeFilter.Food_HP:
                    return StatType.HP;
                case ItemSubTypeFilter.Food_ATK:
                    return StatType.ATK;
                case ItemSubTypeFilter.Food_DEF:
                    return StatType.DEF;
                case ItemSubTypeFilter.Food_CRI:
                    return StatType.CRI;
                case ItemSubTypeFilter.Food_HIT:
                    return StatType.HIT;
                case ItemSubTypeFilter.Food_SPD:
                    return StatType.SPD;
                default:
                    return StatType.NONE;
            }
        }

        public static ItemSubTypeFilter ToItemTypeFilter(this ItemSubTypeFilter type)
        {
            switch (type)
            {
                case ItemSubTypeFilter.All:
                case ItemSubTypeFilter.Weapon:
                case ItemSubTypeFilter.Armor:
                case ItemSubTypeFilter.Belt:
                case ItemSubTypeFilter.Necklace:
                case ItemSubTypeFilter.Ring:
                    return ItemSubTypeFilter.Equipment;

                case ItemSubTypeFilter.Food_HP:
                case ItemSubTypeFilter.Food_ATK:
                case ItemSubTypeFilter.Food_DEF:
                case ItemSubTypeFilter.Food_CRI:
                case ItemSubTypeFilter.Food_HIT:
                case ItemSubTypeFilter.Food_SPD:
                    return ItemSubTypeFilter.Food;

                case ItemSubTypeFilter.FullCostume:
                case ItemSubTypeFilter.Title:
                    return ItemSubTypeFilter.Costume;

                case ItemSubTypeFilter.Hourglass:
                case ItemSubTypeFilter.ApStone:
                    return ItemSubTypeFilter.Materials;

                case ItemSubTypeFilter.RuneStone:
                case ItemSubTypeFilter.PetSoulStone:
                    return ItemSubTypeFilter.Stones;

                default:
                    return type;
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
                    return ItemSubTypeFilter.Food_SPD;
                case StatType.NONE:
                default:
                    throw new ArgumentOutOfRangeException(nameof(statType), statType, null);
            }
        }

        public static ItemSubTypeFilter GetItemSubTypeFilter(this FungibleAssetValue fav)
        {
            if (RuneFrontHelper.TryGetRuneData(fav.Currency.Ticker, out _))
            {
                return ItemSubTypeFilter.RuneStone;
            }

            var petSheet = Game.Game.instance.TableSheets.PetSheet;
            if (petSheet.Values.Any(x => x.SoulStoneTicker == fav.Currency.Ticker))
            {
                return ItemSubTypeFilter.PetSoulStone;
            }

            return ItemSubTypeFilter.Stones;
        }

        public static List<ItemSubTypeFilter> GetItemSubTypeFilter(int itemId)
        {
            var result = new List<ItemSubTypeFilter>();
            var row = TableSheets.Instance.ItemSheet[itemId];
            if (row.ItemType == ItemType.Consumable)
            {
                var consumableRow = (ConsumableItemSheet.Row) row;
                foreach (var statMap in consumableRow.Stats)
                {
                    switch (statMap.StatType)
                    {
                        case StatType.HP:
                            result.Add(ItemSubTypeFilter.Food_HP);
                            break;
                        case StatType.ATK:
                            result.Add(ItemSubTypeFilter.Food_ATK);
                            break;
                        case StatType.DEF:
                            result.Add(ItemSubTypeFilter.Food_DEF);
                            break;
                        case StatType.CRI:
                            result.Add(ItemSubTypeFilter.Food_CRI);
                            break;
                        case StatType.HIT:
                            result.Add(ItemSubTypeFilter.Food_HIT);
                            break;
                        case StatType.SPD:
                            result.Add(ItemSubTypeFilter.Food_SPD);
                            break;
                        case StatType.NONE:
                        default:
                            throw new ArgumentOutOfRangeException(
                                nameof(statMap.StatType),
                                statMap.StatType,
                                null);
                    }
                }

                return result;
            }

            switch (row.ItemSubType)
            {
                case ItemSubType.Weapon:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.Weapon };
                case ItemSubType.Armor:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.Armor };
                case ItemSubType.Belt:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.Belt };
                case ItemSubType.Necklace:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.Necklace };
                case ItemSubType.Ring:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.Ring };
                case ItemSubType.FullCostume:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.FullCostume };
                case ItemSubType.HairCostume:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.HairCostume };
                case ItemSubType.EarCostume:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.EarCostume };
                case ItemSubType.EyeCostume:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.EyeCostume };
                case ItemSubType.TailCostume:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.TailCostume };
                case ItemSubType.Title:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.Title };
                case ItemSubType.Hourglass:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.Hourglass };
                case ItemSubType.ApStone:
                    return new List<ItemSubTypeFilter> { ItemSubTypeFilter.ApStone };
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
