using Lib9c;
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

            for (int i = 0; i < rewardItemViews.Length; i++)
            {
                if(i < rewards.Count)
                {
                    rewardItemViews[i].gameObject.SetActive(true);
                    var reward = rewards[i];
                    switch (reward.ItemType)
                    {
                        case "Material":
                            rewardItemViews[i].ItemViewSetItemData(reward.ItemId, reward.Amount);
                            break;
                        case "Rune":
                            rewardItemViews[i].ItemViewSetCurrencyData(reward.ItemId, reward.Amount);
                            break;
                        case "Crystal":
                            rewardItemViews[i].ItemViewSetCurrencyData(Currencies.Crystal.Ticker, rewards[i].Amount);
                            break;
                        default:
                            NcDebug.LogError("Invalid ItemType: " + reward.ItemType);
                            break;
                    }
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
