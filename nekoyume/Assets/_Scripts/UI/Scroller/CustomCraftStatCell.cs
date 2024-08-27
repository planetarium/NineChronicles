using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class CustomCraftStatCell : RectCell<CustomCraftStatCell.Model,CustomCraftStatScroll.ContextModel>
    {
        public class Model
        {
            public string CompositionString;
            public string SubStatTotalString;
        }

        [SerializeField]
        private TextMeshProUGUI compositionText;

        [SerializeField]
        private TextMeshProUGUI subStatTotalText;

        public override void UpdateContent(Model itemData)
        {
            compositionText.SetText(itemData.CompositionString);
            subStatTotalText.SetText(itemData.SubStatTotalString);
        }
    }
}
