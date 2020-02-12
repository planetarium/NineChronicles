using Nekoyume.EnumType;

namespace Nekoyume.UI
{
    class AnimationWidget:Widget
    {
        public override WidgetType WidgetType => WidgetType.Animation;

        protected override void Awake()
        {
            base.Awake();

            CloseWidget = null;
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            Destroy(gameObject);
        }
    }
}