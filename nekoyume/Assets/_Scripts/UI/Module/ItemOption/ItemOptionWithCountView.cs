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

        public void Show(
            string leftText,
            string rightText,
            int optionCount,
            bool ignoreAnimation = false)
        {
            UpdateView(leftText, rightText, optionCount);
            Show(ignoreAnimation);
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

        public virtual void UpdateAsStatWithCount(StatType type, int value, int count) =>
            UpdateView(
                $"{type.ToString()} {value}",
                string.Empty,
                count);

        public virtual void UpdateAsTotalAndPlusStatWithCount(StatType type, int totalValue, int count, int plusValue) =>
            UpdateView(
                $"{type.ToString()} {totalValue}",
                $"+{plusValue}",
                count);

        public virtual void UpdateAsSkill(string skillName, int totalPower, int totalChance) =>
            UpdateView(
                $"{skillName} {totalPower} / {totalChance}%",
                string.Empty,
                1);

        public virtual void UpdateAsTotalAndPlusSkill(string skillName, int totalPower, int totalChance, int plusPower,
            int plusChance) =>
            UpdateView(
                $"{skillName} {totalPower} / {totalChance}%",
                $"+{plusPower} / +{plusChance}%",
                1);

        public override void UpdateToEmpty() =>
            UpdateView(string.Empty, string.Empty, 0);
    }
}
