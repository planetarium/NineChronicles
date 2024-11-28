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

        Scroll,
        Circle,
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
                    ItemSubTypeFilter.Circle,
                    ItemSubTypeFilter.Scroll,
                    ItemSubTypeFilter.RuneStone,
                    ItemSubTypeFilter.PetSoulStone
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
                    return ((ItemSubType)Enum.Parse(typeof(ItemSubType), type.ToString()))
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

                case ItemSubTypeFilter.Scroll:
                    return ItemSubType.Scroll;

                case ItemSubTypeFilter.Circle:
                    return ItemSubType.Circle;

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

        public static bool IsEquipment(this ItemSubType type)
        {
            return type == ItemSubType.Weapon ||
                   type == ItemSubType.Armor ||
                   type == ItemSubType.Belt ||
                   type == ItemSubType.Necklace ||
                   type == ItemSubType.Ring;
        }
    }
}
