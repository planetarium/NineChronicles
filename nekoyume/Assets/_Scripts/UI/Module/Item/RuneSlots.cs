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
            foreach (var (view, slot) in GetZippedRuneSlotViewsWithRuneSlotStates(slots, runeSlotStates))
            {
                view.Set(slot, onClick, onDoubleClick);
            }
        }

        public void Set(
            List<RuneSlot> runeSlotStates,
            AllRuneState allRuneState,
            System.Action<RuneSlotView> onClick)
        {
            foreach (var (view, slot) in GetZippedRuneSlotViewsWithRuneSlotStates(slots, runeSlotStates))
            {
                RuneState runeState = null;
                if (slot.RuneId.HasValue)
                {
                    allRuneState.TryGetRuneState(slot.RuneId.Value, out runeState);
                }

                view.Set(slot, runeState, onClick);
            }
        }

        private static List<(RuneSlotView, RuneSlot)> GetZippedRuneSlotViewsWithRuneSlotStates(
            IEnumerable<RuneSlotView> slotViews,
            IEnumerable<RuneSlot> runeSlotStates)
        {
            var orderedSlotStates = runeSlotStates
                .Where(runeSlot => runeSlot.RuneSlotType != RuneSlotType.Stake)
                .OrderBy(slot => slot.RuneType)
                .ThenBy(slot => slot.RuneSlotType);
            return slotViews.Zip(orderedSlotStates, (view, slot) => (view, slot)).ToList();
        }
    }
}
