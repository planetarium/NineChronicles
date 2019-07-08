using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public abstract class VerticalTooltipWidget<T> : TooltipWidget<T> where T : Model.Tooltip
    {
        public VerticalLayoutGroup verticalLayoutGroup;

        protected override void UpdateAnchoredPosition()
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform) verticalLayoutGroup.transform);
            base.UpdateAnchoredPosition();
        }
    }
}
