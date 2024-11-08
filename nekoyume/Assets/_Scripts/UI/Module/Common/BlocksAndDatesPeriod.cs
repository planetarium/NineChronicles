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
            double secondsPerBlock,
            DateTime now)
        {
            blocksText.text = $"{beginningBlockIndex:N0} - {endBlockIndex:N0}";
            blocksText.enabled = true;

            var from = beginningBlockIndex.BlockIndexToDateTimeString(currentBlockIndex, secondsPerBlock, now);
            var to = endBlockIndex.BlockIndexToDateTimeString(currentBlockIndex, secondsPerBlock, now);
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
