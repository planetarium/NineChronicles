using System;
using Nekoyume.Model.Item;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class InventoryItemViewModel : IDisposable, IItemViewModel
    {
        public ItemBase ItemBase { get; }

        public readonly ReactiveProperty<int> Count;
        public readonly ReactiveProperty<bool> Disabled;
        public readonly ReactiveProperty<bool> Selected;
        public readonly ReactiveProperty<bool> Focused;
        public readonly ReactiveProperty<bool> Equipped;
        public readonly ReactiveProperty<bool> HasNotification;

        public RectTransform View { get; set; }

        public InventoryItemViewModel(ItemBase itemBase, int count, bool equipped, bool disabled)
        {
            ItemBase = itemBase;
            Count = new ReactiveProperty<int>(count);
            Disabled = new ReactiveProperty<bool>(disabled);
            Selected = new ReactiveProperty<bool>(false);
            Focused = new ReactiveProperty<bool>(false);
            Equipped = new ReactiveProperty<bool>(equipped);
            HasNotification = new ReactiveProperty<bool>(false);
        }

        public void Dispose()
        {
            Count.Dispose();
            Disabled.Dispose();
            Selected.Dispose();
            Focused.Dispose();
            Equipped.Dispose();
            HasNotification.Dispose();
        }
    }
}
