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
        public readonly ReactiveProperty<bool> Limited;
        public readonly ReactiveProperty<bool> Equipped;
        public readonly ReactiveProperty<bool> Disabled;
        public readonly ReactiveProperty<bool> Selected;
        public readonly ReactiveProperty<bool> Focused;
        public readonly ReactiveProperty<bool> HasNotification;

        public RectTransform View { get; set; }

        public InventoryItemViewModel(ItemBase itemBase, int count, bool equipped, bool limited)
        {
            ItemBase = itemBase;
            Count = new ReactiveProperty<int>(count);
            Equipped = new ReactiveProperty<bool>(equipped);
            Limited = new ReactiveProperty<bool>(limited);
            Disabled = new ReactiveProperty<bool>(false);
            Selected = new ReactiveProperty<bool>(false);
            Focused = new ReactiveProperty<bool>(false);
            HasNotification = new ReactiveProperty<bool>(false);
        }

        public void Dispose()
        {
            Count.Dispose();
            Equipped.Dispose();
            Limited.Dispose();
            Disabled.Dispose();
            Selected.Dispose();
            Focused.Dispose();
            HasNotification.Dispose();
        }
    }
}
