using Nekoyume.ActionExtensions;
using Nekoyume.L10n;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nekoyume
{
    public class FloorRewardCell : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI floorText;
        [SerializeField] private TextMeshProUGUI floorDescText;
        [SerializeField] private BaseItemView[] rewardItemViews;
        [SerializeField] private GameObject receivedObj;

        public void SetData(int floor, List<TableData.AdventureBoss.AdventureBossSheet.RewardAmountData> rewards)
        {
            if (Game.Game.instance.AdventureBossData.ExploreInfo.Value == null)
            {
                receivedObj.SetActive(false);
            }
            else
            {
                receivedObj.SetActive(Game.Game.instance.AdventureBossData.ExploreInfo.Value.Floor >= floor);
            }

            floorText.text = $"{floor}F";

            for (var i = 0; i < rewardItemViews.Length; i++)
            {
                if (i < rewards.Count)
                {
                    rewardItemViews[i].gameObject.SetActive(true);
                    var reward = rewards[i];
                    rewardItemViews[i].ItemViewSetAdventureBossItemData(reward);
                }
                else
                {
                    rewardItemViews[i].gameObject.SetActive(false);
                }
            }


            floorDescText.text = L10nManager.Localize("UI_ADVENTUREBOSS_FLOOR_REWARD_DESC", floor);
        }
    }
}
