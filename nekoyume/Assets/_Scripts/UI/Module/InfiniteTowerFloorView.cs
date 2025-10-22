using Nekoyume.TableData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class InfiniteTowerFloorView : MonoBehaviour
    {
        public enum FloorState
        {
            Current,      // 현재 도전 중인 층 (하이라이트)
            Locked,       // 잠긴 층 (어둡게 + 자물쇠)
            Cleared,      // 클리어한 층 (체크마크 + 밝은 색상)
        }

        [SerializeField] private GameObject floorContainer;      // 전체 층 컨테이너

        [SerializeField] private TextMeshProUGUI floorLockNumberText;  // 층 번호 텍스트 (예: "9F")
        [SerializeField] private TextMeshProUGUI floorOpenNumberText;  // 층 번호 텍스트 (예: "9F")
        [SerializeField] private Image floorBackgroundImage;       // 층 배경 이미지

        // 층별 보상 아이콘 (스크린샷의 원형 아이콘들)
        [SerializeField] private BaseItemView itemRewardIcon;             // 첫번째 아이템 보상 아이콘
        [SerializeField] private BaseItemView fungibleAssetRewardIcon;    // 첫번째 Fungible Asset 보상 아이콘
        [SerializeField] private GameObject rewardIconsContainer;  // 보상 아이콘 컨테이너

        [SerializeField] private GameObject openContainer;
        [SerializeField] private GameObject doorClear;
        [SerializeField] private GameObject doorNoClear;
        [SerializeField] private GameObject lockContainer;

        private int _floorNumber;
        private InfiniteTowerFloorSheet.Row _floorData;

        public void SetState(FloorState state, int floorNumber, InfiniteTowerFloorSheet.Row floorData)
        {
            _floorNumber = floorNumber;
            _floorData = floorData;

            // 층 번호 설정
            floorLockNumberText.text = $"{floorNumber}F";
            floorOpenNumberText.text = $"{floorNumber}F";
            UnityEngine.Debug.Log($"[InfiniteTowerFloorView] SetState - FloorNumber: {floorNumber}, State: {state}, FloorData: {(floorData != null ? "Found" : "Null")}");

            // 층 상태에 따른 UI 설정
            UpdateFloorUI(state);

            // 층 속성 아이콘 설정 (floorData 기반)
            if (floorData != null)
            {
                SetFloorRewards(floorData);
            }
        }

        private void UpdateFloorUI(FloorState state)
        {
            if (floorBackgroundImage == null) return;

            switch (state)
            {
                case FloorState.Current:
                    openContainer.gameObject.SetActive(true);
                    lockContainer.gameObject.SetActive(false);
                    doorClear.gameObject.SetActive(false);
                    doorNoClear.gameObject.SetActive(true);
                    break;
                case FloorState.Cleared:
                    openContainer.gameObject.SetActive(true);
                    lockContainer.gameObject.SetActive(false);
                    doorClear.gameObject.SetActive(true);
                    doorNoClear.gameObject.SetActive(false);
                    break;
                case FloorState.Locked:
                    openContainer.gameObject.SetActive(false);
                    lockContainer.gameObject.SetActive(true);
                    break;
            }
        }

        private void SetFloorRewards(InfiniteTowerFloorSheet.Row floorData)
        {
            if (rewardIconsContainer == null) return;

            rewardIconsContainer.SetActive(true);

            // 첫번째 아이템 보상 아이콘
            if (floorData.ItemRewardId1 is > 0)
            {
                itemRewardIcon.gameObject.SetActive(true);
                itemRewardIcon.ItemViewSetItemData(floorData.ItemRewardId1.Value, floorData.ItemRewardCount1!.Value);
            }
            else
            {
                itemRewardIcon.gameObject.SetActive(false);
            }

            // 첫번째 Fungible Asset 보상 아이콘
            if (!string.IsNullOrEmpty(floorData.FungibleAssetRewardTicker1) &&
                floorData.FungibleAssetRewardAmount1 is > 0)
            {
                fungibleAssetRewardIcon.gameObject.SetActive(true);
                fungibleAssetRewardIcon.ItemViewSetCurrencyData(floorData.FungibleAssetRewardTicker1, floorData.FungibleAssetRewardAmount1.Value);
            }
            else
            {
                fungibleAssetRewardIcon.gameObject.SetActive(false);
            }
        }
    }
}
