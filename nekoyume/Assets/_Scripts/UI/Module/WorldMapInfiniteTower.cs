using System;
using System.Collections;
using Cysharp.Threading.Tasks;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.TableData;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Blockchain;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Battle;
using Nekoyume.Model.BattleStatus;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.InfiniteTower;
using Nekoyume.State;
using Nekoyume.UI.Scroller;
using Serilog.Events;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using Helper;
    using Model;
    using UniRx;
    using UnityEngine.UI;

    public class WorldMapInfiniteTower : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI[] remainingBlockIndexs;
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private WorldButton worldButton;
        [SerializeField] private Transform towerImageParent;
        [SerializeField] private GameObject[] unActivateObjs;
        [SerializeField] private GameObject loadingRewardIndicator;
        [SerializeField] private Button prevSeasonPopupBtn;

        private readonly List<System.IDisposable> _disposables = new();
        private long _remainingBlockIndex = 0;

        private int _towerId;
        private GameObject _towerImage;

        private void Awake()
        {
            // prevSeasonPopupBtn.onClick.AddListener(() =>
            // {
            //     Widget.Find<PreviousSeasonReportPopup>().Show(GetCurrentTowerSeason()).Forget();
            // });

            worldButton.OnClickSubject.Subscribe(button =>
            {
                if (Game.LiveAsset.GameConfig.IsKoreanBuild)
                {
                    OneLineSystem.Push(MailType.System, L10nManager.Localize("UI_INFINITETOWER_ENTER_KOREAN_BUILD"), NotificationCell.NotificationType.Alert);
                    return;
                }

                var curState = GetInfiniteTowerState();

                // 비활성화 상태에서는 진입 불가
                if (curState == InfiniteTowerState.None || curState == InfiniteTowerState.End)
                {
                    var scheduleInfo = GetCurrentScheduleInfo();
                    if (scheduleInfo != null && curState == InfiniteTowerState.End)
                    {
                        var nextScheduleInfo = GetNextScheduleInfo();
                        if (nextScheduleInfo != null)
                        {
                            var remainBlock = nextScheduleInfo.StartBlockIndex - Game.Game.instance.Agent.BlockIndex;
                            var nextStartTime = string.Empty;
                            try
                            {
                                nextStartTime = nextScheduleInfo.StartBlockIndex.BlockIndexToDateTimeStringHour(Game.Game.instance.Agent.BlockIndex);
                            }
                            catch (System.Exception e)
                            {
                                NcDebug.LogError(e);
                            }

                            OneLineSystem.Push(
                                MailType.System,
                                L10nManager.Localize("UI_INFINITETOWER_SEASON_ENDED", remainBlock, remainBlock.BlockRangeToTimeSpanString(), nextStartTime),
                                NotificationCell.NotificationType.Alert);
                        }
                        else
                        {
                            OneLineSystem.Push(MailType.System, L10nManager.Localize("UI_INFINITETOWER_SEASON_NOT_AVAILABLE"), NotificationCell.NotificationType.Alert);
                        }
                    }
                    else
                    {
                        OneLineSystem.Push(MailType.System, L10nManager.Localize("UI_INFINITETOWER_NOT_AVAILABLE"), NotificationCell.NotificationType.Alert);
                    }
                    return;
                }

                // 무한의탑 정보 위젯 표시
                OnClickOpenInfiniteTowerInfo();
            }).AddTo(gameObject);
        }

        private void OnEnable()
        {
            // 초기 상태 설정
            UpdateInfiniteTowerState();

            Game.Game.instance.Agent.BlockIndexSubject
                .StartWith(Game.Game.instance.Agent.BlockIndex)
                .Subscribe(_ => UpdateViewAsync(Game.Game.instance.Agent.BlockIndex))
                .AddTo(_disposables);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        public void SetLoadingIndicator(bool isActive)
        {
            loadingIndicator.SetActive(isActive);
        }

        private void UpdateViewAsync(long blockIndex)
        {
            UpdateInfiniteTowerState();

            var scheduleInfo = GetCurrentScheduleInfo();
            if (scheduleInfo == null)
            {
                SetDefaultRemainingBlockIndexs();
                return;
            }

            var state = GetInfiniteTowerState();
            if (state == InfiniteTowerState.Ready)
            {
                SetDefaultRemainingBlockIndexs();
                return;
            }

            if (state == InfiniteTowerState.End)
            {
                // 다음 시즌 시작 블록 인덱스 표시
                var nextScheduleInfo = GetNextScheduleInfo();
                if (nextScheduleInfo != null)
                {
                    RefreshBlockIndexText(blockIndex, nextScheduleInfo.StartBlockIndex);
                }
                else
                {
                    SetDefaultRemainingBlockIndexs();
                }
                return;
            }

            if (state == InfiniteTowerState.Progress)
            {
                RefreshBlockIndexText(blockIndex, scheduleInfo.EndBlockIndex);
                return;
            }

            SetDefaultRemainingBlockIndexs();
        }

        private void UpdateInfiniteTowerState()
        {
            var state = GetInfiniteTowerState();
            OnInfiniteTowerStateChanged(state);
        }

        private void RefreshBlockIndexText(long blockIndex, long targetBlock)
        {
            _remainingBlockIndex = targetBlock - blockIndex;
            var timeText = "(-)";
            if (_remainingBlockIndex >= 0)
            {
                timeText = $"{_remainingBlockIndex:#,0}({_remainingBlockIndex.BlockRangeToTimeSpanString()})";
            }
            else
            {
                NcDebug.LogError($"RemainingBlockIndex is negative blockIndex: {blockIndex}, targetBlock: {targetBlock}");
            }

            foreach (var text in remainingBlockIndexs)
            {
                text.text = timeText;
            }
        }

        private void SetDefaultRemainingBlockIndexs()
        {
            foreach (var text in remainingBlockIndexs)
            {
                text.text = "(-)";
            }
        }

        public void OnClickOpenInfiniteTowerInfo()
        {
            // 무한의탑 위젯 비동기 표시
            var infiniteTowerWidget = Widget.Find<InfiniteTower>();
            if (infiniteTowerWidget != null)
            {
                infiniteTowerWidget.ShowAsync().Forget();
                NcDebug.Log("[WorldMapInfiniteTower] InfiniteTower widget shown");
            }
            else
            {
                NcDebug.LogError("[WorldMapInfiniteTower] InfiniteTower widget not found!");
            }
        }

        public void OnClickOpenEnterTowerPopup()
        {
            // Widget.Find<InfiniteTowerEnterPopup>().Show();
        }

        public void OnClickOpenInfiniteTower()
        {
            Widget.Find<LoadingScreen>().Show(LoadingScreen.LoadingType.AdventureBoss);
            try
            {
            }
            catch (System.Exception e)
            {
                NcDebug.LogError(e);
                Widget.Find<LoadingScreen>().Close();
            }
        }

        public void OnClickInfiniteTowerAlert()
        {
            var remainingTimespan = _remainingBlockIndex.BlockToTimeSpan();
            OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_INFINITE_TOWER_REMAINING_TIME", remainingTimespan.Hours, remainingTimespan.Minutes % 60), NotificationCell.NotificationType.Notification);
        }

        private void OnInfiniteTowerStateChanged(InfiniteTowerState state)
        {
            if (Game.LiveAsset.GameConfig.IsKoreanBuild)
            {
                worldButton.IsLockNameShow = false;
                worldButton.HasNotification.Value = false;
                SetDefaultRemainingBlockIndexs();
                if (_towerImage != null)
                {
                    DestroyImmediate(_towerImage);
                }

                worldButton.Lock(true);

                foreach (var obj in unActivateObjs)
                {
                    obj.SetActive(false);
                }
                prevSeasonPopupBtn.gameObject.SetActive(false);
                return;
            }

            switch (state)
            {
                case InfiniteTowerState.Ready:
                    worldButton.Unlock();
                    worldButton.HasNotification.Value = true;
                    worldButton.IsLockNameShow = false;
                    if (_towerImage != null)
                    {
                        DestroyImmediate(_towerImage);
                    }
                    prevSeasonPopupBtn.gameObject.SetActive(true);
                    _towerId = 0;
                    break;
                case InfiniteTowerState.Progress:
                    worldButton.Unlock();
                    worldButton.HasNotification.Value = true;
                    worldButton.IsLockNameShow = false;
                    prevSeasonPopupBtn.gameObject.SetActive(false);
                    break;
                case InfiniteTowerState.None:
                case InfiniteTowerState.End:
                default:
                    worldButton.HasNotification.Value = false;
                    worldButton.Lock(true);
                    SetDefaultRemainingBlockIndexs();
                    if (_towerImage != null)
                    {
                        DestroyImmediate(_towerImage);
                    }
                    prevSeasonPopupBtn.gameObject.SetActive(true);
                    _towerId = 0;
                    break;
            }

            // 비활성화 오브젝트들 상태 업데이트
            foreach (var obj in unActivateObjs)
            {
                obj.SetActive(state == InfiniteTowerState.None || state == InfiniteTowerState.End);
            }
        }

        private InfiniteTowerState GetInfiniteTowerState()
        {
            var scheduleInfo = GetCurrentScheduleInfo();
            if (scheduleInfo == null)
            {
                return InfiniteTowerState.None;
            }

            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;

            // 종료 여부를 먼저 체크
            if (scheduleInfo.HasEnded(currentBlockIndex))
            {
                return InfiniteTowerState.End;
            }

            if (scheduleInfo.IsActive(currentBlockIndex))
            {
                return InfiniteTowerState.Progress;
            }

            if (scheduleInfo.HasStarted(currentBlockIndex))
            {
                // HasStarted이지만 아직 활성화되지 않은 경우 (Ready 상태는 거의 없을 것)
                return InfiniteTowerState.Ready;
            }

            return InfiniteTowerState.Ready;
        }

        private InfiniteTowerScheduleSheet.Row GetCurrentScheduleInfo()
        {
            var tableSheets = Game.Game.instance.TableSheets;
            if (tableSheets?.InfiniteTowerScheduleSheet == null)
            {
                return null;
            }

            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;

            // 먼저 활성화된 시즌을 찾음
            foreach (var schedule in tableSheets.InfiniteTowerScheduleSheet.Values)
            {
                if (schedule.IsActive(currentBlockIndex))
                {
                    return schedule;
                }
            }

            // 활성화된 시즌이 없으면 시작된 시즌을 찾음 (종료된 시즌 포함)
            foreach (var schedule in tableSheets.InfiniteTowerScheduleSheet.Values)
            {
                if (schedule.HasStarted(currentBlockIndex))
                {
                    return schedule;
                }
            }

            return null;
        }

        private InfiniteTowerScheduleSheet.Row GetNextScheduleInfo()
        {
            var tableSheets = Game.Game.instance.TableSheets;
            if (tableSheets?.InfiniteTowerScheduleSheet == null)
            {
                return null;
            }

            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;

            // 아직 시작되지 않은 가장 가까운 시즌을 찾음
            return tableSheets.InfiniteTowerScheduleSheet.Values
                .Where(schedule => schedule.StartBlockIndex > currentBlockIndex)
                .OrderBy(schedule => schedule.StartBlockIndex)
                .FirstOrDefault();
        }

        private int GetCurrentTowerSeason()
        {
            var scheduleInfo = GetCurrentScheduleInfo();
            return scheduleInfo?.Id ?? 0;
        }

        private static async UniTask RefreshInfiniteTowerData()
        {
            // TODO: Implement infinite tower data refresh logic
            await UniTask.Delay(1000); // Placeholder
        }

        public enum InfiniteTowerState
        {
            None,
            Ready,
            Progress,
            End
        }
    }
}
