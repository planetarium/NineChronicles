using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
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
                if (Game.Game.instance.AdventureBossData.SeasonInfo?.Value is null)
                {
                    NcDebug.LogError("[UnlockFloor] : Game.Game.instance.AdventureBossData.SeasonInfo is null or States.Instance.CurrentAvatarState is null");
                }
                else
                {
                    ActionManager.Instance.UnlockFloor(true, (int)Game.Game.instance.AdventureBossData.SeasonInfo.Value.Season).Subscribe(eval =>
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
                }
            }).AddTo(gameObject);

            goldenDustUnlockButton.OnSubmitSubject.Subscribe(_ =>
            {
                Close();
                NcDebug.Log("Unlocking floor " + _floorIndex + " with goldenDust" + _goldenDustCost);
                _loadingStart?.Invoke();
                if (Game.Game.instance.AdventureBossData.SeasonInfo?.Value is null)
                {
                    NcDebug.LogError("[UnlockFloor] : Game.Game.instance.AdventureBossData.SeasonInfo is null or States.Instance.CurrentAvatarState is null");
                }
                else
                {
                    ActionManager.Instance.UnlockFloor(false, (int)Game.Game.instance.AdventureBossData.SeasonInfo.Value.Season).Subscribe(eval =>
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
                }
            }).AddTo(gameObject);
        }

        public void Show(int floor, System.Action loadingStart, System.Action<bool> loadingEnd, bool ignoreShowAnimation = false)
        {
            _floorIndex = floor;
            _loadingStart = loadingStart;
            _loadingEnd = loadingEnd;
            if (Game.Game.instance.AdventureBossData.GetCurrentUnlockFloorCost(_floorIndex + 1, out var unlockData))
            {
                goldUnlockButton.SetCost(CostType.NCG, (long)unlockData.NcgPrice);
                goldUnlockButton.SetText(L10nManager.Localize("UI_ADVENTURE_BOSS_UNLOCK_FLOOR_NCG_OPEN_BTN"));
                _goldCost = (int)unlockData.NcgPrice;
                goldenDustUnlockButton.SetCost(CostType.GoldDust, unlockData.GoldenDustPrice);
                goldUnlockButton.SetText(L10nManager.Localize("UI_ADVENTURE_BOSS_UNLOCK_FLOOR_GOLDENDUST_OPEN_BTN"));
                _goldenDustCost = unlockData.GoldenDustPrice;
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
