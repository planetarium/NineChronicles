using System;
using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class CountableItem : Item
    {
        public readonly ReactiveProperty<int> count = new ReactiveProperty<int>(0);
        public readonly ReactiveProperty<bool> countEnabled = new ReactiveProperty<bool>(true);
        public readonly ReactiveProperty<Func<CountableItem, bool>> countEnabledFunc = new ReactiveProperty<Func<CountableItem, bool>>();
        
        public CountableItem(ItemBase item, int count) : base(item)
        {
            this.count.Value = count;
            countEnabledFunc.Value = CountEnabledFunc;

            countEnabledFunc.Subscribe(func =>
            {
                if (countEnabledFunc.Value == null)
                {
                    countEnabledFunc.Value = CountEnabledFunc;
                }

                countEnabled.Value = countEnabledFunc.Value(this);
            });
        }
        
        public override void Dispose()
        {
            base.Dispose();
            
            count.Dispose();
            countEnabledFunc.Dispose();
        }

        private bool CountEnabledFunc(CountableItem countableItem)
        {
            if (countableItem.item.Value == null)
            {
                return false;
            }
            
            return countableItem.item.Value.Data.cls.ToEnumItemType() == ItemBase.ItemType.Material;
        }
    }
}
