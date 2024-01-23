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

        public void UpdateAsTotalAndPlusStatWithCount(StatType type, long totalValue, long plusValue, int count)
        {
            var totalValueString = type.ValueToString(totalValue);
            var plusValueString = type.ValueToString(plusValue);

            UpdateView(
                $"{type} {totalValueString}",
                plusValue > 0 ? $"+{plusValueString}" : string.Empty,
                count);
        }

        public override void UpdateToEmpty() =>
            UpdateView(string.Empty, string.Empty, 0);
    }
}
