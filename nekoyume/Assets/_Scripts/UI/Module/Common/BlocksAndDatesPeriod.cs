using System;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module.Common
{
    public class BlocksAndDatesPeriod : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI blocksText;

        [SerializeField]
        private TextMeshProUGUI datesText;

        public void Show(
            long beginningBlockIndex,
            long endBlockIndex,
            long currentBlockIndex,
            DateTime now)
        {
            blocksText.text = $"{beginningBlockIndex:N0} - {endBlockIndex:N0}";
            blocksText.enabled = true;

            var from = beginningBlockIndex.BlockIndexToDateTimeString(currentBlockIndex, now);
            var to = endBlockIndex.BlockIndexToDateTimeString(currentBlockIndex, now);
            datesText.text = $"({from} - {to})";
            datesText.enabled = true;
        }

        public void Hide()
        {
            blocksText.enabled = false;
            datesText.enabled = false;
        }
    }
}
