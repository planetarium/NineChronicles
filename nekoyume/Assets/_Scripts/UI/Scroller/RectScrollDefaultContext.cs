using System;
using FancyScrollView;

namespace Nekoyume.UI.Scroller
{
    public abstract class RectScrollDefaultContext : IFancyScrollRectContext
    {
        public ScrollDirection ScrollDirection { get; set; }
        public Func<(float ScrollSize, float ReuseMargin)> CalculateScrollSize { get; set; }
    }
}
