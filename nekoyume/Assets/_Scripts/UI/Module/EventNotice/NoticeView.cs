using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class NoticeView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI headerText;

        [SerializeField]
        private TextMeshProUGUI dataText;

        [SerializeField]
        private TextMeshProUGUI contentsText;

        public void Set(NoticeData data)
        {
            headerText.text = data.Header;
            dataText.text = data.Date;
            contentsText.text = data.Contents;
        }
    }
}
