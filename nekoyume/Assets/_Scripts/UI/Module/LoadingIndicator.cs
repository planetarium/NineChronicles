using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class LoadingIndicator : MonoBehaviour
    {
        public TextMeshProUGUI text;

        public void Show(string msg)
        {
            UpdateMessage(msg);
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        public void UpdateMessage(string msg)
        {
            text.text = msg;
        }
    }
}
