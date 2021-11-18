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
        
        [SerializeField]
        private Button button = null;

        public System.Action OnClick { get; set; } = null;

        public string Text
        {
            get => text.text;
            set => text.text = value;
        }

        #region Mono

        protected void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(() => OnClick?.Invoke());
        }

        #endregion
    }
}
