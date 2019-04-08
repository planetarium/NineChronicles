using System;
using System.Collections.Generic;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class CombinationResultPopup<T> : IDisposable where T : Game.Item.Inventory.InventoryItem
    {
        public bool IsSuccess;
        public T ResultItem;
        public ICollection<CountEditableItem<T>> MaterialItems;

        public readonly Subject<CombinationResultPopup<T>> OnClickSubmit = new Subject<CombinationResultPopup<T>>();
        
        public void Dispose()
        {   
            OnClickSubmit.Dispose();
        }
    }
}
