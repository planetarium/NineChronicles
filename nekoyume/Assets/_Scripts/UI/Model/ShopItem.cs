using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopItem : CountableItem
    {
        public readonly ReactiveProperty<int> price = new ReactiveProperty<int>();

        public ShopItem(Game.Item.Inventory.InventoryItem item) : base(item, item.Count)
        {
            price.Value = 0;
        }
        
        public ShopItem(Game.Item.Inventory.InventoryItem item, int count) : base(item, count)
        {
            price.Value = 0;
        }
        
        public ShopItem(Game.Item.Inventory.InventoryItem item, int count, int price) : base(item, count)
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
