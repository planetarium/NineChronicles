using System;
using System.Collections;
using System.Collections.Generic;
using Libplanet;
using Nekoyume.Helper;
using Nekoyume.State;
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
        private TextMeshProUGUI ticketText;

        [SerializeField]
        private TimeBlock timeBlock;

        [SerializeField]
        private Button claimRewardButton;

        private bool _isDone;
        private readonly List<IDisposable> _disposables = new();

        private void Awake()
        {
            claimRewardButton.OnClickAsObservable()
                .Subscribe(_ => ClaimSeasonReward()).AddTo(gameObject);

            WorldBossStates.SubscribeNotification((b) => notification.SetActive(b));
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

        private IEnumerator CoClaimSeasonReward(int raidId, Address agentAddress,
            Address avatarAddress)
        {
            const string url =
                "http://a93dd1d705f7a43149125438c63d092e-1911438231.us-east-2.elb.amazonaws.com:8080/raid/reward";
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

        private void Start()
        {
            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(UpdateBlockIndex)
                .AddTo(_disposables);
        }

        private void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
        }

        private void UpdateBlockIndex(long currentBlockIndex)
        {
            var curStatus = WorldBossFrontHelper.GetStatus(currentBlockIndex);
            switch (curStatus)
            {
                case WorldBossStatus.OffSeason:
                    timeContainer.SetActive(false);
                    ticketContainer.SetActive(false);
                    break;
                case WorldBossStatus.Season:
                    if (!WorldBossFrontHelper.TryGetCurrentRow(currentBlockIndex, out var row))
                    {
                        return;
                    }

                    timeContainer.SetActive(true);
                    timeBlock.SetTimeBlock($"{row.EndedBlockIndex - currentBlockIndex:#,0}",
                        Util.GetBlockToTime(row.EndedBlockIndex - currentBlockIndex));

                    var avatarAddress = States.Instance.CurrentAvatarState.address;
                    var raiderState = WorldBossStates.GetRaiderState(avatarAddress);
                    if (raiderState is null)
                    {
                        ticketContainer.SetActive(false);
                    }
                    else
                    {
                        ticketContainer.SetActive(true);
                        var count =
                            WorldBossFrontHelper.GetRemainTicket(raiderState, currentBlockIndex);
                        ticketText.text = $"{count}";
                    }

                    break;
                case WorldBossStatus.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
