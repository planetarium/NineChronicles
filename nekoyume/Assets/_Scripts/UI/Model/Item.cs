using System;
using Nekoyume.Game.Item;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class Item : IDisposable
    {
        public readonly ReactiveProperty<ItemBase> ItemBase = new ReactiveProperty<ItemBase>();
        public readonly ReactiveProperty<bool> GradeEnabled = new ReactiveProperty<bool>(true);
        public readonly ReactiveProperty<bool> Selected = new ReactiveProperty<bool>(false);

        public readonly Subject<Item> OnClick = new Subject<Item>();
        public readonly Subject<Item> OnRightClick = new Subject<Item>();
        
        public Item(ItemBase value)
        {
            ItemBase.Value = value;
        }

        public virtual void Dispose()
        {
            ItemBase.Dispose();
            GradeEnabled.Dispose();
            Selected.Dispose();
            
            OnClick.Dispose();
            OnRightClick.Dispose();
        }
    }
}
