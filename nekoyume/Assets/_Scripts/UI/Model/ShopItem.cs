using UniRx;

namespace Nekoyume.UI.Model
{
    public class ShopItem : CountableItem
    {
        public readonly ReactiveProperty<int> price = new ReactiveProperty<int>();
        
        public ShopItem(Game.Item.Inventory.InventoryItem item) : base(item, item.Count)
        {
        }
        
        public ShopItem(Game.Item.Inventory.InventoryItem item, int count) : base(item, count)
        {
        }

        public override void Dispose()
        {
            base.Dispose();
            
            price.Dispose();
        }
    }
}
