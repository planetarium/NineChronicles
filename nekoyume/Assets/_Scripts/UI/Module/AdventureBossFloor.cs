using Cysharp.Threading.Tasks;
using DG.Tweening;
using Nekoyume.Game;
using Nekoyume.TableData.AdventureBoss;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Module
{
    public class AdventureBossFloor : MonoBehaviour
    {
        public enum FloorState
        {
            Clear,
            NotClear,
            Lock,
            UnLock
        }

        [SerializeField] private GameObject floorOpen;
        [SerializeField] private GameObject floorClear;
        [SerializeField] private GameObject floorNotClear;
        [SerializeField] private GameObject floorLock;
        [SerializeField] private GameObject openNameImage;
        [SerializeField] private TextMeshProUGUI openNameText;
        [SerializeField] private GameObject indicatorObj;
        [SerializeField] private TextMeshProUGUI goldenDustUnlockCount;
        [SerializeField] private TextMeshProUGUI goldUnlockCount;
        [SerializeField] private GameObject unlockEffect;
        [SerializeField] private GameObject lockEffect;

        private int _floorIndex;

        public void OnClickUnlockAction()
        {
            NcDebug.Log($"OnClickUnlockAction {_floorIndex}");
            Widget.Find<AdventureBoss_UnlockLockedFloorPopup>()
                .Show(_floorIndex, LoadingStart, LoadingEnd);
        }

        public void LoadingStart()
        {
            indicatorObj.SetActive(true);
        }

        public void LoadingEnd(bool unlock)
        {
            indicatorObj.SetActive(false);
            if (unlock)
            {
                openNameImage.SetActive(false);
                lockEffect.SetActive(false);
                unlockEffect.SetActive(true);
                UniTask.Delay(TimeSpan.FromSeconds(1.5f)).ContinueWith(() =>
                {
                    Game.Game.instance.AdventureBossData.RefreshAllByCurrentState().Forget();
                }).Forget();
            }
        }

        public void SetState(FloorState floorState, int floorIndex, AdventureBossUnlockFloorCostSheet.Row unlockFloorCostRow = null)
        {
            _floorIndex = floorIndex;
            unlockEffect.SetActive(false);
            switch (floorState)
            {
                case FloorState.Clear:
                    floorOpen.SetActive(true);
                    floorClear.SetActive(true);
                    floorNotClear.SetActive(false);
                    floorLock.SetActive(false);
                    openNameImage.SetActive(false);
                    break;
                case FloorState.NotClear:
                    floorOpen.SetActive(true);
                    floorClear.SetActive(false);
                    floorNotClear.SetActive(true);
                    floorLock.SetActive(false);
                    openNameImage.SetActive(false);
                    break;
                case FloorState.Lock:
                    floorOpen.SetActive(false);
                    floorClear.SetActive(false);
                    floorNotClear.SetActive(false);
                    floorLock.SetActive(true);
                    openNameImage.SetActive(false);
                    break;
                case FloorState.UnLock:
                    floorOpen.SetActive(false);
                    floorClear.SetActive(false);
                    floorNotClear.SetActive(false);
                    floorLock.SetActive(true);
                    openNameImage.SetActive(true);
                    lockEffect.SetActive(true);
                    openNameText.text = $"{_floorIndex + 1}F ~ {_floorIndex + 5}F";

                    if (unlockFloorCostRow == null)
                    {
                        NcDebug.LogError($"Not found unlock floor cost data. floor: {_floorIndex + 1}");
                        return;
                    }
                    goldUnlockCount.text = unlockFloorCostRow.NcgPrice.ToString();
                    goldenDustUnlockCount.text = unlockFloorCostRow.GoldenDustPrice.ToString();
                    break;
            }
        }
    }
}
