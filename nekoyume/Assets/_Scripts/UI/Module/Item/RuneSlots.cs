using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.EnumType;
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
            var orderedSlotStates = runeSlotStates
                .Where(runeSlot => runeSlot.RuneSlotType != RuneSlotType.Stake)
                .OrderBy(slot => slot.RuneType)
                .ThenBy(slot => slot.RuneSlotType);
            foreach (var (view, slot) in slots.Zip(orderedSlotStates, (view, slot) => (view, slot)))
            {
                view.Set(slot, onClick, onDoubleClick);
            }
        }

        public void Set(
            List<RuneSlot> runeSlotStates,
            List<RuneState> runeStates,
            System.Action<RuneSlotView> onClick)
        {
            var orderedSlotStates = runeSlotStates
                .Where(runeSlot => runeSlot.RuneSlotType != RuneSlotType.Stake)
                .OrderBy(slot => slot.RuneType)
                .ThenBy(slot => slot.RuneSlotType);
            foreach (var (view, slot) in slots.Zip(
                         orderedSlotStates,
                         (view, slot) => (view, slot))
                    )
            {
                RuneState runeState = null;
                if (slot.RuneId.HasValue)
                {
                    runeState = runeStates.FirstOrDefault(x => x.RuneId == slot.RuneId.Value);
                }

                view.Set(slot, runeState, onClick);
            }
        }
    }
}
