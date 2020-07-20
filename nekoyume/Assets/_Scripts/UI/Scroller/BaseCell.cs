using FancyScrollView;

namespace Nekoyume.UI.Scroller
{
    public abstract class BaseCell<TItemData, TContext> : FancyScrollRectCell<TItemData, TContext>
        where TContext : class, IFancyScrollRectContext, new()
    {
        public void Show()
        {
            gameObject.SetActive(true);
        }

        public void Show(TItemData itemData)
        {
            UpdateContent(itemData);
            Show();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
