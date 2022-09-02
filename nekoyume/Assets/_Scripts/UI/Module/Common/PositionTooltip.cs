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
        private TextMeshProUGUI titleText;

        [SerializeField]
        private TextMeshProUGUI contentText;

        private void Start()
        {
            touchHandler.OnClick
                .Subscribe(_ =>
                {
                    gameObject.SetActive(false);
                })
                .AddTo(gameObject);
        }

        public void Set(string title, string content)
        {
            titleText.text = title;
            contentText.text = content;
        }
    }
}
