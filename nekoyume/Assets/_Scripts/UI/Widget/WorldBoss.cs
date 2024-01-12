using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Nekoyume.Game.Controller;
using Nekoyume.Game.LiveAsset;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Common;
using Nekoyume.UI.Module.WorldBoss;
using Nekoyume.ValueControlComponents.Shader;
using UnityEngine;
using UnityEngine.UI;
using WorldBossState = Nekoyume.Model.State.WorldBossState;

namespace Nekoyume.UI
{
    using TMPro;
    using UniRx;

    public class WorldBoss : Widget
    {
        [SerializeField]
        private WorldBossSeason season;

        [SerializeField]
        private Button backButton;

        [SerializeField]
        public Button rewardButton;

        [SerializeField]
        public Button rankButton;

        [SerializeField]
        public Button informationButton;

        [SerializeField]
        public Button prevRankButton;

        [SerializeField]
        public Button runeButton;

        [SerializeField]
        private Button refreshButton;

        [SerializeField]
        private GameObject refreshBlocker;

        [SerializeField]
        private ConditionalButton enterButton;

        [SerializeField]
        private TextMeshProUGUI titleText;

        [SerializeField]
        private GameObject offSeasonContainer;

        [SerializeField]
        private GameObject seasonContainer;

        [SerializeField]
        private Transform bossNameContainer;

        [SerializeField]
        private Transform bossSpineContainer;

        [SerializeField]
        private Transform backgroundContainer;

        [SerializeField]
        private ShaderPropertySlider timerSlider;

        [SerializeField]
        private TimeBlock timeBlock;

        [SerializeField]
        private GameObject notification;

        [SerializeField]
        private BlocksAndDatesPeriod blocksAndDatesPeriod;

        [SerializeField]
        private List<GameObject> queryLoadingObjects;

        private GameObject _bossNamePrefab;
        private GameObject _bossSpinePrefab;
        private GameObject _backgroundPrefab;
        private int _bossId;
        private (long, long) _period;
        private string _bgmName;

        private WorldBossStatus _status = WorldBossStatus.None;
        private HeaderMenuStatic _headerMenu;
        private readonly List<IDisposable> _disposables = new();

        protected override void Awake()
        {
            base.Awake();

            CloseWidget = () =>
            {
                ForceClose(true);
                Game.Event.OnRoomEnter.Invoke(true);
            };

            backButton.OnClickAsObservable().Subscribe(_ =>
            {
                ForceClose(true);
                Game.Event.OnRoomEnter.Invoke(true);
            }).AddTo(gameObject);

            rewardButton.OnClickAsObservable()
                .Subscribe(_ => ShowDetail(WorldBossDetail.ToggleType.Reward)).AddTo(gameObject);
            rankButton.OnClickAsObservable()
                .Subscribe(_ => ShowDetail(WorldBossDetail.ToggleType.Rank)).AddTo(gameObject);
            informationButton.OnClickAsObservable()
                .Subscribe(_ => ShowDetail(WorldBossDetail.ToggleType.Information))
                .AddTo(gameObject);
            prevRankButton.OnClickAsObservable()
                .Subscribe(_ => ShowDetail(WorldBossDetail.ToggleType.PreviousRank))
                .AddTo(gameObject);
            runeButton.OnClickAsObservable()
                .Subscribe(_ => ShowDetail(WorldBossDetail.ToggleType.Rune)).AddTo(gameObject);
            refreshButton
                .OnClickAsObservable()
                .Where(_=> !refreshBlocker.activeSelf)
                .Subscribe(_ => RefreshMyInformationAsync()).AddTo(gameObject);

            enterButton.OnSubmitSubject.Subscribe(_ => OnClickEnter()).AddTo(gameObject);
            WorldBossStates.SubscribeGradeRewards((b) => notification.SetActive(b));
        }

        protected override void OnCompleteOfShowAnimationInternal()
        {
            base.OnCompleteOfShowAnimationInternal();
            Find<Status>().Close(true);
        }

        protected override void OnEnable()
        {
            Game.Game.instance.Agent.BlockIndexSubject
                .Subscribe(x => UpdateViewAsync(x))
                .AddTo(_disposables);
        }

        protected override void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        public async UniTaskVoid ShowAsync(bool ignoreShowAnimation = false)
        {
            var loading = Find<LoadingScreen>();
            loading.Show(LoadingScreen.LoadingType.WorldBoss);
            await UpdateViewAsync(Game.Game.instance.Agent.BlockIndex, forceUpdate: true);
            loading.Close();
            AudioController.instance.PlayMusic(_bgmName);
            base.Show(ignoreShowAnimation);
        }

        private void ShowHeaderMenu(
            HeaderMenuStatic.AssetVisibleState assetVisibleState,
            bool showHeaderMenuAnimation)
        {
            _headerMenu = Find<HeaderMenuStatic>();
            _headerMenu.Show(assetVisibleState, showHeaderMenuAnimation);
        }

