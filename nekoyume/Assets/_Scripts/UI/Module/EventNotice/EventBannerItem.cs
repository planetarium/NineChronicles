using Nekoyume.UI.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EventBannerItem : MonoBehaviour
    {
        [SerializeField]
        private RawImage image;

        [SerializeField]
        private Button button;

        [SerializeField]
        private GameObject selectObject;

        [SerializeField]
        private GameObject notificationObject;

        public EventNoticeData Data { get; private set; }

        public void Set(EventNoticeData data, bool hasNotification = false, System.Action<EventBannerItem> onClick = null)
        {
            Data = data;
            image.texture = Data.BannerImage.texture;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                var action = onClick ?? (noticeData =>
                    Widget.Find<EventReleaseNotePopup>().Show(noticeData.Data));
                action.Invoke(this);
            });
            if (notificationObject)
            {
                notificationObject.SetActive(hasNotification);
            }
        }

        public void Select()
        {
            selectObject.SetActive(true);
            notificationObject.SetActive(false);
        }

        public void DeSelect()
        {
            selectObject.SetActive(false);
        }
    }
}
