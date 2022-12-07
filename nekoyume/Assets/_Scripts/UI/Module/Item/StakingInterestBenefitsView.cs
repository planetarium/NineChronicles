using Nekoyume.Helper;
using Nekoyume.Model.Item;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class StakingInterestBenefitsView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private TextMeshProUGUI benefitsText;

        public void Set(ItemBase itemBase, int count, int benefitsRate)
        {
            iconImage.sprite = BaseItemView.GetItemIcon(itemBase);
            countText.text = $"+{count}";
            benefitsText.text = $"{benefitsRate}%";
        }

        public void Set(int runeId, int count, int benefitsRate)
        {
            iconImage.sprite = SpriteHelper.GetItemIcon(runeId);
            countText.text = $"+{count}";
            benefitsText.text = $"{benefitsRate}%";
        }
    }
}
