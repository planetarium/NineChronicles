using Nekoyume.L10n;
using System.Collections;
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

        public void SetData(int floor)
        {
            floorText.text = $"{floor}F";
            floorDescText.text = L10nManager.Localize("UI_ADVENTUREBOSS_FLOOR_REWARD_DESC", floor);
        }
    }
}
