using System;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class CountEditableItem<T> : IDisposable where T : Game.Item.Inventory.InventoryItem
    {
        public readonly ReactiveProperty<T> Item = new ReactiveProperty<T>(null);
        public readonly ReactiveProperty<int> Count = new ReactiveProperty<int>(0);
        public readonly ReactiveProperty<string> EditButtonText = new ReactiveProperty<string>("");

        public readonly Subject<CountEditableItem<T>> OnClose = new Subject<CountEditableItem<T>>();
        public readonly Subject<CountEditableItem<T>> OnEdit = new Subject<CountEditableItem<T>>();
        
        public CountEditableItem(T item, int count, string editButtonText)
        {
            Item.Value = item;
            Count.Value = count;
            EditButtonText.Value = editButtonText;
        }
        
        public void Dispose()
        {
            Item.Dispose();
            Count.Dispose();
            EditButtonText.Dispose();

            OnClose.Dispose();
            OnEdit.Dispose();
        }
    }
}
