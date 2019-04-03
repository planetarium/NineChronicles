using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using Uno.Extensions;

namespace Nekoyume.UI.Model
{
    public class Inventory : IDisposable
    {
        public class Item : Game.Item.Inventory.InventoryItem, IDisposable
        {
            public readonly ReactiveProperty<bool> Covered = new ReactiveProperty<bool>(false);
            public readonly ReactiveProperty<bool> Dimmed = new ReactiveProperty<bool>(false);
            public readonly ReactiveProperty<bool> Selected = new ReactiveProperty<bool>(false);

            public readonly Subject<Item> OnClick = new Subject<Item>();

            public Item(Game.Item.Inventory.InventoryItem item) : base(item)
            {
            }

            public void Dispose()
            {
                Covered.Dispose();
                Dimmed.Dispose();
                Selected.Dispose();

                OnClick.Dispose();
            }
        }

        public readonly ReactiveCollection<Item> Items = new ReactiveCollection<Item>();
        public readonly ReactiveProperty<Item> SelectedItem = new ReactiveProperty<Item>(null);

        public Inventory(List<Game.Item.Inventory.InventoryItem> items, params string[] ignoreDimmedTypes)
        {
            items.ForEach(item =>
            {
                var obj = new Item(item);
                obj.Dimmed.Value = !ignoreDimmedTypes.Contains(obj.Item.Data.Cls);
                obj.OnClick.Subscribe(SubscribeOnClick);
                Items.Add(obj);
            });

            Items.ObserveAdd().Subscribe(added =>
            {
                added.Value.Dimmed.Value = !ignoreDimmedTypes.Contains(added.Value.Item.Data.Cls);
                added.Value.OnClick.Subscribe(SubscribeOnClick);
            });

            SelectedItem.Subscribe(item =>
            {
                if (ReferenceEquals(item, null))
                {
                    return;
                }
                
                item.Selected.Value = true;
            });
        }

        public void Dispose()
        {
            Items.DisposeAll();
            SelectedItem.Dispose();
            SelectedItem.Value.Dispose();
        }

        private void SubscribeOnClick(Item item)
        {
            if (!ReferenceEquals(SelectedItem.Value, null))
            {
                SelectedItem.Value.Selected.Value = false;
            }

            SelectedItem.Value = item;
            SelectedItem.Value.Selected.Value = true;
        }
    }
}
