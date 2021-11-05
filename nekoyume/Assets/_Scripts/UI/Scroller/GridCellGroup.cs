using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Scroller
{
    public abstract class GridCellGroup<TItemData, TContext> : FancyCellGroup<TItemData, TContext>
        where TContext : class, IFancyCellGroupContext, new()
    {
    }
}
