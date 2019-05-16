using System;
using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopItem : CountableItem
    {
        public readonly ReactiveProperty<string> owner = new ReactiveProperty<string>();
        public readonly ReactiveProperty<decimal> price = new ReactiveProperty<decimal>();
        public readonly ReactiveProperty<Guid> productId = new ReactiveProperty<Guid>();
        public readonly ReactiveProperty<bool> selected = new ReactiveProperty<bool>();
        
        public readonly Subject<ShopItem> onClick = new Subject<ShopItem>();
        
        public ShopItem(string owner, Game.Item.ShopItem item) : this(owner, item.item, item.count, item.price, item.productId)
        {
        }
        
        public ShopItem(string owner, ItemBase item, int count, decimal price, Guid productId) : base(item, count)
        {
            this.owner.Value = owner;
            this.price.Value = price;
            this.productId.Value = productId;

            onClick.Subscribe(_ =>
            {
                selected.Value = !selected.Value;
            });
        }

        public override void Dispose()
        {
            base.Dispose();
            
            productId.Dispose();
            owner.Dispose();
            price.Dispose();
            selected.Dispose();
            
            onClick.Dispose();
        }
    }
}
