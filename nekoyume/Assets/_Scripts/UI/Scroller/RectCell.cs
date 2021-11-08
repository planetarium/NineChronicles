using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Scroller
{
    public abstract class RectCell<TItemData, TContext> : FancyScrollRectCell<TItemData, TContext>
        where TItemData : class
        where TContext : class, IFancyScrollRectContext, new()
    {
        public void Show(TItemData itemData)
        {
            UpdateContent(itemData);
            SetVisible(true);
        }

        public void Hide()
        {
            SetVisible(false);
        }
    }
}
