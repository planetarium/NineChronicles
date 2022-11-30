using System;
using System.Collections.Generic;
using System.Globalization;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class RequiredItemView : SimpleCountableItemView
    {
        [SerializeField]
        private TextMeshProUGUI requiredText;

        [SerializeField]
        private GameObject enoughObject;

        private const string CountTextFormatEnough = "{0}/{1}";
        private const string CountTextFormatNotEnough = "<#ff5a5a>{0}</color>/{1}";

        public int RequiredCount { get; protected set; } = 1;

        public bool HideEnoughObject { get; set; } = false;

        public void SetData(CountableItem model, int requiredCount)
        {
            RequiredCount = requiredCount;
            SetData(model, () => ShowTooltip(model));
        }

        private void ShowTooltip(Item model)
        {
            AudioController.PlayClick();
            var tooltip = ItemTooltip.Find(model.ItemBase.Value.ItemType);
            tooltip.Show(model.ItemBase.Value, string.Empty, false, null);
        }

        protected override void SetCount(int count)
        {
            bool isEnough = count >= RequiredCount;

            countText.text = string.Format(
                isEnough ? CountTextFormatEnough : CountTextFormatNotEnough,
                Model.Count.Value, RequiredCount);

            countText.gameObject.SetActive(true);
            requiredText.gameObject.SetActive(false);
            enoughObject.SetActive(!HideEnoughObject && isEnough);
        }

        public void SetRequiredText()
        {
            requiredText.text = RequiredCount.ToString(CultureInfo.InvariantCulture);
            requiredText.gameObject.SetActive(true);
            countText.gameObject.SetActive(false);
        }
    }
}
