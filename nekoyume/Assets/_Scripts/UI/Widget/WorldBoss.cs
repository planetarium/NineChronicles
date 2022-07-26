﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Libplanet;
using Nekoyume.BlockChain;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using Nekoyume.ValueControlComponents.Shader;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using StateExtensions = Nekoyume.Model.State.StateExtensions;

namespace Nekoyume.UI
{
    using UniRx;

    public class WorldBoss : Widget
    {
        private enum Status
        {
            None,
            OffSeason,
            Season,
        }

        [SerializeField]
        private WorldBossSeason season;

        [SerializeField]
        private Button backButton;

        [SerializeField]
        public Button prevRankButton;

        [SerializeField]
        public Button rankButton;

        [SerializeField]
        public Button informationButton;

        [SerializeField]
        public Button rewardButton;

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

        private GameObject _bossNamePrefab;
        private GameObject _bossSpinePrefab;
        private (long, long) _period;
        private RaiderState _cachedRaiderState;
        private int _remainTicket;

        // for test
        [SerializeField]
        private TextMeshProUGUI ticketText;

        [SerializeField]
        private Button claimRaidRewardButton;

        [SerializeField]
        private Button viewButton;

        [SerializeField]
        private Button viewRuneButton;

        private Status _status = Status.None;
        private readonly List<IDisposable> _disposables = new();
        private HeaderMenuStatic _headerMenu;
        private List<WorldBossRankingRecord> _records = new List<WorldBossRankingRecord>();
        private WorldBossRankingRecord _myInfo;

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
                .Subscribe(_ => ShowDetail(WorldBossDetail.ToggleType.Information)).AddTo(gameObject);
            prevRankButton.OnClickAsObservable()
                .Subscribe(_ => ShowDetail(WorldBossDetail.ToggleType.PreviousRank)).AddTo(gameObject);

            enterButton.OnSubmitSubject.Subscribe(_ => OnClickEnter()).AddTo(gameObject);

            // claimRaidRewardButton.OnClickAsObservable().Subscribe(_ => ClaimRaidReward()).AddTo(gameObject);
            // viewButton.OnClickAsObservable().Subscribe(_ => View()).AddTo(gameObject);
            // viewRuneButton.OnClickAsObservable().Subscribe(_ =>
            // {
            //     ViewRune(800000);
            //     ViewRune(800001);
            // }).AddTo(gameObject);
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

            ShowSheetValues();
            ShowHeaderMenu();
            await UpdateViewAsync(Game.Game.instance.Agent.BlockIndex, true);

            loading.Close();
            base.Show(ignoreShowAnimation);
        }

        private void ShowHeaderMenu()
        {
            _headerMenu = Find<HeaderMenuStatic>();
            _headerMenu.Show(HeaderMenuStatic.AssetVisibleState.WorldBoss);
        }

        public async Task UpdateViewAsync(long currentBlockIndex, bool forceUpdate = false)
        {
            if (forceUpdate)
            {
                _status = Status.None;
            }

            var curStatus = GetStatus(currentBlockIndex);
            if (_status != curStatus)
            {
                _status = curStatus;
                switch (_status)
                {
                    case Status.OffSeason:
                        UpdateOffSeason(currentBlockIndex);
                        break;
                    case Status.Season:
                        if (!WorldBossFrontHelper.TryGetCurrentRow(currentBlockIndex, out var row))
                        {
                            return;
                        }

                        var (worldBoss, raider) = await GetStatesAsync(row);
                        var address = Game.Game.instance.States.CurrentAvatarState.address;
                        var response = await QueryRankingAsync(row.Id, address);
                        _records = response.WorldBossRanking;
                        _myInfo = _records.FirstOrDefault(r => r.Address == address.ToHex());
                        var userCount = response.WorldBossTotalUsers;
                        UpdateSeason(row, worldBoss, raider, userCount, currentBlockIndex, _myInfo?.Ranking);
                        break;
                    case Status.None:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            UpdateTicket(currentBlockIndex);
            UpdateRemainTimer(_period, currentBlockIndex);
        }

        private Status GetStatus(long currentBlockIndex)
        {
            return WorldBossFrontHelper.IsItInSeason(currentBlockIndex)
                ? Status.Season
                : Status.OffSeason;
        }

        private async Task<WorldBossRankingResponse> QueryRankingAsync(int raidId, Address address)
        {
            var query = @$"query {{
                worldBossTotalUsers(raidId: {raidId})
                worldBossRanking(raidId: {raidId}, avatarAddress: ""{address}"") {{
                    highScore
                    address
                    ranking
                    level
                    cp
                    iconId
                    avatarName
                    totalScore
                }}
            }}";
            var response = await Game.Game.instance.ApiClient.GetObjectAsync<WorldBossRankingResponse>(query);
            Debug.Log($"total raid users: {response.WorldBossTotalUsers}");
            Debug.Log($"find my info: {!(_myInfo is null)}");
            return response;
        }

