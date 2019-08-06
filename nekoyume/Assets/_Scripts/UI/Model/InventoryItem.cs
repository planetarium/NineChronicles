using Nekoyume.Game.Item;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class InventoryItem : CountableItem
    {
        public readonly ReactiveProperty<bool> covered = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<bool> dimmed = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<bool> equipped = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<bool> selected = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<bool> glowed = new ReactiveProperty<bool>(false);

        public readonly Subject<InventoryItemView> onClick = new Subject<InventoryItemView>();
        public readonly Subject<InventoryItemView> onDoubleClick = new Subject<InventoryItemView>();
        public readonly Subject<InventoryItemView> onRightClick = new Subject<InventoryItemView>();

        public InventoryItem(ItemBase item, int count) : base(item, count)
        {
        }

        public override void Dispose()
        {
            base.Dispose();           
            covered.Dispose();
            dimmed.Dispose();
            equipped.Dispose();
            selected.Dispose();
            glowed.Dispose();

            onClick.Dispose();
            onDoubleClick.Dispose();
            onRightClick.Dispose();
        }
    }
}
