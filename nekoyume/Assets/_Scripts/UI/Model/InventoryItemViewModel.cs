using System;
using Nekoyume.Model.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class InventoryItemViewModel : IDisposable, IItemViewModel
    {
        public ItemBase ItemBase { get; }

        public readonly ReactiveProperty<bool> Dimmed;
        public readonly ReactiveProperty<bool> Selected;
        public readonly ReactiveProperty<bool> ActiveSelf;
        public readonly ReactiveProperty<int> Count;
        public readonly ReactiveProperty<bool> EffectEnabled;
        public readonly ReactiveProperty<bool> Focused;
        public readonly ReactiveProperty<bool> Equipped;
        public readonly ReactiveProperty<bool> HasNotification;
        public readonly ReactiveProperty<bool> IsTradable;

        public InventoryItemViewModel(ItemBase itemBase, int count, bool equipped)
        {
            ItemBase = itemBase;
            Count = new ReactiveProperty<int>(count);
            Equipped = new ReactiveProperty<bool>(equipped);
            Dimmed = new ReactiveProperty<bool>(false);
            Selected = new ReactiveProperty<bool>(false);
            ActiveSelf = new ReactiveProperty<bool>(true);
            EffectEnabled = new ReactiveProperty<bool>(false);
            Focused = new ReactiveProperty<bool>(false);
            HasNotification = new ReactiveProperty<bool>(false);
            IsTradable = new ReactiveProperty<bool>(false);
        }

        public void Dispose()
        {
            Dimmed.Dispose();
            Selected.Dispose();
            ActiveSelf.Dispose();
            Count.Dispose();
            EffectEnabled.Dispose();
            Focused.Dispose();
            Equipped.Dispose();
            HasNotification.Dispose();
            IsTradable.Dispose();
        }
    }
}
