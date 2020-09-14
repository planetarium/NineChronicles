using System;
using Nekoyume.Model.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    // todo: Item에 합쳐도 될 것 같음.
    public class CountableItem : Item
    {
        public readonly ReactiveProperty<int> Count = new ReactiveProperty<int>(0);
        public readonly ReactiveProperty<bool> CountEnabled = new ReactiveProperty<bool>(true);
        public readonly ReactiveProperty<Func<CountableItem, bool>> CountEnabledFunc = new ReactiveProperty<Func<CountableItem, bool>>();
        
        public CountableItem(ItemBase item, int count) : base(item)
        {
            Count.Value = count;
            CountEnabledFunc.Value = CountEnabledFuncDefault;

            CountEnabledFunc.Subscribe(func =>
            {
                if (CountEnabledFunc.Value == null)
                {
                    CountEnabledFunc.Value = CountEnabledFuncDefault;
                }

                CountEnabled.Value = CountEnabledFunc.Value(this);
            });
        }
        
        public override void Dispose()
        {
            Count.Dispose();
            CountEnabled.Dispose();
            CountEnabledFunc.Dispose();
            base.Dispose();
        }

        private bool CountEnabledFuncDefault(CountableItem countableItem)
        {
            if (countableItem.ItemBase.Value == null)
            {
                return false;
            }
            
            return countableItem.ItemBase.Value.ItemType == ItemType.Material;
        }
    }
}
