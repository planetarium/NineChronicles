using Nekoyume.Model.Item;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class EquipmentInventoryViewModel : IItemViewModel
    {
        public ItemBase ItemBase { get; }

        public readonly ReactiveProperty<bool> Equipped;
        public readonly ReactiveProperty<bool> LevelLimited;
        public readonly ReactiveProperty<bool> Selected;
        public readonly ReactiveProperty<bool> SelectedBase;
        public readonly ReactiveProperty<bool> SelectedMaterial;
        public readonly ReactiveProperty<bool> Disabled;

        public RectTransform View { get; set; }

        public EquipmentInventoryViewModel(ItemBase itemBase, bool equipped, bool levelLimited)
        {
            ItemBase = itemBase;
            Equipped = new ReactiveProperty<bool>(equipped);
            LevelLimited = new ReactiveProperty<bool>(levelLimited);
            Selected = new ReactiveProperty<bool>();
            SelectedBase = new ReactiveProperty<bool>();
            SelectedMaterial = new ReactiveProperty<bool>();
            Disabled = new ReactiveProperty<bool>();
        }
    }
}
