using System;
using Libplanet;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopItem : CountableItem
    {
        public readonly ReactiveProperty<Address> SellerAgentAddress = new ReactiveProperty<Address>();
        public readonly ReactiveProperty<Address> SellerAvatarAddress = new ReactiveProperty<Address>();
        public readonly ReactiveProperty<decimal> Price = new ReactiveProperty<decimal>();
        public readonly ReactiveProperty<Guid> ProductId = new ReactiveProperty<Guid>();
        
        public ShopItemView View;
        
        public ShopItem(Address sellerAgentAddress, Nekoyume.Model.Item.ShopItem item)
            : this(sellerAgentAddress, item.SellerAvatarAddress, item.Price, item.ProductId, item.ItemUsable)
        {
        }

        private ShopItem(Address sellerAgentAddress, Address sellerAvatarAddress, decimal price, Guid productId,
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
