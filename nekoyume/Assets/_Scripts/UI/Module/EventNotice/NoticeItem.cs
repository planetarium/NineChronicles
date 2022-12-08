using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class NoticeItem : MonoBehaviour
    {
        [SerializeField]
        private Button button;

        [SerializeField]
        private TextMeshProUGUI headerText;

        [SerializeField]
        private TextMeshProUGUI dateText;

        [SerializeField]
        private GameObject selectObject;

        [SerializeField]
        private GameObject notificationObject;

        public NoticeData Data { get; private set; }

        public void Set(NoticeData data, bool hasNotification = false, System.Action<NoticeItem> onClick = null)
        {
            Data = data;
            headerText.text = Data.Header;
            dateText.text = Data.Date;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                onClick?.Invoke(this);
            });
            notificationObject.SetActive(hasNotification);
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
