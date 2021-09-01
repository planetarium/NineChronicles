using System;
using Libplanet;
using Libplanet.Assets;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopBuyItem : CountableItem
    {
        public readonly ReactiveProperty<Address> SellerAgentAddress = new ReactiveProperty<Address>();
        public readonly ReactiveProperty<Address> SellerAvatarAddress = new ReactiveProperty<Address>();
        public readonly ReactiveProperty<FungibleAssetValue> Price = new ReactiveProperty<FungibleAssetValue>();
        public readonly ReactiveProperty<Guid> ProductId = new ReactiveProperty<Guid>();

        public ShopItemView View;

        public ShopBuyItem(Nekoyume.Model.Item.ShopItem item)
            : this(item.SellerAgentAddress, item.SellerAvatarAddress, item.Price, item.ProductId,
                item.ItemUsable ?? (ItemBase)item.Costume)
        {
        }

        private ShopBuyItem(Address sellerAgentAddress, Address sellerAvatarAddress, FungibleAssetValue price, Guid productId,
            ItemBase item) : base(item, 1)
        {
            GradeEnabled.Value = true;
            SellerAgentAddress.Value = sellerAgentAddress;
            SellerAvatarAddress.Value = sellerAvatarAddress;
            Price.Value = price;
            ProductId.Value = productId;
        }

        public override void Dispose()
        {
            SellerAgentAddress.Dispose();
            SellerAvatarAddress.Dispose();
            Price.Dispose();
            ProductId.Dispose();
            base.Dispose();
        }
    }
}
