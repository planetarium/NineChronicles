using System.Collections.Generic;
using Nekoyume.Model.Rune;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class RuneSlots : MonoBehaviour
    {
        [SerializeField]
        private List<RuneSlotView> slots;

        public void Set(
            List<RuneSlot> runeSlotStates,
            System.Action<RuneSlotView> onClick,
            System.Action<RuneSlotView> onDoubleClick)
        {
            foreach (var state in runeSlotStates)
            {
                slots[state.Index].Set(state, onClick, onDoubleClick);
            }
        }
    }
}
