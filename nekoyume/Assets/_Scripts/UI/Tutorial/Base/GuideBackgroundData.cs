using UnityEngine;

namespace Nekoyume.UI
{
    public class GuideBackgroundData
    {
        public bool IsExistFadeIn { get; }
        public bool IsEnableMask { get; }
        public Vector2 Target { get; }
        public System.Action Callback { get; }

        public GuideBackgroundData(bool isExistFadeIn, bool isEnableMask, Vector2 target, System.Action callback)
        {
            IsExistFadeIn = isExistFadeIn;
            IsEnableMask = isEnableMask;
            Target = target;
            Callback = callback;
        }
    }
}
