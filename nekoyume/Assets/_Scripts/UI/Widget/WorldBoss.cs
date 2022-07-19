using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Nekoyume.BlockChain;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Nekoyume.ValueControlComponents.Shader;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class WorldBoss : Widget
    {
        private enum UIState
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
        private TextMeshProUGUI timerText;

        [SerializeField]
        private ShaderPropertySlider timerSlider;

        private GameObject _bossNamePrefab;
        private GameObject _bossSpinePrefab;
        private (long, long) _period;
        private int _ticket;

        // for test
        [SerializeField]
        private TextMeshProUGUI ticketText;

        [SerializeField]
        private Button claimRaidRewardButton;

        [SerializeField]
        private Button viewButton;

        [SerializeField]
        private Button viewRuneButton;

        private UIState _state = UIState.None;
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

            prevRankButton.OnClickAsObservable().Subscribe(_ => ShowPrevRank()).AddTo(gameObject);
            rankButton.OnClickAsObservable().Subscribe(_ => ShowRank()).AddTo(gameObject);
            informationButton.OnClickAsObservable().Subscribe(_ => ShowInformation())
                .AddTo(gameObject);
            rewardButton.OnClickAsObservable().Subscribe(_ => ShowReward()).AddTo(gameObject);

            enterButton.OnSubmitSubject.Subscribe(_ => Raid()).AddTo(gameObject);

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
                .Subscribe(x => UpdateView(x))
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
            UpdateView(Game.Game.instance.Agent.BlockIndex, true);

            var task = Task.Run(async () => { return true; });
            await task;

            loading.Close();
            Show(ignoreShowAnimation);
        }

        public void UpdateView(long currentBlockIndex, bool forceUpdate = false)
        {
            Debug.Log("[WorldBoss] UpdateView");
            if (forceUpdate)
            {
                _state = UIState.None;
            }

            var currentState = WorldBossHelper.IsItInSeason(currentBlockIndex)
                ? UIState.Season
                : UIState.OffSeason;
            if (currentState != _state)
            {
                _state = currentState;

                switch (_state)
                {
                    case UIState.OffSeason:
                        UpdateOffSeason(currentBlockIndex);
                        break;
                    case UIState.Season:
                        UpdateSeason(currentBlockIndex);
                        break;
                    case UIState.None:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            UpdateRemainTimer(_period, currentBlockIndex);
        }

        private void UpdateOffSeason(long currentBlockIndex)
        {
            if (!WorldBossHelper.TryGetNextRow(currentBlockIndex, out var nextRow))
            {
                return;
            }

            offSeasonContainer.SetActive(false);
            seasonContainer.SetActive(true);
            rankButton.gameObject.SetActive(false);
            enterButton.Text = "Practice";

            var begin = WorldBossHelper.TryGetPreviousRow(currentBlockIndex, out var previousRow)
                ? previousRow.EndedBlockIndex
                : 0;
            _period = (begin, nextRow.StartedBlockIndex);

            UpdateBossPrefab(nextRow, true);
        }

        private void UpdateSeason(long currentBlockIndex)
        {
            if (!WorldBossHelper.TryGetCurrentRow(currentBlockIndex, out var row)) // season
            {
                return;
            }

            offSeasonContainer.SetActive(true);
            seasonContainer.SetActive(false);
            rankButton.gameObject.SetActive(true);
            enterButton.Text = "Enter";
            _period = (row.StartedBlockIndex, row.EndedBlockIndex);

            UpdateBossPrefab(row);

            UpdateUserCountAsync(row.Id);
            UpdateBossInformationAsync(row);
            UpdateMyInformationAsync(row.Id);
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

            if (WorldBossHelper.TryGetBossPrefab(row.BossId, out var namePrefab,
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
            timerText.text = $"{Util.GetBlockToTime(end - current)} ({current}/{end})";
            timerSlider.NormalizedValue = (float)progress / range;
        }

        private void ShowPrevRank()
        {
            // Find<WorldBossRank>().Show();
        }

        private void ShowRank()
        {
            Find<WorldBossRank>().Show();
        }

        private void ShowInformation()
        {
            Find<WorldBossInformation>().Show();
        }

        private void ShowReward()
        {
            Find<WorldBossReward>().Show();
        }

        private void Raid()
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
                _ticket <= 0);
        }

        private void ClaimRaidReward()
        {
            ActionManager.Instance.ClaimRaidReward();
        }

        private async void UpdateUserCountAsync(int raidId)
        {
            var address = Addresses.GetRaidersAddress(raidId);
            var state = await Game.Game.instance.Agent.GetStateAsync(address);
            var count = state is Bencodex.Types.List list ? list.Count : 0;
            season.UpdateUserCount(count);
        }

        private async void UpdateBossInformationAsync(WorldBossListSheet.Row row)
        {
            var address = Addresses.GetWorldBossAddress(row.Id);
            var state = await Game.Game.instance.Agent.GetStateAsync(address);
            var bossName = WorldBossHelper.TryGetBossName(row.BossId, out var n)
                                ? n : string.Empty; var level = 1;

            var hpSheet = Game.Game.instance.TableSheets.WorldBossGlobalHpSheet;
            var baseHp = hpSheet.Values.FirstOrDefault(x => x.Level == 1)!.Hp;
            var curHp = baseHp;
            var maxHp = baseHp;

            if (state is Bencodex.Types.List list)
            {
                Debug.Log("");
                var result = new WorldBossState(list);
                level = result.Level;
                curHp = result.CurrentHp;
                maxHp = hpSheet.Values.FirstOrDefault(x => x.Level == result.Level)!.Hp;
            }

            season.UpdateBossInformation(bossName, level, curHp, maxHp);
        }

        private async void UpdateMyInformationAsync(int raidId)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var address = Addresses.GetRaiderAddress(avatarAddress, raidId);
            var state = await Game.Game.instance.Agent.GetStateAsync(address);

            var totalScore = 0;
            var highScore = 0;
            if (state is Bencodex.Types.List list)
            {
                var result = new RaiderState(list);
                totalScore = result.TotalScore;
                highScore = result.HighScore;
                _ticket = result.RemainChallengeCount;

                ticketText.text = $"남은티켓:{result.RemainChallengeCount}\n" +
                                  $"총 도전 횟 수: {result.TotalChallengeCount}\n" +
                                  $"티켓 구매 횟 수: {result.PurchaseCount}\n";
            }

            season.UpdateMyInformation(highScore, totalScore);
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
