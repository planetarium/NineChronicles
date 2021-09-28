using System;
using System.Linq;
using Nekoyume.Helper;
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
        public readonly ReactiveProperty<bool> HasOptions = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<bool> Dimmed = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<bool> Selected = new ReactiveProperty<bool>(false);
        public readonly ReactiveProperty<bool> ActiveSelf = new ReactiveProperty<bool>(true);

        public readonly Subject<Item> OnClick = new Subject<Item>();
        public readonly Subject<Item> OnDoubleClick = new Subject<Item>();

        public Item(ItemBase value)
        {
            ItemBase.Value = value;

            var equipment = ItemBase.Value as Equipment;

            if (equipment != null &&
                equipment.level > 0)
            {
                Enhancement.Value = $"+{equipment.level}";
                EnhancementEnabled.Value = true;
                EnhancementEffectEnabled.Value = equipment.level >= Util.VisibleEnhancementEffectLevel;
            }
            else
            {
                Enhancement.Value = string.Empty;
                EnhancementEnabled.Value = false;
                EnhancementEffectEnabled.Value = false;
            }

            if (equipment != null)
            {
                HasOptions.Value = equipment.optionCountFromCombination > 0 ||
                    equipment.StatsMap.GetAdditionalStats(true).Count() + equipment.Skills.Count > 0;
            }
        }

        public virtual void Dispose()
        {
            ItemBase.Dispose();
            GradeEnabled.Dispose();
            Enhancement.Dispose();
            EnhancementEnabled.Dispose();
            EnhancementEffectEnabled.Dispose();
            HasOptions.Dispose();
            Dimmed.Dispose();
            ActiveSelf.Dispose();
            Selected.Dispose();

            OnClick.Dispose();
            OnDoubleClick.Dispose();
        }
    }
}
