using System;
using System.Collections.Generic;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class CombinationResultPopup : CountableItem
    {
        public bool isSuccess;
        public ICollection<CountEditableItem> materialItems;

        public readonly Subject<CombinationResultPopup> onClickSubmit = new Subject<CombinationResultPopup>();
        
        public CombinationResultPopup(Game.Item.Inventory.InventoryItem value, int count) : base(value, count)
        {
        }
        
        public override void Dispose()
        {   
            base.Dispose();
            onClickSubmit.Dispose();
        }
    }
}
