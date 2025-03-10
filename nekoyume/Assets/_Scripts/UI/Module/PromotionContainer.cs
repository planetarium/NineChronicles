using System;
using System.Globalization;
using System.Linq;
using Nekoyume.Game.LiveAsset;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module.WorldBoss;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class PromotionContainer : MonoBehaviour
    {
        [Serializable]
        private enum Season
        {
            None,
            Arena,
            WorldBoss,
            EventDungeon
        }

        [Serializable]
        private class Time
        {
            public string beginDateTime;
            public string endDateTime;
        }

        [SerializeField]
        private Season season;

        [SerializeField]
        private Time[] times;

        [SerializeField]
        private string enableKey;

        [SerializeField][Header("Optional")]
        private TextMeshProUGUI timeText;

        [SerializeField]
        private Image bannerImage;

        [SerializeField]
        private Button linkButton;

        private EventNoticeData _bannerData;

        private void Awake()
        {
            if (!string.IsNullOrEmpty(enableKey))
            {
                _bannerData = LiveAssetManager.instance.BannerData
                    .FirstOrDefault(data => data.EnableKeys.Contains(enableKey));
            }
        }

        private void OnEnable()
        {
            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var inSeason = false;
            switch (season)
            {
                case Season.Arena:
                    var seasonData = RxProps.GetSeasonResponseByBlockIndex(blockIndex);
                    inSeason = seasonData?.ArenaType == GeneratedApiNamespace.ArenaServiceClient.ArenaType.SEASON &&
                        !Game.LiveAsset.GameConfig.IsKoreanBuild;
                    break;
                case Season.WorldBoss:
                    var worldBossStatus = WorldBossFrontHelper.GetStatus(blockIndex);
                    inSeason = worldBossStatus == WorldBossStatus.Season;
                    break;
                case Season.EventDungeon:
                    inSeason = RxProps.EventScheduleRowForDungeon.Value is not null;
                    break;
            }

            if (inSeason)
            {
                return;
            }

            var isInTime = false;
            foreach (var time in times)
            {
                isInTime |= DateTime.UtcNow.IsInTime(time.beginDateTime, time.endDateTime);
            }

            if (isInTime)
            {
                return;
            }

            var hasBanner = _bannerData is not null && _bannerData.UseDateTime &&
                DateTime.UtcNow.IsInTime(_bannerData.BeginDateTime, _bannerData.EndDateTime);
            if (!hasBanner)
            {
                gameObject.SetActive(false);
                return;
            }

            if (timeText)
            {
                var begin = DateTime
                    .ParseExact(_bannerData.BeginDateTime, "yyyy-MM-ddTHH:mm:ss", null)
                    .ToString("M/d", CultureInfo.InvariantCulture);
                var end = DateTime
                    .ParseExact(_bannerData.EndDateTime, "yyyy-MM-ddTHH:mm:ss", null)
                    .ToString("M/d", CultureInfo.InvariantCulture);
                timeText.text = $"{begin} - {end}";
            }

            if (bannerImage)
            {
                bannerImage.sprite = _bannerData.BannerImage;
            }

            if (linkButton)
            {
                linkButton.onClick.RemoveAllListeners();
                linkButton.onClick.AddListener(() =>
                {
                    if (!string.IsNullOrEmpty(_bannerData.Url))
                    {
                        Widget.Find<EventReleaseNotePopup>().Show(_bannerData);
                    }
                });

                TryOpenBanner();
            }

            return;

            // Open the event banner once a day.
            // Only call when linkButton is not null
            void TryOpenBanner()
            {
                const int requiredStage = Game.LiveAsset.GameConfig.RequiredStage.ShowPopupLobbyEntering;
                if (!States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(requiredStage))
                {
                    return;
                }
                
                var lastReadingDayKey = $"LAST_READING_DAY_{enableKey}";
                const string dateTimeFormat = "yyyy-MM-ddTHH:mm:ss";

                var notReadAtToday = true;
                if (PlayerPrefs.HasKey(lastReadingDayKey) &&
                    DateTime.TryParseExact(PlayerPrefs.GetString(lastReadingDayKey),
                        dateTimeFormat, null, DateTimeStyles.None, out var result))
                {
                    notReadAtToday = DateTime.Today != result.Date;
                }

                if (notReadAtToday)
                {
                    Widget.Find<EventReleaseNotePopup>().Show(_bannerData);
                    PlayerPrefs.SetString(lastReadingDayKey, DateTime.Today.ToString(dateTimeFormat));
                }
            }
        }
    }
}
