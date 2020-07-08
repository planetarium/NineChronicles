using System;
using FancyScrollView;

namespace Nekoyume.UI.Scroller
{
    public class MailScroll : BaseScroll<Nekoyume.Model.Mail.Mail, MailScroll.ContextModel>
    {
        public class ContextModel : IFancyScrollRectContext
        {
            public ScrollDirection ScrollDirection { get; set; }
            public Func<(float ScrollSize, float ReuseMargin)> CalculateScrollSize { get; set; }
        }
    }
}
