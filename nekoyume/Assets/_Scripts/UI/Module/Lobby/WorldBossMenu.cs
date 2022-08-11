using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nekoyume.Helper;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module.WorldBoss;
using TMPro;
using UnityEngine;
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
        private GameObject ticket;

        [SerializeField]
        private Image timeImage;

        [SerializeField]
        private TextMeshProUGUI ticketText;

        [SerializeField]
        private TimeBlock timeBlock;

        private RaiderState _cachedRaiderState;
        private bool _isDone;
        private readonly List<IDisposable> _disposables = new();

        private void Awake()
        {
            Game.Event.OnRoomEnter.AddListener(_ => Set());
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
            var currentBlockIndex = Game.Game.instance.Agent.BlockIndex;
            var curStatus = WorldBossFrontHelper.GetStatus(currentBlockIndex);
            switch (curStatus)
            {
                case WorldBossStatus.OffSeason:
                    break;
                case WorldBossStatus.Season:
                    if (!WorldBossFrontHelper.TryGetCurrentRow(currentBlockIndex, out var row))
                    {
                        break;
                    }

                    _isDone = false;
                    _cachedRaiderState = await GetStatesAsync(row);
                    _isDone = true;
                    break;
                case WorldBossStatus.None:
                default:
                    throw new ArgumentOutOfRangeException();
            }
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

                    ticketContainer.SetActive(true);
                    timeContainer.SetActive(false);
                    if (_cachedRaiderState is null)
                    {
                        notification.SetActive(true);
                        ticket.SetActive(false);
                    }
                    else
                    {
                        notification.SetActive(false);
                        ticket.SetActive(true);
                        var count = WorldBossFrontHelper.GetRemainTicket(_cachedRaiderState, currentBlockIndex);
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
            timeBlock.SetTimeBlock(Util.GetBlockToTime(end - current), $"{current}/{end}");
        }
    }
}
