using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Helper;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    public class AssetInfo: IRegisterInfo
    {
        public Address AvatarAddress { get; set; }
        public FungibleAssetValue Price { get; set; }
        public FungibleAssetValue Asset { get; set; }
        public ProductType Type { get; set; }

        public AssetInfo()
        {
        }

        public AssetInfo(List serialized)
        {
            AvatarAddress = serialized[0].ToAddress();
            Price = serialized[1].ToFungibleAssetValue();
            Type = serialized[2].ToEnum<ProductType>();
            Asset = serialized[3].ToFungibleAssetValue();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(AvatarAddress.Serialize())
                .Add(Price.Serialize())
                .Add(Type.Serialize())
                .Add(Asset.Serialize());
        }

        public void ValidatePrice(Currency ncg)
        {
            if (!Price.Currency.Equals(ncg) || !Price.MinorUnit.IsZero || Price < 1 * ncg)
            {
                throw new InvalidPriceException(
                    $"product price must be greater than 0");
            }
        }

        public void ValidateAddress(Address avatarAddress)
        {
            if (AvatarAddress != avatarAddress)
            {
                throw new InvalidAddressException();
            }
        }

        public void Validate()
        {
            if (Type != ProductType.FungibleAssetValue)
            {
                throw new InvalidProductTypeException($"register asset does not support {Type}");
            }

            if (Asset.Currency.Equals(CrystalCalculator.CRYSTAL))
            {
                throw new InvalidCurrencyException($"{CrystalCalculator.CRYSTAL} does not allow register.");
            }

            if (Asset < Asset.Currency * 1)
            {
                throw new InvalidPriceException($"{Asset.Currency} must be greater than 0");
            }
        }
    }
}
