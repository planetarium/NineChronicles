using Nekoyume.Model.Item;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class EnhancementInventoryItem
    {
        public ItemBase ItemBase { get; }

        public readonly ReactiveProperty<bool> Equipped;
        public readonly ReactiveProperty<bool> LevelLimited;
        public readonly ReactiveProperty<bool> Selected;
        public readonly ReactiveProperty<bool> SelectedBase;
        public readonly ReactiveProperty<int> SelectedMaterialCount;
        public readonly ReactiveProperty<bool> Disabled;
        public readonly ReactiveProperty<bool> HasNotification;
        public readonly ReactiveProperty<int> Count;

        public EnhancementInventoryItem(ItemBase itemBase, bool equipped, bool levelLimited, int count)
        {
            ItemBase = itemBase;
            Equipped = new ReactiveProperty<bool>(equipped);
            LevelLimited = new ReactiveProperty<bool>(levelLimited);
            Selected = new ReactiveProperty<bool>();
            SelectedBase = new ReactiveProperty<bool>();
            SelectedMaterialCount = new ReactiveProperty<int>(0);
            Disabled = new ReactiveProperty<bool>();
            HasNotification = new ReactiveProperty<bool>();
            Count = new ReactiveProperty<int>(count);
        }
    }
}
