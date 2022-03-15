using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine.UI;
using UnityEngine;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;

namespace Nekoyume.UI
{
    public class NoticePopup : PopupWidget
    {
        [Serializable]
        public class NoticeInfo
        {
            public string name;
            public Sprite contentImage;
            public string beginTime;
            public string endTime;
            public string pageUrlFormat;
        }

        [SerializeField]
        private Image contentImage;

        [SerializeField]
        private Button detailButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private NoticeInfo[] noticeList =
        {
            new NoticeInfo
            {
                name = "ItemLevelRequirement",
                contentImage = null,
                beginTime = "2022/03/01 00:00:00",
                endTime = "2022/03/31 23:59:59",
                pageUrlFormat = "https://www.notion.so/planetarium/1bc6de399b3b4ace95fca3a3020b4d79"
            }
        };

        private const string LastNoticeDayKeyFormat = "NOTICE_POPUP_LAST_DAY_{0}";

        private static bool CanShowNoticePopup(NoticeInfo notice)
        {
            if (notice == null)
            {
                return false;
            }

            var worldInfo = Game.Game.instance.States.CurrentAvatarState.worldInformation;
            if (worldInfo is null) return false;
            var clearedStageId = worldInfo.TryGetLastClearedStageId(out var id) ? id : 1;
            if (TutorialController.GetCheckPoint(clearedStageId) != 0) return false;

            var tutorialControllerIsPlaying = Game.Game.instance.Stage.TutorialController.IsPlaying;
            if (tutorialControllerIsPlaying) return false;

            if (!Util.IsInTime(notice.beginTime, notice.endTime, false)) return false;

            var lastNoticeDayKey = string.Format(LastNoticeDayKeyFormat, notice.name);
            var lastNoticeDay = DateTime.Parse(PlayerPrefs.GetString(lastNoticeDayKey, "2022/03/01 00:00:00"));
            var now = DateTime.UtcNow;
            var isNewDay = now.Year != lastNoticeDay.Year || now.Month != lastNoticeDay.Month || now.Day != lastNoticeDay.Day;
            if (isNewDay)
            {
                PlayerPrefs.SetString(lastNoticeDayKey, now.ToString(CultureInfo.InvariantCulture));
            }

            return isNewDay;
        }

        protected override void Awake()
        {
            base.Awake();

            detailButton.onClick.AddListener(() =>
            {
                GoToNoticePage();
                AudioController.PlayClick();
            });

            closeButton.onClick.AddListener(() =>
            {
                Close();
                AudioController.PlayClick();
            });
        }

        public override void Show(bool ignoreStartAnimation = false)
        {
            var firstNotice = noticeList.FirstOrDefault();
            if (!CanShowNoticePopup(firstNotice))
            {
                return;
            }

            contentImage.sprite = firstNotice.contentImage;
            base.Show(ignoreStartAnimation);
        }

        private void GoToNoticePage()
        {
            Application.OpenURL(noticeList.FirstOrDefault()?.pageUrlFormat);
        }
    }
}
