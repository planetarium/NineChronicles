namespace Nekoyume.UI
{
    public class PreloadingScreen : LoadingScreen
    {
        public override void Close(bool ignoreCloseAnimation = false)
        {
            Find<Synopsis>().Show();
            base.Close(ignoreCloseAnimation);
        }
    }
}
