using System.Linq;
using System.Threading.Tasks;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.Model.State;
using Nekoyume.State;
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
        public Image informationButton;

        [SerializeField]
        public Image previousButton;

        [SerializeField]
        public Image rankButton;

        [SerializeField]
        public Image rewardButton;

        [SerializeField]
        private Button backButton;

        [SerializeField]
        private Button joinButton;

        [SerializeField]
        private Button viewButton;

        // 랭킹팝업
        // 월드보상
        // 월드보스 상세
        // 설명

        protected override void Awake()
        {
            base.Awake();

            backButton.OnClickAsObservable().Subscribe(_ =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            }).AddTo(gameObject);

            CloseWidget = () =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            };

            joinButton.OnClickAsObservable().Subscribe(_ =>
            {
                Raid();
            }).AddTo(gameObject);

            viewButton.OnClickAsObservable().Subscribe(_ =>
            {
                View();
            }).AddTo(gameObject);
        }
        public async UniTaskVoid ShowAsync(bool ignoreShowAnimation = false)
        {
            var sheet = Game.Game.instance.TableSheets.WorldBossListSheet;
            foreach (var row in sheet)
            {
                Debug.Log($"[ID : {row.Id}] / " +
                          $"BOSS ID : {row.BossId} / " +
                          $"STARTEDBLOCKINDEX : {row.StartedBlockIndex} / " +
                          $"ENDEDBLOCKINDEX : {row.EndedBlockIndex}");
            }
            var loading = Find<DataLoadingScreen>();
            loading.Show();
            var task = Task.Run(async () =>
            {
                return true;
            });

            await task;

            loading.Close();
            Show(ignoreShowAnimation);
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
                var result =  new WorldBossState(list);
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
                var result =  new RaiderState(list);
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
