using FancyScrollView;

namespace Nekoyume.UI.Scroller
{
    public abstract class GridCell<TItemData, TContext> : FancyGridViewCell<TItemData, TContext>
        where TContext : GridScrollDefaultContext, new()
    {
    }
}
