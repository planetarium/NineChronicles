using System;
using Libplanet;
using Nekoyume.Game.Item;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class ShopItem : InventoryItem
    {
        public readonly ReactiveProperty<Address> sellerAvatarAddress = new ReactiveProperty<Address>();
        public readonly ReactiveProperty<Address> sellerAgentAddress = new ReactiveProperty<Address>();
        public readonly ReactiveProperty<decimal> price = new ReactiveProperty<decimal>();
        public readonly ReactiveProperty<Guid> productId = new ReactiveProperty<Guid>();
        
        public new readonly Subject<ShopItemView> onClick = new Subject<ShopItemView>();
        
        public ShopItem(Address sellerAvatarAddress, Game.Item.ShopItem item) : this(sellerAvatarAddress, item.sellerAgentAddress, item.price, item.productId, item.itemUsable)
        {
        }
        
        public ShopItem(Address sellerAvatarAddress, Address sellerAgentAddress, decimal price, Guid productId, ItemBase item) : base(item, 1)
        {
            this.sellerAvatarAddress.Value = sellerAvatarAddress;
            this.sellerAgentAddress.Value = sellerAgentAddress;
            this.price.Value = price;
            this.productId.Value = productId;
        }

        public override void Dispose()
        {
            base.Dispose();
            
            productId.Dispose();
            sellerAvatarAddress.Dispose();
            price.Dispose();
            
            onClick.Dispose();
        }
    }
}
