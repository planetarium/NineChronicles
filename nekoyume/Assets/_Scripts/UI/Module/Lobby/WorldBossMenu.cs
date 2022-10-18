using System;
using System.Collections.Generic;
using Libplanet;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module.WorldBoss;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module.Lobby
{
    using UniRx;

    public class WorldBossMenu : MainMenu
    {
        private const float RetryTime = 20f;
        private const int MaxRetryCount = 8;

        [SerializeField]
        private GameObject onSeason;

        [SerializeField]
        private GameObject offSeason;

        [SerializeField]
        private GameObject ticketContainer;

        [SerializeField]
        private GameObject timeContainer;

        [SerializeField]
        private GameObject notification;

        [SerializeField]
        private GameObject seasonRewardIcon;

        [SerializeField]
        private TextMeshProUGUI ticketText;

        [SerializeField]
        private TimeBlock timeBlock;

        [SerializeField]
        private GameObject loadingIndicator;

        private readonly List<IDisposable> _disposables = new();
        private readonly Dictionary<Address, bool> _madeRaidMail = new();

        private const long SettleSeasonRewardInterval = 7200;

        private void Start()
        {
            WorldBossStates.SubscribeGradeRewards(b => notification.SetActive(b));

            Game.Game.instance.Agent.BlockIndexSubject.Subscribe(UpdateBlockIndex).AddTo(_disposables);
            UpdateBlockIndex(Game.Game.instance.Agent.BlockIndex);
            CheckSeasonRewards();
        }

        private void OnEnable()
        {
            UpdateBlockIndex(Game.Game.instance.Agent.BlockIndex);
            CheckSeasonRewards();
        }

        private void CheckSeasonRewards()
        {
            if (States.Instance.CurrentAvatarState == null || Game.Game.instance.Agent == null)
            {
                return;
            }

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            if (WorldBossFrontHelper.IsItInSeason(blockIndex))
            {
                return;
            }

            if (!WorldBossFrontHelper.TryGetPreviousRow(blockIndex, out var row))
            {
                return;
            }

            if (blockIndex <= row.EndedBlockIndex + SettleSeasonRewardInterval)
            {
                loadingIndicator.SetActive(true);
                seasonRewardIcon.gameObject.SetActive(true);
                return;
            }

            if (WorldBossStates.GetPreRaiderState(avatarAddress) is null)
            {
                return;
            }

            loadingIndicator.SetActive(false);
            seasonRewardIcon.gameObject.SetActive(false);
            AddSeasonRewardMail(row.Id);
        }

        private void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
        }

        private void AddSeasonRewardMail(int raidId)
        {
            if (States.Instance.CurrentAvatarState == null)
            {
                return;
            }

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            if (_madeRaidMail.ContainsKey(avatarAddress))
            {
                return;
            }

            _madeRaidMail.Add(avatarAddress, true);
            var localRewardKey =
                $"SeasonRewards_{raidId}_{avatarAddress}";
            void MakeMail(string json, bool isNew)
            {
                var seasonReward = JsonUtility.FromJson<SeasonRewardRecord>(json);
                LocalMailHelper.Instance.Initialize();
                var now = Game.Game.instance.Agent.BlockIndex;
                LocalMailHelper.Instance.Add(new Address(seasonReward.avatarAddress),
                    new RaidRewardMail(now, Guid.NewGuid(), now, seasonReward)
                        {New = isNew});
            }

            if (PlayerPrefs.HasKey(localRewardKey))
            {
                var json = PlayerPrefs.GetString(localRewardKey);
                MakeMail(json, false);
            }
            else
            {
                StartCoroutine(WorldBossQuery.CoGetSeasonRewards(
                    raidId,
                    avatarAddress,
                    json =>
                    {
                        MakeMail(json, true);
                        PlayerPrefs.SetString(localRewardKey, json);
                    }, null));
            }
        }

        private void UpdateBlockIndex(long currentBlockIndex)
        {
            var curStatus = WorldBossFrontHelper.GetStatus(currentBlockIndex);
            switch (curStatus)
            {
                case WorldBossStatus.OffSeason:
                    onSeason.SetActive(false);
                    offSeason.SetActive(true);
                    timeContainer.SetActive(false);
                    ticketContainer.SetActive(false);
                    break;
                case WorldBossStatus.Season:
                    if (!WorldBossFrontHelper.TryGetCurrentRow(currentBlockIndex, out var row))
                    {
                        return;
                    }

                    if (States.Instance.CurrentAvatarState is null)
                    {
                        return;
                    }

                    onSeason.SetActive(true);
                    offSeason.SetActive(false);
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
