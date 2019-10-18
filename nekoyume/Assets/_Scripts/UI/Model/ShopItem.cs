using System;
using Libplanet;
using Nekoyume.Game.Item;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopItem : InventoryItem
    {
        public readonly ReactiveProperty<Address> SellerAgentAddress = new ReactiveProperty<Address>();
        public readonly ReactiveProperty<Address> SellerAvatarAddress = new ReactiveProperty<Address>();
        public readonly ReactiveProperty<decimal> Price = new ReactiveProperty<decimal>();
        public readonly ReactiveProperty<Guid> ProductId = new ReactiveProperty<Guid>();
        
        public new readonly Subject<ShopItemView> OnClick = new Subject<ShopItemView>();
        
        public ShopItem(Address sellerAgentAddress, Game.Item.ShopItem item)
            : this(sellerAgentAddress, item.sellerAvatarAddress, item.price, item.productId, item.itemUsable)
        {
        }

        private ShopItem(Address sellerAgentAddress, Address sellerAvatarAddress, decimal price, Guid productId,
            ItemBase item) : base(item, 1)
        {
            SellerAgentAddress.Value = sellerAgentAddress;
            SellerAvatarAddress.Value = sellerAvatarAddress;
            Price.Value = price;
            ProductId.Value = productId;
        }

        public override void Dispose()
        {
            base.Dispose();
            
            SellerAgentAddress.Dispose();
            SellerAvatarAddress.Dispose();
            Price.Dispose();
            ProductId.Dispose();
            
            OnClick.Dispose();
        }
    }
}
