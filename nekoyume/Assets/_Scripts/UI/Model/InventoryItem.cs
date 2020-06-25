using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class InventoryItem : CountableItem
    {
        public readonly ReactiveProperty<bool> EffectEnabled = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<bool> GlowEnabled = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<bool> EquippedEnabled = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<bool> HasNotification = new ReactiveProperty<bool>(false);

        public InventoryItemView View;
        
        public InventoryItem(ItemBase item, int count) : base(item, count)
        {
        }

        public override void Dispose()
        {
            EffectEnabled.Dispose();
            GlowEnabled.Dispose();
            EquippedEnabled.Dispose();
            base.Dispose();
        }
    }
}
