using Nekoyume.Helper;
using Nekoyume.Model.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class WorldBossTickets : MonoBehaviour
    {
        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private Slider slider;

        [SerializeField]
        private TextMeshProUGUI fillText;

        [SerializeField]
        private TextMeshProUGUI timespanText;

        public Image IconImage => iconImage;
        public int RemainTicket { get; private set; }

        private void Set(long remainBlockIndex, int remainTicket, int maxTicket)
        {
            timespanText.text = Util.GetBlockToTime(remainBlockIndex);

            fillText.text = $"{remainTicket}/{maxTicket}";
            slider.normalizedValue = remainTicket / (float)maxTicket;
        }

        public void UpdateTicket(RaiderState state, long current)
        {
            if (!WorldBossFrontHelper.TryGetCurrentRow(current, out var row))
            {
                return;
            }

            RemainTicket = WorldBossFrontHelper.GetRemainTicket(state, current);
            var start = row.StartedBlockIndex;
            var reminder = (current - start) % WorldBossHelper.RefillInterval;
            var remain = WorldBossHelper.RefillInterval - reminder;
            Set(remain, RemainTicket, WorldBossHelper.MaxChallengeCount);
        }
    }
}
