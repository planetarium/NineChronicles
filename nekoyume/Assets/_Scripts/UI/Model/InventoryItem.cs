using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class InventoryItem : CountableItem
    {
        public readonly ReactiveProperty<bool> covered = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<bool> dimmed = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<bool> selected = new ReactiveProperty<bool>(false);

        public readonly Subject<InventoryItem> onClick = new Subject<InventoryItem>();

        public InventoryItem(Game.Item.Inventory.InventoryItem item) : base(item, item.Count)
        {
        }
        
        public InventoryItem(Game.Item.Inventory.InventoryItem item, int count) : base(item, count)
        {
        }

        public override void Dispose()
        {
            base.Dispose();
            
            covered.Dispose();
            dimmed.Dispose();
            selected.Dispose();

            onClick.Dispose();
        }
    }
}
