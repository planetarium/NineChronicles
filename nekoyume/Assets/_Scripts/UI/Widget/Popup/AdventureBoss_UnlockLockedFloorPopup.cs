using Nekoyume.UI.Module;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using System;

namespace Nekoyume.UI
{
    using Nekoyume.Action.Exceptions.AdventureBoss;
    using Nekoyume.Blockchain;
    using Nekoyume.L10n;
    using Nekoyume.Model.Mail;
    using UniRx;
    public class AdventureBoss_UnlockLockedFloorPopup : PopupWidget
    {
        [SerializeField] private TextMeshProUGUI floorName;
        [SerializeField] private TextMeshProUGUI floorDescription;
        [SerializeField] private ConditionalCostButton goldenDustUnlockButton;
        [SerializeField] private ConditionalCostButton goldUnlockButton;

        private int _floorIndex;
        private int _goldCost;
        private int _goldenDustCost;
        private System.Action _loadingStart;
        private System.Action<bool> _loadingEnd;

        protected override void Awake()
        {
            base.Awake();
            goldUnlockButton.OnSubmitSubject.Subscribe(_ =>
            {
                Close();
                NcDebug.Log("Unlocking floor " + _floorIndex + " with gold " + _goldCost);
                _loadingStart?.Invoke();
                ActionManager.Instance.UnlockFloor(true).Subscribe(eval =>
                {
                    if (eval.Exception != null)
                    {
                        NcDebug.LogError(eval.Exception);
                        OneLineSystem.Push(MailType.System, eval.Exception.InnerException.Message, Scroller.NotificationCell.NotificationType.Alert);
                        _loadingEnd?.Invoke(false);
                        return;
                    }
                    _loadingEnd?.Invoke(true);
                });
            }).AddTo(gameObject);

            goldenDustUnlockButton.OnSubmitSubject.Subscribe(_ =>
            {
                Close();
                NcDebug.Log("Unlocking floor " + _floorIndex + " with goldenDust" + _goldenDustCost);
                _loadingStart?.Invoke();
                ActionManager.Instance.UnlockFloor(false).Subscribe(eval =>
                {
                    if (eval.Exception != null)
                    {
                        NcDebug.LogError(eval.Exception);
                        OneLineSystem.Push(MailType.System, eval.Exception.InnerException.Message, Scroller.NotificationCell.NotificationType.Alert);
                        _loadingEnd?.Invoke(false);
                        return;
                    }
                    _loadingEnd?.Invoke(true);
                });
            }).AddTo(gameObject);
        }

        public void Show(int floor, System.Action loadingStart, System.Action<bool> loadingEnd, bool ignoreShowAnimation = false)
        {
            _floorIndex = floor;
            _loadingStart = loadingStart;
            _loadingEnd = loadingEnd;
            if (Game.Game.instance.AdventureBossData.UnlockDict.TryGetValue(_floorIndex, out var unlockData))
            {
                if(unlockData.TryGetValue("NCG", out var ncgCost))
                {
                    goldUnlockButton.SetCost(CostType.NCG, ncgCost);
                    goldUnlockButton.SetText(L10nManager.Localize("UI_ADVENTURE_BOSS_UNLOCK_FLOOR_NCG_OPEN_BTN"));
                    _goldCost = ncgCost;
                }
                if(unlockData.TryGetValue("GoldenDust", out var goldenDustCost))
                {
                    goldenDustUnlockButton.SetCost(CostType.GoldDust, goldenDustCost);
                    goldUnlockButton.SetText(L10nManager.Localize("UI_ADVENTURE_BOSS_UNLOCK_FLOOR_GOLDENDUST_OPEN_BTN"));
                    _goldenDustCost = goldenDustCost;
                }
                floorName.text = $"{_floorIndex + 1}F ~ {_floorIndex + 5}F";
                floorDescription.text = L10nManager.Localize("UI_ADVENTURE_BOSS_UNLOCK_FLOOR_DESC", _floorIndex + 1, _floorIndex + 5);

                base.Show(ignoreShowAnimation);
            }
            else
            {
                NcDebug.LogError("Unlock data not found for floor " + _floorIndex);
            }
        }
    }
}
