using System;
using UnityEngine.UI;
using UnityEngine;
using Nekoyume.Game.Controller;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.Helper;
using UnityEngine.AddressableAssets;

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

        private const string LastNoticeDayKeyFormat = "LAST_NOTICE_DAY_{0}";

        private NoticeInfo _usingNoticeInfo;

        private static bool CanShowNoticePopup(NoticeInfo notice)
        {
            if (notice == null)
            {
                return false;
            }

            if (!Game.Game.instance.Stage.TutorialController.IsCompleted)
            {
                return false;
            }

            if (!Util.IsInTime(notice.beginTime, notice.endTime, false))
            {
                return false;
            }

            var lastNoticeDayKey = string.Format(LastNoticeDayKeyFormat, notice.name);
            var lastNoticeDay = DateTime.ParseExact(
                PlayerPrefs.GetString(lastNoticeDayKey, "2022/03/01 00:00:00"),
                "yyyy/MM/dd HH:mm:ss",
                null);
            var now = DateTime.UtcNow;
            var isNewDay = now.Year != lastNoticeDay.Year ||
                           now.Month != lastNoticeDay.Month ||
                           now.Day != lastNoticeDay.Day;
            if (isNewDay)
            {
                PlayerPrefs.SetString(lastNoticeDayKey, now.ToString("yyyy/MM/dd HH:mm:ss"));
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
            Addressables.LoadAssetAsync<NoticeInfoScriptableObject>("notice").Completed +=
                operationHandle =>
                {
                    if (operationHandle.IsValid())
                    {
                        _usingNoticeInfo = operationHandle.Result.noticeInfo;
                        if (!CanShowNoticePopup(_usingNoticeInfo))
                        {
                            return;
                        }

                        contentImage.sprite = _usingNoticeInfo.contentImage
                            ? _usingNoticeInfo.contentImage
                            : contentImage.sprite;
                    }
                };

            base.Show(ignoreStartAnimation);
        }

        private void GoToNoticePage()
        {
            Application.OpenURL(_usingNoticeInfo.pageUrlFormat);
        }
    }
}
