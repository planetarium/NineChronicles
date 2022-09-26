using Nekoyume.EnumType;

namespace Nekoyume.UI
{
    public class SystemWidget : Widget
    {
        public override WidgetType WidgetType => WidgetType.System;
        public override CloseKeyType CloseKeyType => CloseKeyType.Escape;

        private CapturedImage _capturedImage;

        private UIBackground _background;

        protected override void Awake()
        {
            base.Awake();
            _capturedImage = GetComponentInChildren<CapturedImage>();
            if (_capturedImage != null)
            {
                _capturedImage.OnClick = CloseWidget;
            }

            _background = GetComponentInChildren<UIBackground>();
            if (_background != null)
            {
                _background.OnClick = CloseWidget;
            }
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            if (_capturedImage != null)
            {
                _capturedImage.Show();
                _capturedImage.OnClick = CloseWidget;
            }

            base.Show(ignoreShowAnimation);
        }
    }
}
