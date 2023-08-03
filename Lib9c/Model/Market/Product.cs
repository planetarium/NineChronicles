using System;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Market
{
    public class Product
    {
        public static Address DeriveAddress(Guid productId)
        {
            return Addresses.Market.Derive(productId.ToString());
        }


        public Guid ProductId;
        public ProductType Type;
        public FungibleAssetValue Price;
        public long RegisteredBlockIndex;
        public Address SellerAvatarAddress;
        public Address SellerAgentAddress;

        protected Product()
        {
        }

        protected Product(List serialized)
        {
            ProductId = serialized[0].ToGuid();
            Type = serialized[1].ToEnum<ProductType>();
            Price = serialized[2].ToFungibleAssetValue();
            RegisteredBlockIndex = serialized[3].ToLong();
            SellerAgentAddress = serialized[4].ToAddress();
            SellerAvatarAddress = serialized[5].ToAddress();
        }

        public virtual IValue Serialize()
        {
            return List.Empty
                .Add(ProductId.Serialize())
                .Add(Type.Serialize())
                .Add(Price.Serialize())
                .Add(RegisteredBlockIndex.Serialize())
                .Add(SellerAgentAddress.Serialize())
                .Add(SellerAvatarAddress.Serialize());
        }

        public virtual void Validate(IProductInfo productInfo)
        {
            if (SellerAgentAddress != productInfo.AgentAddress ||
                SellerAvatarAddress != productInfo.AvatarAddress)
            {
                throw new InvalidAddressException();
            }

            if (Price != productInfo.Price)
            {
                throw new InvalidPriceException("");
            }
        }
    }
}
