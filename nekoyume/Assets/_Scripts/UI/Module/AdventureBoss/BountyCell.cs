using Nekoyume.Action.AdventureBoss;
using Nekoyume.Helper;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.Module;
using Nekoyume.UI.Scroller;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume
{
    public class BountyCell : RectCell<BountyItemData, BountyViewScroll.ContextModel>
    {
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private TextMeshProUGUI ncgText;
        [SerializeField] private GameObject bonusObj;
        [SerializeField] private TextMeshProUGUI bonusText;
        [SerializeField] private Color nomalStateColor;
        [SerializeField] private Color bonusStateColor;

        public override void UpdateContent(BountyItemData itemData)
        {
            rankText.text = itemData.Rank.ToString();
            nameText.text = itemData.Name;
            countText.text = $"{itemData.Count}/{Investor.MaxInvestmentCount}";
            ncgText.text = itemData.Ncg.ToString("#,0");
            bonusObj.SetActive(itemData.Bonus > 0);

            if(itemData.Bonus > 0)
            {
                rankText.color = bonusStateColor;
                nameText.color = bonusStateColor;
                countText.color = bonusStateColor;
                ncgText.color = bonusStateColor;
            }
            else
            {
                rankText.color = nomalStateColor;
                nameText.color = nomalStateColor;
                countText.color = nomalStateColor;
                ncgText.color = nomalStateColor;
            }

            bonusText.text = $"x{itemData.Bonus.ToString("F1")}";
        }
    }
}
