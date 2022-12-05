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

        public NoticeData Data { get; private set; }

        public void Set(NoticeData data, System.Action<NoticeItem> onClick = null)
        {
            Data = data;
            headerText.text = Data.Header;
            dateText.text = Data.Date;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                onClick?.Invoke(this);
            });
        }

        public void Select()
        {
            selectObject.SetActive(true);
        }

        public void DeSelect()
        {
            selectObject.SetActive(false);
        }
    }
}
