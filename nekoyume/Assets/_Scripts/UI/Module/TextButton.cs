using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Nekoyume.UI.Module
{
    [RequireComponent(typeof(Button))]
    public class TextButton : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI text = null;

        private Button _button = null;

        public System.Action OnClick { get; set; } = null;

        public string Text
        {
            get => text.text;
            set => text.text = value;
        }

        public bool Interactable
        {
            get => _button.interactable;
            set => _button.interactable = value;
        }

#region Mono

        protected void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(() => OnClick?.Invoke());
        }

#endregion
    }
}
