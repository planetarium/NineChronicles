using System;
using FancyScrollView;

namespace Nekoyume.UI.Scroller
{
    public class MailScroll : RectScroll<Nekoyume.Model.Mail.Mail, MailScroll.ContextModel>
    {
        public class ContextModel : RectScrollDefaultContext, IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
