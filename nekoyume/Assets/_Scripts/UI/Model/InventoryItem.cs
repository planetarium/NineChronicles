using System;
using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class InventoryItem : CountableItem
    {
        public readonly ReactiveProperty<bool> covered = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<bool> dimmed = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<bool> selected = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<bool> glowed = new ReactiveProperty<bool>(false);

        public readonly Subject<InventoryItem> onClick = new Subject<InventoryItem>();
        public readonly Subject<InventoryItem> onDoubleClick = new Subject<InventoryItem>();

        public InventoryItem(ItemBase item, int count) : base(item, count)
        {
        }

        public override void Dispose()
        {
            base.Dispose();
            
            covered.Dispose();
            dimmed.Dispose();
            selected.Dispose();
            glowed.Dispose();

            onClick.Dispose();
            onDoubleClick.Dispose();
        }
    }
}
