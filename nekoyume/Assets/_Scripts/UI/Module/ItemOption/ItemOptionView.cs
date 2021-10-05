using Nekoyume.Game.Controller;
using Nekoyume.Model.Stat;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class ItemOptionView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI _leftText;

        [SerializeField]
        private TextMeshProUGUI _rightText;

        [SerializeField]
        protected Animator animator;

        protected static readonly int AnimatorHashShow = Animator.StringToHash("Show");
        protected static readonly int AnimatorHashHide = Animator.StringToHash("Hide");

        public bool IsEmpty { get; protected set; }

        public void Show(string leftText, string rightText, bool ignoreAnimation = false)
        {
            UpdateView(leftText, rightText);
            Show(ignoreAnimation);
        }

        public void Show(bool ignoreAnimation = false)
        {
            gameObject.SetActive(true);

            if (animator)
            {
                animator.Play(AnimatorHashShow, 0, ignoreAnimation ? 1f : 0f);
            }
        }

        public void Hide(bool ignoreAnimation = false)
        {
            if (ignoreAnimation ||
                !animator)
            {
                gameObject.SetActive(false);
                return;
            }

            animator.SetTrigger(AnimatorHashHide);
        }

        public void UpdateView(string leftText, string rightText)
        {
            UpdateText(_leftText, leftText);
            UpdateText(_rightText, rightText);

            IsEmpty = string.IsNullOrEmpty(leftText) && string.IsNullOrEmpty(rightText);
        }

        public void UpdateViewAsTotalAndPlusStat(StatType type, int totalValue, int plusValue) =>
            UpdateView(
                $"{type} {type.ValueToString(totalValue)}",
                plusValue > 0 ? $"+{type.ValueToString(plusValue)}" : string.Empty);

        public void UpdateAsTotalAndPlusSkill(
            string skillName,
            int totalPower,
            int totalChance,
            int plusPower,
            int plusChance) =>
            UpdateView(
                $"{skillName} {totalPower} / {totalChance}%",
                plusPower > 0 || plusChance > 0 ? $"+{plusPower} / +{plusChance}%" : string.Empty);

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
            if (!textObject)
            {
                return;
            }

            if (string.IsNullOrEmpty(text))
            {
                textObject.gameObject.SetActive(false);
            }
            else
            {
                textObject.text = text;
                textObject.gameObject.SetActive(true);
            }
        }
    }
}
