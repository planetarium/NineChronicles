using System;
using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopItem : InventoryItem
    {
        public readonly ReactiveProperty<string> owner = new ReactiveProperty<string>();
        public readonly ReactiveProperty<decimal> price = new ReactiveProperty<decimal>();
        public readonly ReactiveProperty<Guid> productId = new ReactiveProperty<Guid>();
        
        public new readonly Subject<ShopItem> onClick = new Subject<ShopItem>();
        
        public ShopItem(string owner, Game.Item.ShopItem item) : this(owner, item.price, item.productId, item.item, item.count)
        {
        }
        
        public ShopItem(string owner, decimal price, Guid productId, ItemBase item, int count) : base(item, count)
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
