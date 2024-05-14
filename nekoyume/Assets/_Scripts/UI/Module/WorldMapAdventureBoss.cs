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
        [SerializeField] private GameObject open;
        [SerializeField] private GameObject wantedOpen;
        [SerializeField] private GameObject wantedClose;
        [SerializeField] private GameObject close;
        [SerializeField] private TextMeshProUGUI[] remainingBlockIndexs;
        [SerializeField] private TextMeshProUGUI usedGold;
        [SerializeField] private TextMeshProUGUI floor;

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
                if (Game.Game.instance.AdventureBossData.CurrentState.Value == AdventureBossData.AdventureBossSeasonState.End)
                {
                    var adventureBossData = Game.Game.instance.AdventureBossData;
                    if(adventureBossData.EndedSeasonInfos.TryGetValue(adventureBossData.LatestSeason.Value.SeasonId, out var endedSeasonInfo))
                    {
                        RefreshBlockIndexText(blockIndex, endedSeasonInfo.NextStartBlockIndex);
                        return;
                    }
                }
                SetDefualtRemainingBlockIndexs();
                return;
            }
            RefreshBlockIndexText(blockIndex, seasonInfo.EndBlockIndex);
        }

        private void RefreshBlockIndexText(long blockIndex, long targetBlock)
        {
            _remainingBlockIndex = targetBlock - blockIndex;
            var timeText = $"{_remainingBlockIndex:#,0}({_remainingBlockIndex.BlockRangeToTimeSpanString()})";
            foreach (var text in remainingBlockIndexs)
            {
                text.text = timeText;
            }
        }

        private void SetDefualtRemainingBlockIndexs()
        {
            foreach (var text in remainingBlockIndexs)
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
                    open.SetActive(true);
                    close.SetActive(false);

                    wantedOpen.SetActive(false);
                    wantedClose.SetActive(true);
                    break;
                case AdventureBossData.AdventureBossSeasonState.Progress:
                    open.SetActive(true);
                    close.SetActive(false);

                    wantedOpen.SetActive(true);
                    wantedClose.SetActive(false);

                    var seasonInfo = Game.Game.instance.AdventureBossData.SeasonInfo.Value;
                    var experienceInfo = Game.Game.instance.AdventureBossData.ExploreInfo.Value;
                    usedGold.text = seasonInfo.UsedNcg.ToCurrencyNotation();
                    if(experienceInfo == null)
                    {
                        floor.text = "-";
                    }
                    else
                    {
                        floor.text = $"{experienceInfo.Floor.ToString()}F";
                    }
                    break;
                case AdventureBossData.AdventureBossSeasonState.None:
                case AdventureBossData.AdventureBossSeasonState.End:
                default:
                    SetDefualtRemainingBlockIndexs();
                    close.SetActive(true);
                    open.SetActive(false);
                    break;
            }
        }
    }
}
