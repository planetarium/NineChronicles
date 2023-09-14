using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class IAPRewardView : MonoBehaviour
    {
        [field:SerializeField]
        public Image RewardGrade { get; private set; }

        [field:SerializeField]
        public Image RewardImage { get; private set; }

        [field:SerializeField]
        public TextMeshProUGUI RewardCount { get; private set; }
    }
}
