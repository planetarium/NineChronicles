using Nekoyume.Model.Item;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class InventoryItem : IItemViewModel
    {
        public ItemBase ItemBase { get; }

        public readonly ReactiveProperty<int> Count;
        public readonly ReactiveProperty<bool> LevelLimited;
        public readonly ReactiveProperty<bool> Equipped;
        public readonly ReactiveProperty<bool> Tradable;
        public readonly ReactiveProperty<bool> DimObjectEnabled;
        public readonly ReactiveProperty<bool> Selected;
        public readonly ReactiveProperty<bool> Focused;
        public readonly ReactiveProperty<bool> HasNotification;
        public readonly ReactiveProperty<int> GrindingCount;
        public readonly ReactiveProperty<bool> Disabled;
        public readonly Subject<bool> GrindingCountEnabled;

        public RectTransform View { get; set; }

        public InventoryItem(ItemBase itemBase, int count, bool equipped, bool limited, bool tradable)
        {
            ItemBase = itemBase;
            Count = new ReactiveProperty<int>(count);
            Equipped = new ReactiveProperty<bool>(equipped);
            LevelLimited = new ReactiveProperty<bool>(limited);
            Tradable = new ReactiveProperty<bool>(tradable);
            DimObjectEnabled = new ReactiveProperty<bool>(false);
            Selected = new ReactiveProperty<bool>(false);
            Focused = new ReactiveProperty<bool>(false);
            HasNotification = new ReactiveProperty<bool>(false);
            GrindingCount = new ReactiveProperty<int>(0);
            Disabled = new ReactiveProperty<bool>(false);
            GrindingCountEnabled = new Subject<bool>();
        }
    }
}
