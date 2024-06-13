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
            EventDungeon,
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

        [SerializeField] [Header("Optional")]
        private TextMeshProUGUI timeText;

        [SerializeField]
        private Image bannerImage;

        [SerializeField]
        private Button linkButton;

        private EventNoticeData _bannerData;

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(enableKey))
            {
                _bannerData = LiveAssetManager.instance.BannerData
                    .FirstOrDefault(data => data.EnableKeys.Contains(enableKey));
            }

            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var inSeason = false;
            switch (season)
            {
                case Season.Arena:
                    var arenaSheet = Game.Game.instance.TableSheets.ArenaSheet;
                    var arenaRoundData = arenaSheet.GetRoundByBlockIndex(blockIndex);
                    inSeason = arenaRoundData.ArenaType == ArenaType.Season &&
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

            var isInTime = false;
            foreach (var time in times)
            {
                isInTime |= DateTime.UtcNow.IsInTime(time.beginDateTime, time.endDateTime);
            }

            if (_bannerData is not null)
            {
                isInTime |= _bannerData.UseDateTime &&
                            DateTime.UtcNow.IsInTime(_bannerData.BeginDateTime, _bannerData.EndDateTime);

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
                }
            }

            gameObject.SetActive(inSeason || isInTime);
        }
    }
}
