using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class CountEditableItem : CountableItem
    {
        public readonly ReactiveProperty<string> editButtonText = new ReactiveProperty<string>("");

        public readonly Subject<CountEditableItem> onClose = new Subject<CountEditableItem>();
        public readonly Subject<CountEditableItem> onEdit = new Subject<CountEditableItem>();
        
        public CountEditableItem(Game.Item.Inventory.InventoryItem item, int count, string editButtonText) : base(item, count)
        {
            this.editButtonText.Value = editButtonText;
        }
        
        public override void Dispose()
        {
            base.Dispose();
            
            editButtonText.Dispose();

            onClose.Dispose();
            onEdit.Dispose();
        }
    }
}
