using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class Item : IDisposable
    {
        public readonly ReactiveProperty<Game.Item.Inventory.InventoryItem> item = new ReactiveProperty<Game.Item.Inventory.InventoryItem>();

        public Item(Game.Item.Inventory.InventoryItem value)
        {
            item.Value = value;
        }

        public virtual void Dispose()
        {
            item.Dispose();
        }
    }
}
