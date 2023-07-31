using System;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Model.Market;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    public class RegisterInfo: IRegisterInfo
    {
        public Address AvatarAddress { get; set; }
        public FungibleAssetValue Price { get; set; }
        public Guid TradableId { get; set; }
        public int ItemCount { get; set; }
        public ProductType Type { get; set; }

        public RegisterInfo(List serialized)
        {
            AvatarAddress = serialized[0].ToAddress();
            Price = serialized[1].ToFungibleAssetValue();
            Type = serialized[2].ToEnum<ProductType>();
            TradableId = serialized[3].ToGuid();
            ItemCount = serialized[4].ToInteger();
        }

        public RegisterInfo()
        {
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(AvatarAddress.Serialize())
                .Add(Price.Serialize())
                .Add(Type.Serialize())
                .Add(TradableId.Serialize())
                .Add(ItemCount.Serialize());
        }

        public void ValidatePrice(Currency ncg)
        {
            if (!Price.Currency.Equals(ncg) || !Price.MinorUnit.IsZero || Price < 1 * ncg)
            {
                throw new InvalidPriceException(
                    $"product price must be greater than zero.");
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
            if (Type == ProductType.FungibleAssetValue)
            {
                throw new InvalidProductTypeException($"register item does not support {ProductType.FungibleAssetValue}");
            }

            if (ItemCount < 1)
            {
                throw new InvalidItemCountException();
            }

            if (Type == ProductType.NonFungible && ItemCount != 1)
            {
                throw new InvalidItemCountException();
            }
        }
    }
}
