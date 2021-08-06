using System;
using System.Collections.Generic;
using Nekoyume.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Stat;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class ItemOptionView : MonoBehaviour
    {
        [Serializable]
        public struct OptionCountObjectSet
        {
            public GameObject backgroundObject;
            public GameObject iconObject;

            public void SetActive(bool value)
            {
                backgroundObject.SetActive(value);
                iconObject.SetActive(value);
            }
        }

        [SerializeField]
        private List<OptionCountObjectSet> _optionCountObjects;

        [SerializeField]
        private TextMeshProUGUI _text;

        [SerializeField]
        private TextMeshProUGUI _coverText;

        [SerializeField]
        private Animator _animator;

        private static readonly int AnimatorHashShow = Animator.StringToHash("Show");
        private static readonly int AnimatorHashHide = Animator.StringToHash("Hide");
        private static readonly int AnimatorHashDiscover = Animator.StringToHash("Discover");

        public bool IsEmpty { get; private set; }

        public void Show(string text, string coverText, int optionCount = 1, bool ignoreAnimation = false)
        {
            _text.text = text;
            _coverText.text = coverText;
            for (var i = 0; i < _optionCountObjects.Count; i++)
            {
                _optionCountObjects[i].SetActive(i < optionCount);
            }

            Show();
        }

        public void Show(bool ignoreAnimation = false)
        {
            gameObject.SetActive(true);
            _animator.Play(AnimatorHashShow, 0, ignoreAnimation ? 1f : 0f);
        }

        public void Discover(bool ignoreAnimation = false)
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (ignoreAnimation)
            {
                _animator.Play(AnimatorHashDiscover, 0, 1f);
            }
            else
            {
                _animator.SetTrigger(AnimatorHashDiscover);
            }
        }

        public void Hide(bool ignoreAnimation = false)
        {
            if (ignoreAnimation)
            {
                gameObject.SetActive(false);
                return;
            }

            _animator.SetTrigger(AnimatorHashHide);
        }

        public void UpdateView(string text, string coverText, int optionCount = 1)
        {
            _text.text = text;
            _coverText.text = coverText;
            for (var i = 0; i < _optionCountObjects.Count; i++)
            {
                _optionCountObjects[i].SetActive(i < optionCount);
            }

            IsEmpty = false;
        }

        public void UpdateAsStatTuple((StatType type, int value, int count) tuple) =>
            UpdateView(
                $"{tuple.type.ToString()} +{tuple.value}",
                L10nManager.Localize("UI_ITEM_OPTION_COVER_TEXT_FORMAT", tuple.type.ToString()),
                tuple.count);

        public void UpdateAsSkillTuple((string skillName, int power, int chance) tuple) =>
            UpdateView(
                $"{tuple.skillName} {tuple.power} / {tuple.chance}%",
                L10nManager.Localize("UI_ITEM_OPTION_COVER_TEXT_FORMAT", tuple.skillName));

        public void UpdateAsEmpty()
        {
            UpdateView(string.Empty, string.Empty);
            IsEmpty = true;
        }

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
    }
}