        private void UpdateOffSeason(long currentBlockIndex)
        {
            if (!WorldBossFrontHelper.TryGetNextRow(currentBlockIndex, out var nextRow))
            {
                return;
            }

            offSeasonContainer.SetActive(false);
            seasonContainer.SetActive(true);
            rankButton.gameObject.SetActive(false);
            enterButton.Text = "Practice";

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
            RaiderState raider,
            int userCount,
            long currentBlockIndex,
            int? ranking)
        {
            Debug.Log("[UpdateSeason]");
            offSeasonContainer.SetActive(true);
            seasonContainer.SetActive(false);
            rankButton.gameObject.SetActive(true);
            enterButton.Text = "Enter";
            _period = (row.StartedBlockIndex, row.EndedBlockIndex);

            UpdateBossPrefab(row);
            UpdateBossInformationAsync(worldBoss);
            UpdateMyInformation(raider, currentBlockIndex, ranking);
            UpdateUserCount(userCount);
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

            if (WorldBossFrontHelper.TryGetBossPrefab(row.BossId, out var namePrefab,
                    out var spinePrefab))
            {
                if (isOffSeason)
                {
                    _bossNamePrefab = Instantiate(namePrefab, bossNameContainer);
                }

                _bossSpinePrefab = Instantiate(spinePrefab, bossSpineContainer);
            }
        }

        private void UpdateRemainTimer((long, long) time, long current)
        {
            var (begin, end) = time;
            var range = end - begin;
            var progress = current - begin;
            timerSlider.NormalizedValue = (float)progress / range;
            timeBlock.SetTimeBlock(Util.GetBlockToTime(end - current), $"{current}/{end}");
        }

        private void UpdateTicket(long currentBlockIndex)
        {
            Debug.Log("[UpdateTicket]");
            var maxTicket = 3;
            var refillBlockIndex = _cachedRaiderState?.RefillBlockIndex ?? 0;
            var remainTicket = _cachedRaiderState?.RemainChallengeCount ?? maxTicket;

            if (_cachedRaiderState is { RefillBlockIndex: 0 } &&
                _cachedRaiderState.RemainChallengeCount != 0) // temp
            {
                _remainTicket = remainTicket;
            }
            else
            {
                _remainTicket = currentBlockIndex - refillBlockIndex >=
                                WorldBossHelper.RefillInterval
                    ? maxTicket
                    : remainTicket;
            }

            var remainder = (currentBlockIndex - refillBlockIndex) % WorldBossHelper.RefillInterval;
            var remain = WorldBossHelper.RefillInterval - remainder;
            Debug.Log(
                $"[remain] {remain} / [remainTicket] {remainTicket} / [maxTicket] {maxTicket}");

            _headerMenu.WorldBossTickets.Set(remain, _remainTicket, maxTicket);
        }

        private void ShowDetail(WorldBossDetail.ToggleType toggleType)
        {
            _headerMenu.Close(true);
            Find<WorldBossDetail>().Show(toggleType);
        }

