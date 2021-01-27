namespace Nekoyume.UI
{
    public class GuideDialogData
    {
        public float TargetHeight { get; }
        public System.Action Callback { get; }

        public GuideDialogData(float targetHeight, System.Action callback)
        {
            TargetHeight = targetHeight;
            this.Callback = callback;
        }
    }
}
