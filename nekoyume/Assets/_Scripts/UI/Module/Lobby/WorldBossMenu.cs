using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Libplanet;
using mixpanel;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module.WorldBoss;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Lobby
{
    using UniRx;

    public class WorldBossMenu : MainMenu
    {
        [SerializeField]
        private GameObject ticketContainer;

        [SerializeField]
        private GameObject timeContainer;

        [SerializeField]
        private GameObject notification;

        [SerializeField]
        private Image timeImage;

        [SerializeField]
        private TextMeshProUGUI ticketText;

        [SerializeField]
        private TimeBlock timeBlock;

        [SerializeField]
        private Button claimRewardButton;

        private bool _isDone;
        private readonly List<IDisposable> _disposables = new();

        private void Awake()
        {
            Game.Event.OnRoomEnter.AddListener(_ => Set());
            claimRewardButton.OnClickAsObservable()
                .Subscribe(_ => ClaimSeasonReward()).AddTo(gameObject);
        }

        private void ClaimSeasonReward()
        {
            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            Debug.Log("OnClick claim button");
            Debug.Log($"Agent : {agentAddress}");
            Debug.Log($"Avatar : {avatarAddress}");
            StartCoroutine(CoClaimSeasonReward(3, agentAddress, avatarAddress));
        }

        private IEnumerator CoClaimSeasonReward(int raidId, Address agentAddress, Address avatarAddress)
        {
            const string url = "http://a93dd1d705f7a43149125438c63d092e-1911438231.us-east-2.elb.amazonaws.com:8080/raid/reward";
            var form = new WWWForm();
            form.AddField("raid_id", raidId);
            form.AddField("avatar_address", avatarAddress.ToHex());
            form.AddField("agent_Address", agentAddress.ToHex());
            using (var request = UnityWebRequest.Post(url, form))
            {
                yield return request.SendWebRequest();
                Debug.Log(request.result != UnityWebRequest.Result.Success
                    ? request.error
                    : "Form upload complete");
            }
        }


        private void OnEnable()
        {
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(UpdateBlockIndex)
                .AddTo(_disposables);

            timeContainer.SetActive(false);
            ticketContainer.SetActive(false);
        }

        private void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
        }

        private async void Set()
        {
            _isDone = false;
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            await WorldBossStates.Set(avatarAddress);
            _isDone = true;
        }

        private async Task<RaiderState> GetStatesAsync(WorldBossListSheet.Row row)
        {
            var task = Task.Run(async () =>
            {
                var avatarAddress = States.Instance.CurrentAvatarState.address;
                var raiderAddress = Addresses.GetRaiderAddress(avatarAddress, row.Id);
                var raiderState = await Game.Game.instance.Agent.GetStateAsync(raiderAddress);
                var raider = raiderState is Bencodex.Types.List raiderList
                    ? new RaiderState(raiderList)
                    : null;

                return raider;
            });

            await task;
            return task.Result;
        }

        private void UpdateBlockIndex(long currentBlockIndex)
        {
            if (!_isDone)
            {
                return;
            }

            var curStatus = WorldBossFrontHelper.GetStatus(currentBlockIndex);
            switch (curStatus)
            {
                case WorldBossStatus.OffSeason:
                    if (!WorldBossFrontHelper.TryGetNextRow(currentBlockIndex, out var nextRow))
                    {
                        ticketContainer.SetActive(false);
                        timeContainer.SetActive(false);
                        return;
                    }

                    ticketContainer.SetActive(false);
                    timeContainer.SetActive(true);
                    var begin =
                        WorldBossFrontHelper.TryGetPreviousRow(currentBlockIndex, out var previousRow)
                            ? previousRow.EndedBlockIndex
                            : 0;
                    var period = (begin, nextRow.StartedBlockIndex);
                    UpdateRemainTimer(period, currentBlockIndex);
                    break;
                case WorldBossStatus.Season:
                    if (!WorldBossFrontHelper.TryGetCurrentRow(currentBlockIndex, out var row))
                    {
                        return;
                    }

                    timeContainer.SetActive(false);

                    var avatarAddress = States.Instance.CurrentAvatarState.address;
                    var raiderState = WorldBossStates.GetRaiderState(avatarAddress);
                    if (raiderState is null)
                    {
                        ticketContainer.SetActive(false);
                    }
                    else
                    {
                        ticketContainer.SetActive(true);
                        var count = WorldBossFrontHelper.GetRemainTicket(raiderState, currentBlockIndex);
                        ticketText.text = $"{count}";
                    }

                    break;
                case WorldBossStatus.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void UpdateRemainTimer((long, long) time, long current)
        {
            var (begin, end) = time;
            var range = end - begin;
            var progress = current - begin;
            timeImage.fillAmount = 1f - (float)progress / range;
            timeBlock.SetTimeBlock($"{end - current:#,0}", Util.GetBlockToTime(end - current));
        }
    }
}
