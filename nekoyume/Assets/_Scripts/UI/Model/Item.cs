using System;
using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class Item : IDisposable
    {
        public readonly ReactiveProperty<ItemBase> item = new ReactiveProperty<ItemBase>();

        public Item(ItemBase value)
        {
            item.Value = value;
        }

        public virtual void Dispose()
        {
            item.Dispose();
        }
    }
}
