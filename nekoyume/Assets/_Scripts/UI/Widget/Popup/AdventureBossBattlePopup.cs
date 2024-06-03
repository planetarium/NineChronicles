using Nekoyume.UI.Module;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class AdventureBossBattlePopup : PopupWidget
    {
        [SerializeField] private GameObject[] challengeFloors;
        [SerializeField] private GameObject[] breakThroughFloors;
        [SerializeField] private TextMeshProUGUI challengeFloorText;
        [SerializeField] private TextMeshProUGUI breakThroughFloorText;
        [SerializeField] private ConditionalButton challengeButton;
        [SerializeField] private ConditionalButton breakThroughButton;
        [SerializeField] private BaseItemView[] firstClearItems;
        [SerializeField] private BaseItemView[] challengeRandomItems;
        [SerializeField] private BaseItemView[] breakThroughRandomItems;
        [SerializeField] private TextMeshProUGUI challengeApCostText;
        [SerializeField] private TextMeshProUGUI breakThroughApCostText;
        [SerializeField] private GameObject challengeLockObj;
        [SerializeField] private GameObject challengeContents;
        [SerializeField] private Button gotoUnlock;

        public override void Show(bool ignoreShowAnimation = false)
        {
            var adventurebossData = Game.Game.instance.AdventureBossData;
            var currentFloor = 0;
            var maxFloor = 5;

            if(adventurebossData.ExploreInfo.Value != null)
            {
                currentFloor = adventurebossData.ExploreInfo.Value.Floor;
                maxFloor = adventurebossData.ExploreInfo.Value.MaxFloor;
            }

            if(currentFloor >= maxFloor)
            {
                challengeLockObj.SetActive(true);
                challengeContents.SetActive(false);
            }

            for (int i = 0; i < challengeFloors.Length; i++)
            {
                breakThroughFloors[i].SetActive(i < currentFloor);
                challengeFloors[i].SetActive(i > currentFloor && i < maxFloor);
            }
            challengeFloorText.text = $"{currentFloor}F ~ {maxFloor}F";
            breakThroughFloorText.text = $"{1}F ~ {currentFloor}F";
            
            base.Show(ignoreShowAnimation);
        }

    }
}
