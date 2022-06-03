using Nekoyume.TableData;
using System.Numerics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CostItemView : VanillaItemView
    {
        [SerializeField]
        private CostIconDataScriptableObject costIconData;

        [SerializeField]
        private Image costIcon;

        [SerializeField]
        private TextMeshProUGUI countText;

        [SerializeField]
        private TextMeshProUGUI costText;

        public void SetData(ItemSheet.Row itemRow, CostType costType, int count, BigInteger cost)
        {
            costIcon.overrideSprite = costIconData.GetIcon(costType);
            costText.text = cost.ToString();
            countText.text = $"x{count}";
            SetData(itemRow);
        }
    }
}
