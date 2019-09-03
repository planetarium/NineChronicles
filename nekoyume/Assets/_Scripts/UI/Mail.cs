using Nekoyume.BlockChain;
using Nekoyume.UI.Scroller;

namespace Nekoyume.UI
{
    public class Mail : Widget
    {
        public MailScrollerController scroller;

        public override void Show()
        {
            var mailBox = States.Instance.currentAvatarState.Value.mailBox;
            scroller.SetData(mailBox);
            base.Show();
        }
    }
}
