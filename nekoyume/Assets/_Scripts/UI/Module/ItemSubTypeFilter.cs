using System;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;

namespace Nekoyume.UI.Module
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
    }
}
