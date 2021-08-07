using System;
using System.Collections.Generic;
using Nekoyume.Model.Stat;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class ItemOptionWithCountView : ItemOptionView
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
        protected Animator animator;

        protected static readonly int AnimatorHashShow = Animator.StringToHash("Show");
        protected static readonly int AnimatorHashHide = Animator.StringToHash("Hide");

        public override void Show(bool ignoreAnimation = false)
        {
            gameObject.SetActive(true);
            animator.Play(AnimatorHashShow, 0, ignoreAnimation ? 1f : 0f);
        }

        public void Show(
            string leftText,
            string rightText,
            int optionCount,
            bool ignoreAnimation = false)
        {
            UpdateView(leftText, rightText, optionCount);
            Show(ignoreAnimation);
        }

        public void Hide(bool ignoreAnimation = false)
        {
            if (ignoreAnimation)
            {
                gameObject.SetActive(false);
                return;
            }

            animator.SetTrigger(AnimatorHashHide);
        }

        public void UpdateView(string leftText, string rightText, int optionCount)
        {
            UpdateView(leftText, rightText);

            for (var i = 0; i < _optionCountObjects.Count; i++)
            {
                _optionCountObjects[i].SetActive(i < optionCount);
            }

            IsEmpty = IsEmpty && optionCount == 0;
        }

        public virtual void UpdateAsStat(StatType type, int totalValue, int count) =>
            UpdateView(
                $"{type.ToString()} {totalValue}",
                string.Empty,
                count);

        public virtual void UpdateAsStat(StatType type, int totalValue, int count, int plusValue) =>
            UpdateView(
                $"{type.ToString()} {totalValue}",
                $"+{plusValue}",
                count);

        public virtual void UpdateBySkill(string skillName, int totalPower, int totalChance) =>
            UpdateView(
                $"{skillName} {totalPower} / {totalChance}%",
                string.Empty,
                1);

        public virtual void UpdateBySkill(string skillName, int totalPower, int totalChance, int plusPower,
            int plusChance) =>
            UpdateView(
                $"{skillName} {totalPower} / {totalChance}%",
                $"+{plusPower} / +{plusChance}%",
                1);

        public override void UpdateToEmpty() =>
            UpdateView(string.Empty, string.Empty, 0);
    }
}
