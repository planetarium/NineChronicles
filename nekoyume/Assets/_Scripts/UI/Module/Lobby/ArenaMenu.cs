using System;
using System.Collections.Generic;
using System.Globalization;
using Nekoyume.Game;
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
        private GameObject _ticketCountGO;

        [SerializeField]
        private TextMeshProUGUI _ticketCount;

        [SerializeField]
        private TimeBlock _ticketResetTime;

        [SerializeField]
        private TextMeshProUGUI _seasonText;

        [SerializeField]
        private GameObject _seasonGameObject;

        [SerializeField]
        private GameObject _championshipGameObject;

        private readonly List<IDisposable> _disposables = new();

        private void OnEnable()
        {
            var agent = Game.Game.instance.Agent;
            UpdateArenaSeasonTitle(agent.BlockIndex);
            agent.BlockIndexSubject
                .Subscribe(UpdateArenaSeasonTitle)
                .AddTo(_disposables);
            RxProps.ArenaTicketsProgress
                .ObserveOnMainThread()
                .Subscribe(UpdateTicket)
                .AddTo(_disposables);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        private void UpdateTicket(RxProps.TicketProgress ticketProgress)
        {
            _ticketCountGO.SetActive(ticketProgress.currentTickets > 0);
            _ticketCount.text = ticketProgress.currentTickets.ToString(CultureInfo.InvariantCulture);

            long remainingBlock = ticketProgress.totalBlockRange - ticketProgress.progressedBlockRange;
            _ticketResetTime.SetTimeBlock($"{remainingBlock:#,0}", remainingBlock.BlockRangeToTimeSpanString());
        }

        private void UpdateArenaSeasonTitle(long blockIndex)
        {
            var currentSeasonData = RxProps.GetSeasonResponseByBlockIndex(blockIndex);
            if (currentSeasonData == null)
            {
                _seasonGameObject.SetActive(false);
                _championshipGameObject.SetActive(false);
                return;
            }

            switch (currentSeasonData.ArenaType)
            {
                case GeneratedApiNamespace.ArenaServiceClient.ArenaType.OFF_SEASON:
                    _seasonGameObject.SetActive(false);
                    _championshipGameObject.SetActive(false);
                    break;
                case GeneratedApiNamespace.ArenaServiceClient.ArenaType.SEASON:
                    _seasonText.text = currentSeasonData.SeasonGroupId.ToString();
                    _seasonGameObject.SetActive(true);
                    _championshipGameObject.SetActive(false);
                    break;
                case GeneratedApiNamespace.ArenaServiceClient.ArenaType.CHAMPIONSHIP:
                    _seasonGameObject.SetActive(false);
                    _championshipGameObject.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
