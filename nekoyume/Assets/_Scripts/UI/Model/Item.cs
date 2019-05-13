using System;

namespace Nekoyume.UI.Model
{
    [Serializable]
    public class Item : IDisposable
    {
        public readonly InventoryItemReactiveProperty item = new InventoryItemReactiveProperty();

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
