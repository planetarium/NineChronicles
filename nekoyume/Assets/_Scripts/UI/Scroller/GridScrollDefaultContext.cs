using System;
using UnityEngine.UI.Extensions;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public class GridScrollDefaultContext : IDisposable, IFancyGridViewContext
    {
        public ScrollDirection ScrollDirection { get; set; }
        public Func<(float ScrollSize, float ReuseMargin)> CalculateScrollSize { get; set; }
        public GameObject CellTemplate { get; set; }
        public Func<int> GetGroupCount { get; set; }
        public Func<float> GetStartAxisSpacing { get; set; }
        public Func<float> GetCellSize { get; set; }

        public virtual void Dispose()
        {
        }
    }
}