        private async Task UpdateViewAsync(long currentBlockIndex,
            bool forceUpdate = false,
            bool ignoreHeaderMenuAnimation = false,
            bool ignoreHeaderMenu = false)
        {
            if (forceUpdate)
            {
                _status = WorldBossStatus.None;
            }

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var curStatus = WorldBossFrontHelper.GetStatus(currentBlockIndex);
            if (_status != curStatus)
            {
                _status = curStatus;
                if (Find<WorldBossDetail>().isActiveAndEnabled)
                {
                    Find<WorldBossDetail>().Close();
                }

                switch (_status)
                {
                    case WorldBossStatus.OffSeason:
                        if (!ignoreHeaderMenu)
                        {
                            ShowHeaderMenu(HeaderMenuStatic.AssetVisibleState.CurrencyOnly,
                                ignoreHeaderMenuAnimation);
                        }

                        WorldBossStates.ClearRaiderState();
                        UpdateOffSeason(currentBlockIndex);
                        break;
                    case WorldBossStatus.Season:
                        if (!ignoreHeaderMenu)
                        {
                            ShowHeaderMenu(HeaderMenuStatic.AssetVisibleState.WorldBoss,
                                ignoreHeaderMenuAnimation);
                        }

                        if (!WorldBossFrontHelper.TryGetCurrentRow(currentBlockIndex, out var row))
                        {
                            return;
                        }

                        season.PrepareRefresh();
                        var (worldBoss, raider, killReward, myRecord, blockIndex, userCount)
                            = await GetStatesAsync(row);
                        WorldBossStates.UpdateState(avatarAddress, raider, killReward);
                        UpdateSeason(row, worldBoss, myRecord, blockIndex, userCount);

                        break;
                    case WorldBossStatus.None:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var raiderState = WorldBossStates.GetRaiderState(avatarAddress);
            var refillInterval = States.Instance.GameConfigState.DailyWorldBossInterval;
            _headerMenu.WorldBossTickets.UpdateTicket(raiderState, currentBlockIndex, refillInterval);
            var secondsPerBlock = LiveAssetManager.instance.GameConfig.SecondsPerBlock;
            UpdateRemainTimer(_period, currentBlockIndex, secondsPerBlock);
            SetActiveQueryLoading(false);
        }

        private void UpdateOffSeason(long currentBlockIndex)
        {
            if (!WorldBossFrontHelper.TryGetNextRow(currentBlockIndex, out var nextRow))
            {
                Close();
                Game.Event.OnRoomEnter.Invoke(true);
                return;
            }

            offSeasonContainer.SetActive(true);
            seasonContainer.SetActive(false);
            rankButton.gameObject.SetActive(false);
            enterButton.Text = L10nManager.Localize("UI_PRACTICE");
            var begin =
                WorldBossFrontHelper.TryGetPreviousRow(currentBlockIndex, out var previousRow)
                    ? previousRow.EndedBlockIndex
                    : 0;
            _period = (begin, nextRow.StartedBlockIndex);
            UpdateBossName(nextRow);
            UpdateBossPrefab(nextRow, true);
        }

        private void UpdateSeason(
            WorldBossListSheet.Row row,
            WorldBossState worldBoss,
            WorldBossRankingRecord myRecord,
            long blockIndex,
            int userCount)
        {
            offSeasonContainer.SetActive(false);
            seasonContainer.SetActive(true);
            rankButton.gameObject.SetActive(true);
            enterButton.Text = L10nManager.Localize("UI_WORLD_MAP_ENTER");
            _period = (row.StartedBlockIndex, row.EndedBlockIndex);
            UpdateBossName(row);
            UpdateBossPrefab(row);
            UpdateBossInformationAsync(worldBoss);
            season.UpdateMyInformation(row.BossId, myRecord, blockIndex);
            season.UpdateUserCount(userCount);
        }

        private void UpdateBossName(WorldBossListSheet.Row nextRow)
        {
            titleText.text = L10nManager.LocalizeCharacterName(nextRow.BossId);
        }

        private void UpdateBossPrefab(WorldBossListSheet.Row row, bool isOffSeason = false)
        {
            if (WorldBossFrontHelper.TryGetBossData(row.BossId, out var data))
            {
                if(_bossId == row.BossId)
                {
                    return;
                }

                if (_bossNamePrefab != null)
                {
                    Destroy(_bossNamePrefab);
                }

                if (_bossSpinePrefab != null)
                {
                    Destroy(_bossSpinePrefab);
                }

                if (_backgroundPrefab != null)
                {
                    Destroy(_backgroundPrefab);
                }

                if (isOffSeason)
                {
                    _bossNamePrefab = Instantiate(data.namePrefab, bossNameContainer);
                }

                _bossSpinePrefab = Instantiate(data.spinePrefab, bossSpineContainer);
                _backgroundPrefab = Instantiate(data.backgroundPrefab, backgroundContainer);
                _bossId = row.BossId;

                if (string.IsNullOrWhiteSpace(_bgmName))
                {
                    _bgmName = data.entranceMusicName;
                }
            }
        }

        private void UpdateRemainTimer((long, long) time, long current, int secondsPerBlock)
        {
            var (begin, end) = time;
            var range = end - begin;
            var progress = current - begin;
            var remaining = end - current;
            timerSlider.NormalizedValue = 1f - ((float)progress / range);
            timeBlock.SetTimeBlock($"{remaining:#,0}", remaining.BlockRangeToTimeSpanString());
            blocksAndDatesPeriod.Show(begin, end, current, secondsPerBlock, DateTime.Now);
        }

        private void ShowDetail(WorldBossDetail.ToggleType toggleType)
        {
            _headerMenu.Close(true);
            Find<WorldBossDetail>().Show(toggleType);
        }

        private void OnClickEnter()
        {
            Find<RaidPreparation>().Show(_bossId);
        }

        private async Task<(
            WorldBossState worldBoss,
            RaiderState raiderState,
            WorldBossKillRewardRecord killReward,
            WorldBossRankingRecord myRecord,
            long blockIndex,
            int userCount)>
            GetStatesAsync(WorldBossListSheet.Row row)
        {
            var task = Task.Run(async () =>
            {
                var worldBossAddress = Addresses.GetWorldBossAddress(row.Id);
                var worldBossState = await Game.Game.instance.Agent.GetStateAsync(
                    ReservedAddresses.LegacyAccount,
                    worldBossAddress);
                var worldBoss = worldBossState is Bencodex.Types.List worldBossList
                    ? new WorldBossState(worldBossList)
                    : null;

                var avatarAddress = States.Instance.CurrentAvatarState.address;
                var raiderAddress = Addresses.GetRaiderAddress(avatarAddress, row.Id);
                var raiderState = await Game.Game.instance.Agent.GetStateAsync(
                    ReservedAddresses.LegacyAccount,
                    raiderAddress);
                var raider = raiderState is Bencodex.Types.List raiderList
                    ? new RaiderState(raiderList)
                    : null;

                var killRewardAddress = Addresses.GetWorldBossKillRewardRecordAddress(avatarAddress, row.Id);
                var killRewardState = await Game.Game.instance.Agent.GetStateAsync(
                    ReservedAddresses.LegacyAccount,
                    killRewardAddress);
                var killReward = killRewardState is Bencodex.Types.List killRewardList
                    ? new WorldBossKillRewardRecord(killRewardList)
                    : null;

                var (record, blockIndex, userCount) = await QueryRankingAsync(row, avatarAddress);
                return (worldBoss, raider, killReward, record, blockIndex, userCount);
            });

            await task;
            return task.Result;
        }

        private static async Task<(
            WorldBossRankingRecord myRecord,
            long blockIndex,
            int userCount)>
            QueryRankingAsync(WorldBossListSheet.Row row, Address avatarAddress)
        {
            var response = await WorldBossQuery.QueryRankingAsync(row.Id, avatarAddress);
            var records = response?.WorldBossRanking?.RankingInfo ?? new List<WorldBossRankingRecord>();
            var myRecord =
                records.FirstOrDefault(record => record.Address == avatarAddress.ToHex());
            var userCount = response?.WorldBossTotalUsers ?? 0;
            var blockIndex = response?.WorldBossRanking?.BlockIndex ?? 0;
            return (myRecord, blockIndex, userCount);
        }

        private void UpdateBossInformationAsync(WorldBossState state)
        {
            var level = state?.Level ?? 1;
            var hpSheet = Game.Game.instance.TableSheets.WorldBossGlobalHpSheet;
            var bossSheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            var baseHp = hpSheet.Values.FirstOrDefault(x => x.Level == 1)!.Hp;
            var curHp = state?.CurrentHp ?? baseHp;
            var maxHp = hpSheet.Values.FirstOrDefault(x => x.Level == level)!.Hp;
            var bossId = state?.Id ?? bossSheet.Values.First().BossId;
            season.UpdateBossInformation(bossId, level, curHp, maxHp);
        }

        private async void RefreshMyInformationAsync()
        {
            SetActiveQueryLoading(true);
            await UpdateViewAsync(Game.Game.instance.Agent.BlockIndex,
                forceUpdate: true,
                ignoreHeaderMenuAnimation: true);
        }

        private void SetActiveQueryLoading(bool value)
        {
            refreshBlocker.SetActive(value);
            foreach (var o in queryLoadingObjects)
            {
                o.SetActive(value);
            }
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionClickWorldBossSeasonRewardsButton()
        {
            ShowDetail(WorldBossDetail.ToggleType.Reward);
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionClickWorldBossEnterPracticeButton()
        {
            OnClickEnter();
        }
    }
}
