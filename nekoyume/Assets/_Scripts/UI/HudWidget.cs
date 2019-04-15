namespace Nekoyume.UI
{
    public class HudWidget : Widget
    {
        public override void Close()
        {
            Destroy(gameObject);
        }
    }
}
