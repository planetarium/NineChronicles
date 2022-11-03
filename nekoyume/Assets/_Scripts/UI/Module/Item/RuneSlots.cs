using System.Collections;
using System.Collections.Generic;
using Nekoyume.Model.Rune;
using Nekoyume.Model.State;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class RuneSlots : MonoBehaviour
    {
        [SerializeField]
        private List<RuneSlotView> slots;

        public void Set(
            Dictionary<int, RuneSlot> runeSlots,
            System.Action<RuneSlotView> onClick,
            System.Action<RuneSlotView> onDoubleClick)
        {
            foreach (var (key, value) in runeSlots)
            {
                slots[key].Set(value, onClick, onDoubleClick);
            }
        }

        public void ActiveWearable(List<int> slotIndexes)
        {
            foreach (var index in slotIndexes)
            {
                slots[index].IsWearableImage = true;
            }
        }
    }
}
