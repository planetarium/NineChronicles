using System;
using System.Collections.Generic;

namespace Nekoyume.EnumType
{
    public enum ShopSortFilter
    {
        CP = 0,
        Price = 1,
        Class = 2,
        Crystal = 3,
        CrystalPerPrice = 4,
        EquipmentLevel = 5,
        OptionCount = 6,
        UnitPrice = 7
    }

    public static class ShopSortFilterExtension
    {
        public static IEnumerable<ShopSortFilter> ShopSortFilters
        {
            get
            {
                return new[]
                {
                    ShopSortFilter.CP,
                    ShopSortFilter.Price,
                    ShopSortFilter.Class,
                    ShopSortFilter.Crystal,
                    ShopSortFilter.CrystalPerPrice,
                    ShopSortFilter.EquipmentLevel,
                    ShopSortFilter.OptionCount,
                    ShopSortFilter.UnitPrice
                };
            }
        }

        public static MarketOrderType ToMarketOrderType(this ShopSortFilter type, bool isAscending)
        {
            return type switch
            {
                ShopSortFilter.CP => isAscending ? MarketOrderType.cp : MarketOrderType.cp_desc,
                ShopSortFilter.Price => isAscending ? MarketOrderType.price : MarketOrderType.price_desc,
                ShopSortFilter.Class => isAscending ? MarketOrderType.grade : MarketOrderType.grade_desc,
                ShopSortFilter.Crystal => isAscending ? MarketOrderType.crystal : MarketOrderType.crystal_desc,
                ShopSortFilter.CrystalPerPrice => isAscending ? MarketOrderType.crystal_per_price : MarketOrderType.crystal_per_price_desc,
                ShopSortFilter.EquipmentLevel => isAscending ? MarketOrderType.level : MarketOrderType.level_desc,
                ShopSortFilter.OptionCount => isAscending ? MarketOrderType.opt_count : MarketOrderType.opt_count_desc,
                ShopSortFilter.UnitPrice => isAscending ? MarketOrderType.unit_price : MarketOrderType.unit_price_desc,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}
