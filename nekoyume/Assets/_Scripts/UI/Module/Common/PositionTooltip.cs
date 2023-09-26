using Nekoyume.Game.Character;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module.Common
{
    using UniRx;
    public class PositionTooltip : MonoBehaviour
    {
        [SerializeField]
        private TouchHandler touchHandler;

        [SerializeField]
        protected TextMeshProUGUI titleText;

        [SerializeField]
        protected TextMeshProUGUI contentText;

        public void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                gameObject.SetActive(false);
            }
        }

        public void Set(string title, string content)
        {
            if(titleText != null)
                titleText.text = title;

            if(content != null)
                contentText.text = content;
        }
    }
}
