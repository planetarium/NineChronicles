using UnityEngine;
using TMPro;
using System;

namespace Nekoyume.UI.Module.Lobby
{
    using UniRx;
    public class SeasonPassMenu : MainMenu
    {
        [SerializeField]
        private GameObject premiumIcon;
        [SerializeField]
        private GameObject premiumPlusIcon;
        [SerializeField]
        private TextMeshProUGUI levelText;
        [SerializeField]
        private TextMeshProUGUI timeText;

        private void Awake()
        {
            var seasonPassService = Game.Game.instance.SeasonPassServiceManager;
            seasonPassService.IsPremium.SubscribeTo(premiumIcon).AddTo(gameObject);
            seasonPassService.SeasonPassLevel.Subscribe((level) =>
            {
                levelText.text = $"Lv.{level}";
            }).AddTo(gameObject);

            Game.Game.instance.SeasonPassServiceManager.SeasonEndDate.Subscribe((endDate) =>
            {
                RefreshTimeText();
            });

            Observable.Timer(TimeSpan.Zero, TimeSpan.FromMinutes(1)).Subscribe((time) =>
            {
                RefreshTimeText();
            }).AddTo(gameObject);

            RefreshTimeText();
        }

        private void RefreshTimeText()
        {
            var timeSpan = Game.Game.instance.SeasonPassServiceManager.SeasonEndDate.Value - DateTime.Now;
            var dayExist = timeSpan.TotalDays > 1;
            var hourExist = timeSpan.TotalHours >= 1;
            var dayText = dayExist ? $"{(int)timeSpan.TotalDays}d " : string.Empty;
            var hourText = hourExist ? $"{(int)timeSpan.Hours}h " : string.Empty;
            timeText.text = $"<Style=Clock> {dayText}{hourText}";
        }
    }
}
