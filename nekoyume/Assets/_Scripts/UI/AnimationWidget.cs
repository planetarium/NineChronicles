using Nekoyume.EnumType;

namespace Nekoyume.UI
{
    public class AnimationWidget : Widget
    { 
        public override WidgetType WidgetType => WidgetType.Animation;
        public bool IsPlaying { get; protected set; }
        protected float _animationTime;
        private static int _animationCount;

        protected override void Awake()
        {
            base.Awake();

            CloseWidget = null;
        }

        public override void Show()
        {
            base.Show();
            if (!(this is AnimationScreen))
            {
                Widget.Find<AnimationScreen>().Show();
                _animationCount++;
            }
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            if (this is AnimationScreen)
            {
                gameObject.SetActive(false);
                return;
            }

            if (--_animationCount <= 0)
            {
                Widget.Find<AnimationScreen>().Close();
            }
            Destroy(gameObject);
        }
    }
}
