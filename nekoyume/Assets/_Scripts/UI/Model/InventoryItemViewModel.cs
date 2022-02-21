using Nekoyume.Model.Item;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class InventoryItemViewModel : IItemViewModel
    {
        public ItemBase ItemBase { get; }

        public readonly ReactiveProperty<int> Count;
        public readonly ReactiveProperty<bool> LevelLimited;
        public readonly ReactiveProperty<bool> Equipped;
        public readonly ReactiveProperty<bool> Tradable;
        public readonly ReactiveProperty<bool> ElementalTypeDisabled;
        public readonly ReactiveProperty<bool> Selected;
        public readonly ReactiveProperty<bool> Focused;
        public readonly ReactiveProperty<bool> HasNotification;

        public RectTransform View { get; set; }

        public InventoryItemViewModel(ItemBase itemBase, int count, bool equipped, bool limited, bool tradable)
        {
            ItemBase = itemBase;
            Count = new ReactiveProperty<int>(count);
            Equipped = new ReactiveProperty<bool>(equipped);
            LevelLimited = new ReactiveProperty<bool>(limited);
            Tradable = new ReactiveProperty<bool>(tradable);
            ElementalTypeDisabled = new ReactiveProperty<bool>(false);
            Selected = new ReactiveProperty<bool>(false);
            Focused = new ReactiveProperty<bool>(false);
            HasNotification = new ReactiveProperty<bool>(false);
        }
    }
}
