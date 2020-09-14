using Nekoyume.EnumType;

namespace Nekoyume.UI
{
    public class AnimationWidget : Widget
    {
        public override WidgetType WidgetType => WidgetType.Animation;
        public bool IsPlaying { get; protected set; }
        protected float _animationTime;

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
