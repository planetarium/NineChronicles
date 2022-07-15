using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
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
        [SerializeField]
        public TextMeshProUGUI bossName;

        [SerializeField]
        public Image bossImage;

        [SerializeField]
        private Button backButton;

        [SerializeField]
        public Button informationButton;

        [SerializeField]
        public Button rankButton;

        [SerializeField]
        public Button rewardButton;

        [SerializeField]
        private Button joinButton;

        [SerializeField]
        private Button viewButton;

        [SerializeField]
        private Transform bossContainer;

        [SerializeField]
        private TextMeshProUGUI timerText;

        [SerializeField]
        private ShaderPropertySlider timerSlider;

        private GameObject _bossPrefab;
        private WorldBossListSheet.Row _selectedRow;

        // 랭킹팝업
        // 월드보상
        // 월드보스 상세
        // 설명

        private long _currentBlockIndex;

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

            informationButton.OnClickAsObservable().Subscribe(_ => ShowInformation()).AddTo(gameObject);
            rankButton.OnClickAsObservable().Subscribe(_ => ShowRank()).AddTo(gameObject);
            rewardButton.OnClickAsObservable().Subscribe(_ => ShowReward()).AddTo(gameObject);

            joinButton.OnClickAsObservable().Subscribe(_ => Raid()).AddTo(gameObject);
            viewButton.OnClickAsObservable().Subscribe(_ => View()).AddTo(gameObject);
        }

        public override void Initialize()
        {
            base.Initialize();
            Game.Game.instance.Agent.BlockIndexSubject
                .ObserveOnMainThread()
                .Subscribe(SubscribeBlockIndex)
                .AddTo(gameObject);
        }

        private void OnEnable()
        {
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(UpdateBlockIndex)
                .AddTo(_disposables);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        private void UpdateBlockIndex(long blockIndex)
        {
            _currentBlockIndex = blockIndex;
        }

        public async UniTaskVoid ShowAsync(bool ignoreShowAnimation = false)
        {
            var sheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            foreach (var sheetValue in sheet)
            {
                Debug.Log($"[ID : {sheetValue.Id}] / " +
                          $"BOSS ID : {sheetValue.BossId} / " +
                          $"STARTEDBLOCKINDEX : {sheetValue.StartedBlockIndex} / " +
                          $"ENDEDBLOCKINDEX : {sheetValue.EndedBlockIndex}");
            }

            var loading = Find<DataLoadingScreen>();
            loading.Show();

            if (WorldBossHelper.TryGetCurrentRow(_currentBlockIndex, out var row)) // season
            {
                UpdateRemainTimer(row.StartedBlockIndex, row.EndedBlockIndex, _currentBlockIndex);
                UpdateBossPrefab(row);
            }
            else // practice mode
            {
                if (!WorldBossHelper.TryGetNextRow(_currentBlockIndex, out var nextRow))
                {
                    return;
                }

                var begin =
                    WorldBossHelper.TryGetPreviousRow(_currentBlockIndex, out var previousRow)
                        ? previousRow.EndedBlockIndex
                        : 0;

                UpdateRemainTimer(begin, nextRow.StartedBlockIndex, _currentBlockIndex);
                UpdateBossPrefab(nextRow);
            }

            var task = Task.Run(async () => { return true; });

            await task;

            loading.Close();
            Show(ignoreShowAnimation);
        }

        private void SubscribeBlockIndex(long blockIndex)
        {
            _currentBlockIndex = blockIndex;
        }

        private void UpdateBossPrefab(WorldBossListSheet.Row row)
        {
            if (bossContainer != null)
            {
                Destroy(_bossPrefab);
            }

            if (WorldBossHelper.TryGetBossPrefab(row.BossId, out var prefab))
            {
                _bossPrefab = Instantiate(prefab, bossContainer);
            }
        }

        private void UpdateRemainTimer(long begin, long end, long current)
        {
            var range = end - begin;
            var progress = current - begin;
            timerText.text = $"{Util.GetBlockToTime(end - current)} ({current}/{end})";
            timerSlider.NormalizedValue = (float)progress / range;
        }

        private void ShowInformation()
        {
            Find<WorldBossInformation>().Show();
        }

        private void ShowRank()
        {
            Find<WorldBossRank>().Show();
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
                1);
        }

        private void View()
        {
            Debug.Log("[VIEW]");

            GetWorldBossStateAsync(1);
            GetRaidersStateAsync(1);
            GetRaiderStateAsync(1);
        }

        private static async void GetWorldBossStateAsync(int raidId)
        {
            var address = Addresses.GetWorldBossAddress(raidId);
            var state = await Game.Game.instance.Agent.GetStateAsync(address);
            if (state is Bencodex.Types.List list)
            {
                var result = new WorldBossState(list);
                Debug.Log($"[WORLD_BOSS_STATE]" +
                          $"Id: {result.Id} / " +
                          $"Level: {result.Level} / " +
                          $"CurrentHP: {result.CurrentHP} / " +
                          $"StartedBlockIndex: {result.StartedBlockIndex} / " +
                          $"EndedBlockIndex: {result.EndedBlockIndex}");
            }
            else
            {
                Debug.Log("WorldBossState is null");
            }
        }

        private static async void GetRaidersStateAsync(int raidId)
        {
            var address = Addresses.GetRaidersAddress(raidId);
            var state = await Game.Game.instance.Agent.GetStateAsync(address);
            if (state is Bencodex.Types.List list)
            {
                var raiders = list.ToList(StateExtensions.ToAddress);

                var index = 0;
                foreach (var raider in raiders)
                {
                    Debug.Log($"[RAIDERS_STATE] index: {index} / address: {raider}");
                    index++;
                }
            }
            else
            {
                Debug.Log("RaidersState is null");
            }
        }

        private static async void GetRaiderStateAsync(int raidId)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var address = Addresses.GetRaiderAddress(avatarAddress, raidId);
            var state = await Game.Game.instance.Agent.GetStateAsync(address);
            if (state is Bencodex.Types.List list)
            {
                var result = new RaiderState(list);
                Debug.Log($"[RAIDER_STATE] TotalScore: {result.TotalScore} / " +
                          $"HighScore: {result.HighScore} / " +
                          $"TotalChallengeCount: {result.TotalChallengeCount} / " +
                          $"RemainChallengeCount: {result.RemainChallengeCount}");
            }
            else
            {
                Debug.Log("RaiderState is null");
            }
        }
    }
}
