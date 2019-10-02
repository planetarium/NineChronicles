using System;
using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class Item : IDisposable
    {
        public readonly ReactiveProperty<ItemBase> ItemBase = new ReactiveProperty<ItemBase>();

        public Item(ItemBase value)
        {
            ItemBase.Value = value;
        }

        public virtual void Dispose()
        {
            ItemBase.Dispose();
        }
    }
}
