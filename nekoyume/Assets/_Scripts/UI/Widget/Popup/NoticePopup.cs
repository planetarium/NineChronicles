using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using Nekoyume.Game.Controller;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.Helper;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

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
        private const string BucketUrl =
            "https://9c-asset-bundle.s3.us-east-2.amazonaws.com/Images/Notice_";

        private NoticeInfo _usingNoticeInfo;
        private bool _spriteIsInitialized;

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
            closeButton.interactable = false;
        }

        public override void Initialize()
        {
            Addressables.LoadAssetAsync<NoticeInfoScriptableObject>("notice").Completed +=
                operationHandle =>
                {
                    if (operationHandle.IsValid())
                    {
                        _usingNoticeInfo = operationHandle.Result.noticeInfo;
                    }
                    base.Initialize();
                };
        }

        public override void Show(bool ignoreStartAnimation = false)
        {
            if (!CanShowNoticePopup(_usingNoticeInfo))
            {
                return;
            }

            base.Show(ignoreStartAnimation);
            if (!_spriteIsInitialized)
            {
                StartCoroutine(CoSetTexture());
            }
        }

        private void GoToNoticePage()
        {
            Application.OpenURL(_usingNoticeInfo.pageUrlFormat);
        }

        private IEnumerator CoSetTexture()
        {
            var www = UnityWebRequestTexture.GetTexture($"{BucketUrl}{_usingNoticeInfo.name}.png");
            yield return www.SendWebRequest();
            closeButton.interactable = true;
            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.Log(www.error);
            }
            else
            {
                var myTexture = ((DownloadHandlerTexture)www.downloadHandler).texture;
                contentImage.sprite = Sprite.Create(
                    myTexture,
                    new Rect(0, 0, myTexture.width, myTexture.height),
                    new Vector2(0.5f, 0.5f));
                _spriteIsInitialized = true;
            }
        }
    }
}
