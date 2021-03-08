using System;
using System.Numerics;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopItem : CountableItem
    {
        public readonly ReactiveProperty<Address> SellerAgentAddress = new ReactiveProperty<Address>();
        public readonly ReactiveProperty<Address> SellerAvatarAddress = new ReactiveProperty<Address>();
        public readonly ReactiveProperty<FungibleAssetValue> Price = new ReactiveProperty<FungibleAssetValue>();
        public readonly ReactiveProperty<Guid> ProductId = new ReactiveProperty<Guid>();
        public readonly ReactiveProperty<long> ExpiredBlockIndex = new ReactiveProperty<long>();

        public ShopItemView View;

        public ShopItem(Nekoyume.Model.Item.ShopItem item)
            : this(item.SellerAgentAddress, item.SellerAvatarAddress, item.Price, item.ProductId,
                item.ItemUsable ?? (ItemBase)item.Costume, item.ExpiredBlockIndex)
        {
        }

        private ShopItem(Address sellerAgentAddress, Address sellerAvatarAddress, FungibleAssetValue price, Guid productId,
            ItemBase item, long expiredBlockIndex) : base(item, 1)
        {
            GradeEnabled.Value = true;
            SellerAgentAddress.Value = sellerAgentAddress;
            SellerAvatarAddress.Value = sellerAvatarAddress;
            Price.Value = price;
            ProductId.Value = productId;
            ExpiredBlockIndex.Value = expiredBlockIndex;
        }

        public override void Dispose()
        {
            SellerAgentAddress.Dispose();
            SellerAvatarAddress.Dispose();
            Price.Dispose();
            ProductId.Dispose();
            ExpiredBlockIndex.Dispose();
            base.Dispose();
        }
    }
}
