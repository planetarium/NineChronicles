using System;
using FancyScrollView;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Scroller
{
    public abstract class GridScrollDefaultContext : IFancyGridViewContext
    {
        public ScrollDirection ScrollDirection { get; set; }
        public Func<(float ScrollSize, float ReuseMargin)> CalculateScrollSize { get; set; }
        public GameObject CellTemplate { get; set; }
        public Func<int> GetGroupCount { get; set; }
        public Func<float> GetStartAxisSpacing { get; set; }
        public Func<float> GetCellSize { get; set; }
    }
}
