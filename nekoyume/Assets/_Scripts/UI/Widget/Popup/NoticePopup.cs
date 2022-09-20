using System;
using System.Collections;
using UnityEngine.UI;
using UnityEngine;
using Nekoyume.Game.Controller;
using UnityEngine.Networking;
using Nekoyume.UI.Model;
using System.Text.Json;

namespace Nekoyume.UI
{
    public class NoticePopup : PopupWidget
    {
        [SerializeField]
        private Image contentImage;

        [SerializeField]
        private Button detailButton;

        [SerializeField]
        private Button closeButton;

        private const string LastNoticeDayKeyFormat = "LAST_NOTICE_DAY_{0}";
        private const string JsonUrl =
            "https://raw.githubusercontent.com/planetarium/NineChronicles.LiveAssets/main/Assets/Json/Notice.json";

        private const string ImageUrl =
            "https://raw.githubusercontent.com/planetarium/NineChronicles.LiveAssets/main/Assets/Images/Notice";

        private Notice _data;

        private bool CanShowNoticePopup()
        {
            if (_data == null)
            {
                return false;
            }

            if (!Game.Game.instance.Stage.TutorialController.IsCompleted)
            {
                return false;
            }

            if (!DateTime.UtcNow.IsInTime(_data.BeginDateTime, _data.EndDateTime))
            {
                return false;
            }

            var lastNoticeDayKey = string.Format(LastNoticeDayKeyFormat, _data.ImageName);
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

            closeButton.onClick.AddListener(() =>
            {
                Close();
                AudioController.PlayClick();
            });
            closeButton.interactable = false;
        }

        private void Start()
        {
            StartCoroutine(RequestManager.instance.GetJson(JsonUrl, Set));
        }

        private void Set(string json)
        {
            var data = JsonSerializer.Deserialize<Notice>(json);
            _data = data;
            StartCoroutine(CoSetTexture(_data.ImageName));

            detailButton.onClick.RemoveAllListeners();
            detailButton.onClick.AddListener(() =>
            {
                Application.OpenURL(data.Url);
                AudioController.PlayClick();
            });
        }

        public override void Show(bool ignoreStartAnimation = false)
        {
            if (!CanShowNoticePopup())
            {
                return;
            }

            base.Show(ignoreStartAnimation);
        }

        private IEnumerator CoSetTexture(string imageName)
        {
            var www = UnityWebRequestTexture.GetTexture($"{ImageUrl}/{imageName}.png");
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
            }
        }
    }
}
