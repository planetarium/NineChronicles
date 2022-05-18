using System;
using System.Collections.Generic;
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
        private ArenaSeasonProgressBar _progressBar;

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
            RxProps.ArenaInfo.SubscribeWithUpdateOnce(info =>
                    _ticketCount.text = info?.DailyChallengeCount.ToString()
                                        ?? string.Empty)
                .AddTo(_disposables);
        }

        private void OnDisable()
        {
            _progressBar.Pause();
            _disposables.DisposeAllAndClear();
        }
    }
}
