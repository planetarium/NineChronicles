using Bencodex.Types;
using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Market
{
    public static class ProductFactory
    {
        public static Product DeserializeProduct(List serialized)
        {
            if (serialized[1].ToEnum<ProductType>() == ProductType.FungibleAssetValue)
            {
                return new FavProduct(serialized);
            }

            return new ItemProduct(serialized);
        }

        public static IProductInfo DeserializeProductInfo(List serialized)
        {
            if (serialized[4].ToEnum<ProductType>() == ProductType.FungibleAssetValue)
            {
                return new FavProductInfo(serialized);
            }

            return new ItemProductInfo(serialized);
        }

        public static IRegisterInfo DeserializeRegisterInfo(List serialized)
        {
            if (serialized[2].ToEnum<ProductType>() == ProductType.FungibleAssetValue)
            {
                return new AssetInfo(serialized);
            }

            return new RegisterInfo(serialized);
        }
    }
}
