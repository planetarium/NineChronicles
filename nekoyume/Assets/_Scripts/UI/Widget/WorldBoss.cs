using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Libplanet;
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
        private ConditionalButton enterButton;

        [SerializeField]
        private GameObject offSeasonContainer;

        [SerializeField]
        private GameObject seasonContainer;

        [SerializeField]
        private Transform bossNameContainer;

        [SerializeField]
        private Transform bossSpineContainer;

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
        private int _bossId;
        private (long, long) _period;

        private WorldBossStatus _status = WorldBossStatus.None;
        private HeaderMenuStatic _headerMenu;
        private readonly List<IDisposable> _disposables = new();

        protected override void Awake()
        {
            base.Awake();

            CloseWidget = () =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            };

            backButton.OnClickAsObservable().Subscribe(_ =>
            {
                Close(true);
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
            refreshButton.OnClickAsObservable()
                .Subscribe(_ => RefreshMyInformationAsync()).AddTo(gameObject);

            enterButton.OnSubmitSubject.Subscribe(_ => OnClickEnter()).AddTo(gameObject);
            WorldBossStates.SubscribeNotification((b) => notification.SetActive(b));
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
            var loading = Find<DataLoadingScreen>();
            loading.Show();
            await UpdateViewAsync(Game.Game.instance.Agent.BlockIndex, forceUpdate: true);
            loading.Close();
            base.Show(ignoreShowAnimation);
        }

        private void ShowHeaderMenu(
            HeaderMenuStatic.AssetVisibleState assetVisibleState,
            bool showHeaderMenuAnimation)
        {
            _headerMenu = Find<HeaderMenuStatic>();
            _headerMenu.Show(assetVisibleState, showHeaderMenuAnimation);
        }

        private async Task UpdateViewAsync(long currentBlockIndex
            , bool forceUpdate = false
            , bool ignoreHeaderMenuAnimation = false
            , bool ignoreHeaderMenu = false)
        {
            if (forceUpdate)
            {
                _status = WorldBossStatus.None;
            }

            var curStatus = WorldBossFrontHelper.GetStatus(currentBlockIndex);
            if (_status != curStatus)
            {
                _status = curStatus;
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
                        var (worldBoss, raider, myRecord, userCount) = await GetStatesAsync(row);
                        WorldBossStates.UpdateRaiderState(States.Instance.CurrentAvatarState.address, raider);
                        UpdateSeason(row, worldBoss, myRecord, userCount);

                        break;
                    case WorldBossStatus.None:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var raiderState = WorldBossStates.GetRaiderState(avatarAddress);
            _headerMenu.WorldBossTickets.UpdateTicket(raiderState, currentBlockIndex);
            UpdateRemainTimer(_period, currentBlockIndex);
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

            UpdateBossPrefab(nextRow, true);
        }

        private void UpdateSeason(
            WorldBossListSheet.Row row,
            WorldBossState worldBoss,
            WorldBossRankingRecord myRecord,
            int userCount)
        {
            Debug.Log("[UpdateSeason]");

            offSeasonContainer.SetActive(false);
            seasonContainer.SetActive(true);
            rankButton.gameObject.SetActive(true);
            enterButton.Text = L10nManager.Localize("UI_WORLD_MAP_ENTER");
            _period = (row.StartedBlockIndex, row.EndedBlockIndex);
            UpdateBossPrefab(row);
            UpdateBossInformationAsync(worldBoss);
            season.UpdateMyInformation(myRecord);
            season.UpdateUserCount(userCount);
        }

        private void UpdateBossPrefab(WorldBossListSheet.Row row, bool isOffSeason = false)
        {
            if (_bossNamePrefab != null)
            {
                Destroy(_bossNamePrefab);
            }

            if (_bossSpinePrefab != null)
            {
                Destroy(_bossSpinePrefab);
            }

            if (WorldBossFrontHelper.TryGetBossData(row.BossId, out var data))
            {
                if (isOffSeason)
                {
                    _bossNamePrefab = Instantiate(data.namePrefab, bossNameContainer);
                }

                _bossSpinePrefab = Instantiate(data.spinePrefab, bossSpineContainer);
                _bossId = row.BossId;
            }
        }

        private void UpdateRemainTimer((long, long) time, long current)
        {
            var (begin, end) = time;
            var range = end - begin;
            var progress = current - begin;
            timerSlider.NormalizedValue = 1f - ((float)progress / range);
            timeBlock.SetTimeBlock($"{end - current:#,0}", Util.GetBlockToTime(end - current));
            blocksAndDatesPeriod.Show(begin, end, current, DateTime.Now);
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
                WorldBossRankingRecord myRecord,
                int userCount)>
            GetStatesAsync(WorldBossListSheet.Row row)
        {
            var task = Task.Run(async () =>
            {
                var worldBossAddress = Addresses.GetWorldBossAddress(row.Id);
                var worldBossState = await Game.Game.instance.Agent.GetStateAsync(worldBossAddress);
                var worldBoss = worldBossState is Bencodex.Types.List worldBossList
                    ? new WorldBossState(worldBossList)
                    : null;


                var avatarAddress = States.Instance.CurrentAvatarState.address;
                var raiderAddress = Addresses.GetRaiderAddress(avatarAddress, row.Id);
                var raiderState = await Game.Game.instance.Agent.GetStateAsync(raiderAddress);
                var raider = raiderState is Bencodex.Types.List raiderList
                    ? new RaiderState(raiderList)
                    : null;

                var (record, userCount) = await QueryRankingAsync(row, avatarAddress);
                return (worldBoss, raider, record, userCount);
            });

            await task;
            return task.Result;
        }

        private static async Task<(WorldBossRankingRecord, int)> QueryRankingAsync(
            WorldBossListSheet.Row row,
            Address avatarAddress)
        {
            var response = await WorldBossQuery.QueryRankingAsync(row.Id, avatarAddress);
            var records = response?.WorldBossRanking ?? new List<WorldBossRankingRecord>();
            var myRecord =
                records.FirstOrDefault(record => record.Address == avatarAddress.ToHex());
            var userCount = response?.WorldBossTotalUsers ?? 0;
            return (myRecord, userCount);
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
            var bossName = WorldBossFrontHelper.TryGetBossData(bossId, out var data)
                ? data.name
                : string.Empty;

            season.UpdateBossInformation(bossId, bossName, level, curHp, maxHp);
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
            foreach (var o in queryLoadingObjects)
            {
                o.SetActive(value);
            }
        }
    }
}
