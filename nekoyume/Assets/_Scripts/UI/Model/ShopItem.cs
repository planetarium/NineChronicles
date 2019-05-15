using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopItem : CountableItem // Nekoyume.Game.Item.ShopItem
    {
        // public readonly ByteArrayReactiveProperty owner = new ByteArrayReactiveProperty();
        public readonly DecimalReactiveProperty price = new DecimalReactiveProperty();

//        public ShopItem(Nekoyume.Game.Item.ShopItem item)
//        {
//            
//        }

        public ShopItem(Game.Item.Inventory.InventoryItem item) : base(item, item.Count)
        {
            price.Value = 0M;
        }
        
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
            
            price.Dispose();
        }
    }
}
