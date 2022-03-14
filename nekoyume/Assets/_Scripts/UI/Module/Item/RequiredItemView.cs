using System.Globalization;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Model;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class RequiredItemView : SimpleCountableItemView
    {
        [SerializeField]
        private TextMeshProUGUI requiredText;

        [SerializeField]
        private GameObject enoughObject;

        protected const string CountTextFormatEnough = "{0}/{1}";
        protected const string CountTextFormatNotEnough = "<#ff5a5a>{0}</color>/{1}";

        public int RequiredCount { get; set; } = 1;

        public void SetData(CountableItem model, int requiredCount)
        {
            RequiredCount = requiredCount;
            base.SetData(model);
            touchHandler.OnClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                var rt = GetComponent<RectTransform>();
                var tooltip = ItemTooltip.Find(model.ItemBase.Value.ItemType);
                var item = new InventoryItem(model.ItemBase.Value, 0, true, false, true);
                tooltip.Show(rt, item, string.Empty, false, null);
            }).AddTo(gameObject);
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
