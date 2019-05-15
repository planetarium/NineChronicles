using System;
using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    [Serializable]
    public class CountableItem : Item
    {
        public readonly IntReactiveProperty count = new IntReactiveProperty(0);
        
        public CountableItem(Game.Item.Inventory.InventoryItem item, int count) : base(item)
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
