using Cysharp.Threading.Tasks;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Scroller;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using Helper;
    using Model;
    using UniRx;
    using UnityEngine.UI;

    public class WorldMapAdventureBoss : MonoBehaviour
    {
        [SerializeField] private GameObject open;
        [SerializeField] private GameObject wantedClose;
        [SerializeField] private TextMeshProUGUI[] remainingBlockIndexs;
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private WorldButton worldButton;
        [SerializeField] private Transform bossImageParent;
        [SerializeField] private GameObject[] unActivateObjs;
        [SerializeField] private GameObject loadingRewardIndicator;
        [SerializeField] private Button prevSeasonPopupBtn;

        private readonly List<System.IDisposable> _disposables = new();
        private long _remainingBlockIndex = 0;

        private int _bossId;
        private GameObject _bossImage;

        private void Awake()
        {
            prevSeasonPopupBtn.onClick.AddListener(() =>
            {
                Widget.Find<PreviousSeasonReportPopup>().Show(Game.Game.instance.AdventureBossData.SeasonInfo.Value.Season).Forget();
            });

            worldButton.OnClickSubject.Subscribe(button =>
            {
                if (Game.LiveAsset.GameConfig.IsKoreanBuild)
                {
                    OneLineSystem.Push(MailType.System, L10nManager.Localize("UI_ADVENTUREBOSS_ENTER_KOREAN_BUILD"), NotificationCell.NotificationType.Alert);
                    return;
                }

                var curState = Game.Game.instance.AdventureBossData.CurrentState.Value;
                if (curState == AdventureBossData.AdventureBossSeasonState.Ready)
                {
                    OnClickOpenEnterBountyPopup();
                    return;
                }

                if (curState == AdventureBossData.AdventureBossSeasonState.Progress)
                {
                    OnClickOpenAdventureBoss();
                    return;
                }

                if (curState == AdventureBossData.AdventureBossSeasonState.End)
                {
                    var adventureBossData = Game.Game.instance.AdventureBossData;
                    long remainBlock = 0;
                    if (adventureBossData.EndedSeasonInfos.TryGetValue(adventureBossData.SeasonInfo.Value.Season, out var endedSeasonInfo))
                    {
                        remainBlock = endedSeasonInfo.NextStartBlockIndex - Game.Game.instance.Agent.BlockIndex;
                    }

                    var NextStartTime = string.Empty;
                    try
                    {
                        NextStartTime = endedSeasonInfo.NextStartBlockIndex.BlockIndexToDateTimeStringHour(Game.Game.instance.Agent.BlockIndex);
                    }
                    catch (System.Exception e)
                    {
                        NcDebug.LogError(e);
                    }

                    OneLineSystem.Push(MailType.System, L10nManager.Localize("UI_ADVENTUREBOSS_SEASON_ENDED", remainBlock, remainBlock.BlockRangeToTimeSpanString(), NextStartTime), NotificationCell.NotificationType.Alert);
                    return;
                }
            }).AddTo(gameObject);
        }

        private void OnEnable()
        {
            Game.Game.instance.AdventureBossData.CurrentState.Subscribe(OnAdventureBossStateChanged).AddTo(_disposables);

            Game.Game.instance.Agent.BlockIndexSubject
                .StartWith(Game.Game.instance.Agent.BlockIndex)
                .Subscribe(UpdateViewAsync)
                .AddTo(_disposables);

            Game.Game.instance.AdventureBossData.IsRewardLoading.Subscribe(isLoading => { loadingRewardIndicator.SetActive(isLoading); }).AddTo(_disposables);
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
            var seasonInfo = Game.Game.instance.AdventureBossData.SeasonInfo.Value;
            if (seasonInfo == null)
            {
                NcDebug.LogWarning("SeasonInfo is null");
                return;
            }

            if (Game.Game.instance.AdventureBossData.CurrentState.Value == AdventureBossData.AdventureBossSeasonState.Ready)
            {
                SetDefualtRemainingBlockIndexs();
                return;
            }

            if (Game.Game.instance.AdventureBossData.CurrentState.Value == AdventureBossData.AdventureBossSeasonState.End)
            {
                var adventureBossData = Game.Game.instance.AdventureBossData;
                if (adventureBossData.EndedSeasonInfos.TryGetValue(adventureBossData.SeasonInfo.Value.Season, out var endedSeasonInfo))
                {
                    RefreshBlockIndexText(blockIndex, endedSeasonInfo.NextStartBlockIndex);
                    return;
                }
            }

            RefreshBlockIndexText(blockIndex, seasonInfo.EndBlockIndex);
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

        private void SetDefualtRemainingBlockIndexs()
        {
            foreach (var text in remainingBlockIndexs)
            {
                text.text = "(-)";
            }
        }

        public void OnClickOpenEnterBountyPopup()
        {
            Widget.Find<AdventureBossEnterBountyPopup>().Show();
        }

        public static void OnClickOpenAdventureBoss()
        {
            Widget.Find<LoadingScreen>().Show(LoadingScreen.LoadingType.AdventureBoss);
            try
            {
                Game.Game.instance.AdventureBossData.RefreshAllByCurrentState().ContinueWith(() =>
                {
                    Widget.Find<LoadingScreen>().Close();
                    Widget.Find<AdventureBoss>().Show();
                });
            }
            catch (System.Exception e)
            {
                NcDebug.LogError(e);
                Widget.Find<LoadingScreen>().Close();
            }
        }

        public void OnClickAdventureSeasonAlert()
        {
            var remaingTimespan = _remainingBlockIndex.BlockToTimeSpan();
            OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_ADVENTURE_BOSS_REMAINIG_TIME", remaingTimespan.Hours, remaingTimespan.Minutes % 60), NotificationCell.NotificationType.Notification);
        }

        private void OnAdventureBossStateChanged(AdventureBossData.AdventureBossSeasonState state)
        {
            if (Game.LiveAsset.GameConfig.IsKoreanBuild)
            {
                worldButton.IsLockNameShow = false;
                worldButton.HasNotification.Value = false;
                open.SetActive(false);
                SetDefualtRemainingBlockIndexs();
                if (_bossImage != null)
                {
                    DestroyImmediate(_bossImage);
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
                case AdventureBossData.AdventureBossSeasonState.Ready:
                    worldButton.Unlock();
                    open.SetActive(true);
                    wantedClose.SetActive(true);
                    worldButton.HasNotification.Value = true;
                    if (_bossImage != null)
                    {
                        DestroyImmediate(_bossImage);
                    }
                    prevSeasonPopupBtn.gameObject.SetActive(true);
                    _bossId = 0;
                    break;
                case AdventureBossData.AdventureBossSeasonState.Progress:
                    worldButton.Unlock();
                    open.SetActive(true);
                    wantedClose.SetActive(false);
                    if (_bossId != Game.Game.instance.AdventureBossData.SeasonInfo.Value.BossId)
                    {
                        if (_bossImage != null)
                        {
                            DestroyImmediate(_bossImage);
                        }

                        _bossId = Game.Game.instance.AdventureBossData.SeasonInfo.Value.BossId;
                        _bossImage = Instantiate(SpriteHelper.GetBigCharacterIconFace(_bossId), bossImageParent);
                        _bossImage.transform.localPosition = Vector3.zero;
                    }
                    worldButton.HasNotification.Value = true;
                    prevSeasonPopupBtn.gameObject.SetActive(false);
                    break;
                case AdventureBossData.AdventureBossSeasonState.None:
                case AdventureBossData.AdventureBossSeasonState.End:
                default:
                    worldButton.HasNotification.Value = false;
                    worldButton.Unlock();
                    open.SetActive(false);
                    SetDefualtRemainingBlockIndexs();
                    if (_bossImage != null)
                    {
                        DestroyImmediate(_bossImage);
                    }
                    prevSeasonPopupBtn.gameObject.SetActive(true);
                    _bossId = 0;
                    break;
            }
        }
    }
}
