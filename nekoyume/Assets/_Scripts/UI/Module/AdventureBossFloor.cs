using Cysharp.Threading.Tasks;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
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

        public void SetState(FloorState floorState, int floorIndex)
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
                    if (Game.Game.instance.AdventureBossData.UnlockDict.TryGetValue(_floorIndex,
                            out var unlockData))
                    {
                        if (unlockData.TryGetValue("NCG", out var ncg))
                        {
                            goldUnlockCount.text = ncg.ToString();
                        }

                        if (unlockData.TryGetValue("GoldenDust", out var gd))
                        {
                            goldenDustUnlockCount.text = gd.ToString();
                        }
                    }
                    else
                    {
                        NcDebug.LogError($"UnlockDict not found key {_floorIndex}");
                    }

                    openNameText.text = $"{_floorIndex + 1}F ~ {_floorIndex + 5}F";
                    break;
            }
        }
    }
}
