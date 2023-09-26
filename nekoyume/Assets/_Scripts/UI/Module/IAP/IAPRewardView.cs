using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
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

        private ItemBase itemBaseForToolTip = null;

        public void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() => {
                if (itemBaseForToolTip == null)
                    return;

                AudioController.PlayClick();
                var tooltip = ItemTooltip.Find(itemBaseForToolTip.ItemType);
                tooltip.Show(itemBaseForToolTip, string.Empty, false, null);
            });
        }

        public void SetItemBase(ItemBase itemBase)
        {
            itemBaseForToolTip = itemBase;
        }
    }
}
