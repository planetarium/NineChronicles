using System.Collections.Generic;
using System.Linq;
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
            List<RuneSlot> runeSlotStates,
            System.Action<RuneSlotView> onClick,
            System.Action<RuneSlotView> onDoubleClick)
        {
            foreach (var state in runeSlotStates)
            {
                slots[state.Index].Set(state, onClick, onDoubleClick);
            }
        }

        public void Set(
            List<RuneSlot> runeSlotStates,
            List<RuneState> runeStates,
            System.Action<RuneSlotView> onClick)
        {
            foreach (var state in runeSlotStates)
            {
                RuneState runeState = null;
                if (state.RuneId.HasValue)
                {
                    runeState = runeStates.FirstOrDefault(x => x.RuneId == state.RuneId.Value);
                }

                slots[state.Index].Set(state, runeState, onClick);
            }
        }
    }
}
