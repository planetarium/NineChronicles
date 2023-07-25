using System;
using Bencodex.Types;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Action;
using Nekoyume.Model.State;

namespace Nekoyume.Model.Market
{
    public class ProductReceipt
    {
        public static Address DeriveAddress(Guid productId)
        {
            return Product.DeriveAddress(productId).Derive(nameof(ProductReceipt));
        }

        public readonly Guid ProductId;
        public readonly Address SellerAvatarAddress;
        public readonly Address BuyerAvatarAddress;
        public FungibleAssetValue Price;
        public long PurchasedBlockIndex;

        public ProductReceipt(Guid productId, Address sellerAvatarAddress,
            Address buyerAvatarAddress, FungibleAssetValue price, long purchasedBlockIndex)
        {
            ProductId = productId;
            SellerAvatarAddress = sellerAvatarAddress;
            BuyerAvatarAddress = buyerAvatarAddress;
            Price = price;
            PurchasedBlockIndex = purchasedBlockIndex;
        }

        public ProductReceipt(List serialized)
        {
            ProductId = serialized[0].ToGuid();
            SellerAvatarAddress = serialized[1].ToAddress();
            BuyerAvatarAddress = serialized[2].ToAddress();
            Price = serialized[3].ToFungibleAssetValue();
            PurchasedBlockIndex = serialized[4].ToLong();
        }

        public IValue Serialize()
        {
            return List.Empty
                .Add(ProductId.Serialize())
                .Add(SellerAvatarAddress.Serialize())
                .Add(BuyerAvatarAddress.Serialize())
                .Add(Price.Serialize())
                .Add(PurchasedBlockIndex.Serialize());
        }
    }
}
