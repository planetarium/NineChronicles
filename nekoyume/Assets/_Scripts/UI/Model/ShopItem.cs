using System;
using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopItem : CountableItem
    {
        public readonly ReactiveProperty<byte[]> owner = new ReactiveProperty<byte[]>();
        public readonly ReactiveProperty<decimal> price = new ReactiveProperty<decimal>();
        
        public string ProductId { get; }

        public ShopItem(Game.Item.ShopItem item) : this(item.item, item.count, item.owner, item.price, item.productId)
        {
        }
        
        public ShopItem(ItemBase item, int count, byte[] owner, decimal price, string productId) : base(item, count)
        {
            this.owner.Value = owner;
            this.price.Value = price;
            
            ProductId = productId;
        }

        public override void Dispose()
        {
            base.Dispose();
            
            owner.Dispose();
            price.Dispose();
        }
    }
}
