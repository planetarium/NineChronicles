using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Market
{
    public static class ProductFactory
    {
        public static Product Deserialize(List serialized)
        {
            if (serialized[1].ToEnum<ProductType>() == ProductType.FungibleAssetValue)
            {
                return new FavProduct(serialized);
            }

            return new ItemProduct(serialized);
        }
    }
}
