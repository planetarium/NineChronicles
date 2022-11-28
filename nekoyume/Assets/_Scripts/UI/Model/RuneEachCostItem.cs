using System;
using Nekoyume.EnumType;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Model
{
    [Serializable]
    public class RuneEachCostItem
    {
        [SerializeField]
        private RuneCostType costType;

        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TextMeshProUGUI countText;

        public RuneCostType CostType => costType;

        public void Set(int count)
        {
            if (count > 0)
            {
                iconImage.gameObject.SetActive(true);
                countText.gameObject.SetActive(true);
                countText.text = count.ToCurrencyNotation();
            }
            else
            {
                iconImage.gameObject.SetActive(false);
                countText.gameObject.SetActive(false);
            }
        }
    }
}
