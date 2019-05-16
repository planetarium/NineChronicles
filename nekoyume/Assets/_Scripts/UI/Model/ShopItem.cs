using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopItem : CountableItem
    {
        public readonly ByteArrayReactiveProperty owner = new ByteArrayReactiveProperty();
        public readonly DecimalReactiveProperty price = new DecimalReactiveProperty();
        
        public ShopItem(Game.Item.Inventory.InventoryItem item, int count) : base(item, count)
        {
            price.Value = 0M;
        }
        
        public ShopItem(Game.Item.Inventory.InventoryItem item, int count, decimal price) : base(item, count)
        {
            this.price.Value = price;
        }

        public override void Dispose()
        {
            base.Dispose();
            
            owner.Dispose();
            price.Dispose();
        }
    }
}
