using System;
using System.Collections.Generic;
using System.Globalization;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Model;
using TMPro;
using UniRx;
using UnityEngine;
using ObservableExtensions = UniRx.ObservableExtensions;

namespace Nekoyume.UI.Module
{
    public class RequiredItemView : SimpleCountableItemView
    {
        [SerializeField]
        private TextMeshProUGUI requiredText;

        [SerializeField]
        private GameObject enoughObject;

        private const string CountTextFormatEnough = "{0}/{1}";
        private const string CountTextFormatNotEnough = "<#ff5a5a>{0}</color>/{1}";
        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public int RequiredCount { get; set; } = 1;

        public void SetData(CountableItem model, int requiredCount)
        {
            RequiredCount = requiredCount;
            base.SetData(model);
            _disposables.DisposeAllAndClear();
            ObservableExtensions.Subscribe(touchHandler.OnClick, _ =>
            {
                AudioController.PlayClick();
                var rt = GetComponent<RectTransform>();
                var tooltip = ItemTooltip.Find(model.ItemBase.Value.ItemType);
                tooltip.Show(rt, model.ItemBase.Value, string.Empty, false, null);
            }).AddTo(_disposables);
        }

        protected override void SetCount(int count)
        {
            bool isEnough = count >= RequiredCount;

            countText.text = string.Format(isEnough ?
                CountTextFormatEnough :
                CountTextFormatNotEnough,
                Model.Count.Value, RequiredCount);

            countText.gameObject.SetActive(true);
            requiredText.gameObject.SetActive(false);
            enoughObject.SetActive(isEnough);
        }

        public void SetRequiredText()
        {
            requiredText.text = RequiredCount.ToString(CultureInfo.InvariantCulture);
            requiredText.gameObject.SetActive(true);
            countText.gameObject.SetActive(false);
        }
    }
}
