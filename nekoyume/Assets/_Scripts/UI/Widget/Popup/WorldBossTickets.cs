using Nekoyume.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class WorldBossTickets : MonoBehaviour
    {
        [SerializeField]
        private Slider slider;

        [SerializeField]
        private TextMeshProUGUI fillText;

        [SerializeField]
        private TextMeshProUGUI timespanText;

        public void Set(long remainBlockIndex, int remainTicket, int maxTicket)
        {
            timespanText.text = Util.GetBlockToTime(remainBlockIndex);

            fillText.text = $"{remainTicket}/{maxTicket}";
            slider.normalizedValue = remainTicket / (float)maxTicket;
        }
    }
}
