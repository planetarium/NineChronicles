using System;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI.Scroller
{
    public class RectScrollDefaultContext : IDisposable, IFancyScrollRectContext
    {
        public ScrollDirection ScrollDirection { get; set; }
        public Func<(float ScrollSize, float ReuseMargin)> CalculateScrollSize { get; set; }

        public virtual void Dispose()
        {
        }
    }
}
