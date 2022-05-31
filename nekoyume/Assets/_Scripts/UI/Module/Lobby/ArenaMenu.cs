using System;
using System.Collections.Generic;
using Nekoyume.Game;
using Nekoyume.Model.Arena;
using Nekoyume.State;
using Nekoyume.UI.Module.Arena;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module.Lobby
{
    using UniRx;

    public class ArenaMenu : MainMenu
    {
        [SerializeField]
        private ArenaTicketProgressBar _progressBar;

        [SerializeField]
        private TextMeshProUGUI _ticketCount;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private void OnEnable()
        {
            var agent = Game.Game.instance.Agent;
            if (agent is null)
            {
                Debug.Log("Agent is null");
                return;
            }

            _progressBar.ResumeOrShow();
            _ticketCount.text = string.Empty;
            RxProps.ArenaInfoTuple
                .SubscribeOnMainThreadWithUpdateOnce(tuple =>
                {
                    var (current, _) = tuple;
                    if (current is null)
                    {
                        _ticketCount.text = ArenaInformation.MaxTicketCount.ToString();
                        return;
                    }

                    var blockIndex = Game.Game.instance.Agent.BlockIndex;
                    var currentRoundData = TableSheets.Instance.ArenaSheet.GetRoundByBlockIndex(blockIndex);
                    var ticket = current.GetTicketCount(
                        blockIndex,
                        currentRoundData.StartBlockIndex,
                        States.Instance.GameConfigState.DailyArenaInterval);
                    _ticketCount.text = ticket.ToString();
                })
                .AddTo(_disposables);
        }

        private void OnDisable()
        {
            _progressBar.Pause();
            _disposables.DisposeAllAndClear();
        }
    }
}
