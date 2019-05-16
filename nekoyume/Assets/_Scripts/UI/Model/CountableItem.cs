using System;
using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class CountableItem : Item
    {
        public readonly ReactiveProperty<int> count = new ReactiveProperty<int>(0);
        
        public CountableItem(ItemBase item, int count) : base(item)
        {
            this.count.Value = count;
        }
        
        public override void Dispose()
        {
            base.Dispose();
            
            count.Dispose();
        }
    }
}
