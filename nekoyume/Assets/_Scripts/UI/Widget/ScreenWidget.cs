
using Nekoyume.EnumType;

namespace Nekoyume.UI
{
    public class ScreenWidget : Widget
    {
        public override WidgetType WidgetType => WidgetType.Screen;
        private CapturedImage _capturedImage;
        private UIBackground _background;

        protected override void Awake()
        {
            base.Awake();

            CloseWidget = null;
            _capturedImage = GetComponentInChildren<CapturedImage>();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if (_capturedImage != null)
            {
                _capturedImage.Show();
            }

            base.Show(ignoreShowAnimation);
        }
    }
}
