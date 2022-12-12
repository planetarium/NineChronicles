using System.Collections.Generic;

namespace Nekoyume.EnumType
{
    public enum ShopSortFilter
    {
        CP = 0,
        Price = 1,
        Class = 2,
        CrystalPerNcg = 3,
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
                    ShopSortFilter.CrystalPerNcg,
                };
            }
        }
    }
}
