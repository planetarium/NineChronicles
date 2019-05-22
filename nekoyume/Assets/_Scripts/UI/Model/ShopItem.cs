using System;
using Libplanet;
using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopItem : InventoryItem
    {
        public readonly ReactiveProperty<Address> owner = new ReactiveProperty<Address>();
        public readonly ReactiveProperty<decimal> price = new ReactiveProperty<decimal>();
        public readonly ReactiveProperty<Guid> productId = new ReactiveProperty<Guid>();
        
        public new readonly Subject<ShopItem> onClick = new Subject<ShopItem>();
        
        public ShopItem(Address owner, Game.Item.ShopItem item) : this(owner, item.price, item.productId, item.item, item.count)
        {
        }
        
        public ShopItem(Address owner, decimal price, Guid productId, ItemBase item, int count) : base(item, count)
        {
            this.owner.Value = owner;
            this.price.Value = price;
            this.productId.Value = productId;
        }

        public override void Dispose()
        {
            base.Dispose();
            
            productId.Dispose();
            owner.Dispose();
            price.Dispose();
            
            onClick.Dispose();
        }
    }
}