        private void OnClickEnter()
        {
            switch (_status)
            {
                case Status.OffSeason:
                    break;
                case Status.Season:
                    if (_remainTicket > 0)
                    {
                        Raid(false);
                    }
                    else
                    {
                        ShowTicketPurchasePopup();
                    }

                    break;
                case Status.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Raid(bool payNcg)
        {
            var inventory = States.Instance.CurrentAvatarState.inventory;
            ActionManager.Instance.Raid(inventory.Costumes
                    .Where(e => e.Equipped)
                    .Select(e => e.NonFungibleId)
                    .ToList(),
                inventory.Equipments
                    .Where(e => e.Equipped)
                    .Select(e => e.NonFungibleId)
                    .ToList(),
                new List<Guid>(),
                payNcg);
        }

        private async void ShowTicketPurchasePopup()
        {
            var state = await Game.Game.instance.Agent.GetStateAsync(GoldCurrencyState.Address);
            if (state is not Bencodex.Types.Dictionary dic)
            {
                return;
            }

            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            if (!WorldBossFrontHelper.TryGetCurrentRow(currentBlockIndex, out var row))
            {
                return;
            }

            var cur = new GoldCurrencyState(dic).Currency;
            var cost = WorldBossHelper.CalculateTicketPrice(row, _cachedRaiderState, cur);
            var balance = States.Instance.GoldBalanceState;
            Find<TicketPurchasePopup>().Show(
                CostType.WorldBossTicket,
                CostType.NCG,
                balance.Gold,
                cost,
                _cachedRaiderState.PurchaseCount,
                row.MaxPurchaseCount,
                () => Raid(true));
        }

        private void ClaimRaidReward()
        {
            ActionManager.Instance.ClaimRaidReward();
        }

        private async Task<(WorldBossState worldBoss, RaiderState raider)>
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

                return (worldBoss, raider);
            });

            await task;
            return task.Result;
        }

        private void UpdateUserCount(int count)
        {
            season.UpdateUserCount(count);
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
            var bossName = WorldBossFrontHelper.TryGetBossName(bossId, out var n)
                ? n
                : string.Empty;

            season.UpdateBossInformation(bossName, level, curHp, maxHp);
        }

        private void UpdateMyInformation(RaiderState state, long currentBlockIndex, int? ranking)
        {
            _cachedRaiderState = state;
            var totalScore = state?.TotalScore ?? 0;
            var highScore = state?.HighScore ?? 0;
            season.UpdateMyInformation(highScore, totalScore, ranking);
            ticketText.text = $"총 도전 횟 수: {state?.TotalChallengeCount ?? 0}\n" +
                              $"티켓 구매 횟 수: {state?.PurchaseCount ?? 0}\n";
        }

        private async void ViewRune(int runeId)
        {
            var agentAddress = States.Instance.AgentState.address;
            var rune = RuneHelper.ToCurrency(runeId);
            var state = await Game.Game.instance.Agent.GetBalanceAsync(agentAddress, rune);

            if (state != null)
            {
                Debug.Log($"[{state.Currency.ToString()}] :{state.MajorUnit.ToString()}");
            }
            else
            {
                Debug.Log("Balance is null!");
            }
        }

        private static void ShowSheetValues()
        {
            Debug.Log("---- [WorldBossListSheet] ----");
            var sheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            foreach (var sheetValue in sheet)
            {
                Debug.Log($"[ID : {sheetValue.Id}] / " +
                          $"BOSS ID : {sheetValue.BossId} / " +
                          $"STARTEDBLOCKINDEX : {sheetValue.StartedBlockIndex} / " +
                          $"ENDEDBLOCKINDEX : {sheetValue.EndedBlockIndex}");
            }

            Debug.Log("---- [WorldBossRankRewardSheet] ----");
            var rewardSheet = Game.Game.instance.TableSheets.WorldBossRankRewardSheet;
            foreach (var sheetValue in rewardSheet)
            {
                Debug.Log($"[ID : {sheetValue.Id}] / " +
                          $"BOSS ID : {sheetValue.BossId} / " +
                          $"RANK : {sheetValue.Rank} / " +
                          $"RANK : {sheetValue.Rune} / " +
                          $"CRYSTAL : {sheetValue.Crystal}");
            }

            Debug.Log("---- [WorldBossGlobalHpSheet] ----");
            var hpSheet = Game.Game.instance.TableSheets.WorldBossGlobalHpSheet;
            foreach (var sheetValue in hpSheet)
            {
                Debug.Log($"[LEVEL : {sheetValue.Level}] / " +
                          $"HP : {sheetValue.Hp}");
            }

            Debug.Log("---- [RuneWeightSheet] ----");
            var runeSheet = Game.Game.instance.TableSheets.RuneWeightSheet;
            foreach (var sheetValue in runeSheet)
            {
                Debug.Log($"[ID : {sheetValue.Id}] / " +
                          $"BOSS ID : {sheetValue.BossId} / " +
                          $"CRYSTAL : {sheetValue.Rank}");

                foreach (var runeInfo in sheetValue.RuneInfos)
                {
                    Debug.Log($"runeInfo.RuneId : {runeInfo.RuneId} / " +
                              $"runeInfo.Weight : {runeInfo.Weight}");
                }
            }
        }
    }
}
