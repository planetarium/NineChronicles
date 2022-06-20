using System;
using System.Collections.Generic;
using Nekoyume.Game;
using Nekoyume.Model.Arena;
using Nekoyume.Model.EnumType;
using Nekoyume.State;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module.Lobby
{
    using UniRx;

    public class ArenaMenu : MainMenu
    {
        [SerializeField]
        private TextMeshProUGUI _ticketCount;

        [SerializeField]
        private TextMeshProUGUI _ticketResetTime;

        [SerializeField]
        private TextMeshProUGUI _seasonText;

        [SerializeField]
        private GameObject _seasonGameObject;

        [SerializeField]
        private GameObject _championshipGameObject;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private void OnEnable()
        {
            Game.Game.instance.Agent.BlockIndexSubject
                .Subscribe(UpdateArenaSeasonTitle)
                .AddTo(_disposables);
            RxProps.ArenaInfoTuple
                .SubscribeOnMainThreadWithUpdateOnce(tuple => UpdateTicketCount(tuple.current))
                .AddTo(_disposables);
            RxProps.ArenaTicketProgress
                .SubscribeOnMainThread()
                .Subscribe(UpdateTicketResetTime)
                .AddTo(_disposables);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        private void UpdateTicketCount(ArenaInformation arenaInformation)
        {
            if (arenaInformation is null)
            {
                _ticketCount.text = ArenaInformation.MaxTicketCount.ToString();
                _ticketResetTime.text = string.Empty;
                return;
            }

            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var currentRoundData =
                TableSheets.Instance.ArenaSheet.GetRoundByBlockIndex(blockIndex);
            var ticket = arenaInformation.GetTicketCount(
                blockIndex,
                currentRoundData.StartBlockIndex,
                States.Instance.GameConfigState.DailyArenaInterval);
            _ticketCount.text = ticket.ToString();
        }

        private void UpdateTicketResetTime((
            int currentTicketCount,
            int maxTicketCount,
            int progressedBlockRange,
            int totalBlockRange,
            string remainTimespanToReset) tuple)
        {
            var (_, _, _, _, remainTimespan) = tuple;
            _ticketResetTime.text = remainTimespan;
        }

        private void UpdateArenaSeasonTitle(long blockIndex)
        {
            var currentRoundData =
                TableSheets.Instance.ArenaSheet.GetRoundByBlockIndex(blockIndex);
            switch (currentRoundData.ArenaType)
            {
                case ArenaType.OffSeason:
                    _seasonGameObject.SetActive(false);
                    _championshipGameObject.SetActive(false);
                    break;
                case ArenaType.Season:
                    _seasonText.text = TableSheets.Instance.ArenaSheet
                        .GetSeasonNumber(
                            blockIndex,
                            currentRoundData.Round)
                        .ToString();
                    _seasonGameObject.SetActive(true);
                    _championshipGameObject.SetActive(false);
                    break;
                case ArenaType.Championship:
                    _seasonGameObject.SetActive(false);
                    _championshipGameObject.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
