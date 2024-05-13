using CommandLine;
using Cysharp.Threading.Tasks;
using Nekoyume.L10n;
using Nekoyume.Model.AdventureBoss;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Scroller;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using System;

namespace Nekoyume.UI.Module
{
    using Nekoyume.UI.Model;
    using UniRx;
    public class WorldMapAdventureBoss : MonoBehaviour
    {
        [SerializeField] private GameObject Open;
        [SerializeField] private GameObject WantedOpen;
        [SerializeField] private GameObject WantedClose;
        [SerializeField] private GameObject Close;

        [SerializeField] private TextMeshProUGUI[] RemainingBlockIndexs;
        [SerializeField] private TextMeshProUGUI UsedNCG;
        [SerializeField] private TextMeshProUGUI Floor;

        private readonly List<System.IDisposable> _disposables = new();
        private long _remainingBlockIndex = 0;

        private void OnEnable()
        {
            Game.Game.instance.Agent.BlockIndexSubject
                .Subscribe(UpdateViewAsync)
                .AddTo(_disposables);

            Game.Game.instance.AdventureBossData.CurrentState.Subscribe(OnAdventureBossStateChanged).AddTo(_disposables);
        }


        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        private void UpdateViewAsync(long blockIndex)
        {
            var seasonInfo = Game.Game.instance.AdventureBossData.SeasonInfo.Value;
            if (seasonInfo == null)
            {
                SetDefualtRemainingBlockIndexs();
                return;
            }
            _remainingBlockIndex =  seasonInfo.EndBlockIndex - blockIndex;
            var timeText = $"{_remainingBlockIndex:#,0}({_remainingBlockIndex.BlockRangeToTimeSpanString()})";
            foreach (var text in RemainingBlockIndexs)
            {
                text.text = timeText;
            }
        }

        private void SetDefualtRemainingBlockIndexs()
        {
            foreach (var text in RemainingBlockIndexs)
            {
                text.text = "(-)";
            }
        }

        public void OnClickOpenEnterBountyPopup()
        {
            Widget.Find<AdventureBossEnterBountyPopup>().Show();
        }

        public void OnClickOpenAdventureBoss()
        {
            Widget.Find<LoadingScreen>().Show();
            try
            {
                Game.Game.instance.AdventureBossData.RefreshAllByCurrentState().ContinueWith(() =>
                {
                    Widget.Find<LoadingScreen>().Close();
                    Widget.Find<AdventureBoss>().Show();
                });
            }
            catch (System.Exception e)
            {
                NcDebug.LogError(e);
                Widget.Find<LoadingScreen>().Close();
            }
        }

        public void OnClickAdventureSeasonAlert()
        {
            var remaingTimespan = _remainingBlockIndex.BlockToTimeSpan();
            OneLineSystem.Push(MailType.System, L10nManager.Localize("NOTIFICATION_ADVENTURE_BOSS_REMAINIG_TIME", remaingTimespan.Hours, remaingTimespan.Minutes%60), NotificationCell.NotificationType.Notification);
        }

        private void OnAdventureBossStateChanged(AdventureBossData.AdventureBossSeasonState state)
        {
            switch (state)
            {
                case AdventureBossData.AdventureBossSeasonState.Ready:
                    Open.SetActive(true);
                    Close.SetActive(false);

                    WantedOpen.SetActive(false);
                    WantedClose.SetActive(true);
                    break;
                case AdventureBossData.AdventureBossSeasonState.Progress:
                    Open.SetActive(true);
                    Close.SetActive(false);

                    var seasonInfo = Game.Game.instance.AdventureBossData.SeasonInfo.Value;
                    var experienceInfo = Game.Game.instance.AdventureBossData.ExploreInfo.Value;
                    UsedNCG.text = seasonInfo.UsedNcg.ToCurrencyNotation();
                    Floor.text = experienceInfo.Floor.ToString();
                    break;
                case AdventureBossData.AdventureBossSeasonState.None:
                case AdventureBossData.AdventureBossSeasonState.End:
                default:
                    SetDefualtRemainingBlockIndexs();
                    Close.SetActive(true);
                    Open.SetActive(false);
                    break;
            }
        }
    }
}
