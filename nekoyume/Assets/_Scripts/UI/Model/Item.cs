using System;
using Nekoyume.Model.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class Item : IDisposable
    {
        public readonly ReactiveProperty<ItemBase> ItemBase = new ReactiveProperty<ItemBase>();
        public readonly ReactiveProperty<bool> GradeEnabled = new ReactiveProperty<bool>(true);
        public readonly ReactiveProperty<string> Enhancement = new ReactiveProperty<string>();
        public readonly ReactiveProperty<bool> EnhancementEnabled = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<bool> EnhancementEffectEnabled = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<int> Options = new ReactiveProperty<int>(0);
        public readonly ReactiveProperty<bool> Dimmed = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<bool> Selected = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<bool> ActiveSelf = new ReactiveProperty<bool>(true);

        public readonly Subject<Item> OnClick = new Subject<Item>();
        public readonly Subject<Item> OnDoubleClick = new Subject<Item>();

        private const int VisibleEnhancementEffectValue = 11; // todo : When a weapon effect is added, the value must be modified.
        public Item(ItemBase value)
        {
            ItemBase.Value = value;

            var equipment = ItemBase.Value as Equipment;

            if (equipment != null &&
                equipment.level > 0)
            {
                Enhancement.Value = $"+{equipment.level}";
                EnhancementEnabled.Value = true;
                EnhancementEffectEnabled.Value = equipment.level >= VisibleEnhancementEffectValue;
            }
            else
            {
                Enhancement.Value = string.Empty;
                EnhancementEnabled.Value = false;
                EnhancementEffectEnabled.Value = false;
            }

            if (equipment != null)
            {
                Options.Value = equipment.GetOptionCount();
            }
        }

        public virtual void Dispose()
        {
            ItemBase.Dispose();
            GradeEnabled.Dispose();
            Enhancement.Dispose();
            EnhancementEnabled.Dispose();
            EnhancementEffectEnabled.Dispose();
            Options.Dispose();
            Dimmed.Dispose();
            ActiveSelf.Dispose();
            Selected.Dispose();

            OnClick.Dispose();
            OnDoubleClick.Dispose();
        }
    }
}
