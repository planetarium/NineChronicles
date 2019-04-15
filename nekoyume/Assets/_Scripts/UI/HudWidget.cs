using Nekoyume.UI;

namespace _Scripts.UI
{
    public class HudWidget : Widget
    {
        public override void Close()
        {
            Destroy(gameObject);
        }
    }
}
