using System;
using System.Collections.Generic;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.Model.Arena;
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

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private void OnEnable()
        {
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
            var currentRoundData = TableSheets.Instance.ArenaSheet.GetRoundByBlockIndex(blockIndex);
            var ticket = arenaInformation.GetTicketCount(
                blockIndex,
                currentRoundData.StartBlockIndex,
                States.Instance.GameConfigState.DailyArenaInterval);
            _ticketCount.text = ticket.ToString();
        }

        private void UpdateTicketResetTime((long beginning, long end, long progress) tuple)
        {
            var (beginning, end, progress) = tuple;
            _ticketResetTime.text = Util.GetBlockToTime(end - beginning - progress);
        }
    }
}
