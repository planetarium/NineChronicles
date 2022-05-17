using System;
using System.Collections.Generic;
using Bencodex.Types;
using Cysharp.Threading.Tasks;
using Nekoyume.Action;
using Nekoyume.Model.State;
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

        private long _beginningBlockIndex;

        private long _endBlockIndex;

        private long _progressBlockIndex;

        private readonly Subject<(long bedinning, long end, long progress)> _subject
            = new Subject<(long bedinning, long end, long progress)>();

        private void OnEnable()
        {
            var agent = Game.Game.instance.Agent;
            if (agent is null)
            {
                Debug.Log("Agent is null");
                return;
            }

            agent.BlockIndexSubject
                .ObserveOnMainThread()
                .Subscribe(UpdateProgressBar)
                .AddTo(_disposables);
            _progressBar.ResumeOrShow(_subject);
            UpdateProgressBar(agent.BlockIndex);
            UpdateTicketCountAsync().Forget();
        }

        private void OnDisable()
        {
            _progressBar.Pause();
            _disposables.DisposeAllAndClear();
        }

        private void UpdateProgressBar(long blockIndex)
        {
            var cas = States.Instance.CurrentAvatarState;
            if (cas is null)
            {
                return;
            }

            _beginningBlockIndex = cas.dailyRewardReceivedIndex;
            var gcs = States.Instance.GameConfigState;
            if (gcs is null)
            {
                return;
            }

            _endBlockIndex = _beginningBlockIndex + gcs.DailyArenaInterval;
            _progressBlockIndex = blockIndex - _beginningBlockIndex;
            _subject.OnNext((_beginningBlockIndex, _endBlockIndex, _progressBlockIndex));
        }

        private async UniTaskVoid UpdateTicketCountAsync()
        {
            _ticketCount.text = string.Empty;

            var currentAddress = States.Instance.CurrentAvatarState?.address;
            if (!currentAddress.HasValue)
            {
                return;
            }

            var avatarAddress = currentAddress.Value;
            var infoAddress = States.Instance.WeeklyArenaState.address.Derive(avatarAddress.ToByteArray());
            var rawInfo = await UniTask.Run(async () =>
                await Game.Game.instance.Agent.GetStateAsync(infoAddress));
            if (rawInfo is Dictionary dictionary)
            {
                _ticketCount.text = dictionary["dailyChallengeCount"].ToInteger().ToString();
            }
        }
    }
}
