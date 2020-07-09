using System;
using FancyScrollView;
using QuestModel = Nekoyume.Model.Quest.Quest;

namespace Nekoyume.UI.Scroller
{
    public class QuestScroll : BaseScroll<QuestModel, QuestScroll.ContextModel>
    {
        public class ContextModel : IFancyScrollRectContext
        {
            public ScrollDirection ScrollDirection { get; set; }
            public Func<(float ScrollSize, float ReuseMargin)> CalculateScrollSize { get; set; }
        }
    }
}
