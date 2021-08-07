using Nekoyume.Game.Controller;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public abstract class ItemOptionView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _leftText;

        [SerializeField]
        private TextMeshProUGUI _rightText;

        public bool IsEmpty { get; protected set; }

        public abstract void Show(bool ignoreAnimation = false);

        public void Show(string leftText, string rightText, bool ignoreAnimation = false)
        {
            UpdateView(leftText, rightText);
            Show(ignoreAnimation);
        }

        public void UpdateView(string leftText, string rightText)
        {
            UpdateText(_leftText, leftText);
            UpdateText(_rightText, rightText);

            IsEmpty = string.IsNullOrEmpty(leftText) && string.IsNullOrEmpty(rightText);
        }

        public virtual void UpdateToEmpty() => UpdateView(string.Empty, string.Empty);

        #region Invoke from Animation

        public void OnAnimatorStateBeginning(string stateName)
        {
        }

        public void OnAnimatorStateEnd(string stateName)
        {
            switch (stateName)
            {
                case "Hide":
                    gameObject.SetActive(false);
                    break;
            }
        }

        public void OnRequestPlaySFX(string sfxCode) =>
            AudioController.instance.PlaySfx(sfxCode);

        #endregion

        protected static void UpdateText(TMP_Text textObject, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                textObject.enabled = false;
            }
            else
            {
                textObject.text = text;
            }
        }
    }
}
