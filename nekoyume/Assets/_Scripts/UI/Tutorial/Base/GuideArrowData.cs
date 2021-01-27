using UnityEngine;

namespace Nekoyume.UI
{
    public class GuideArrowData
    {
        public GuideType GuideType { get; }
        public Vector2 Target { get; }
        public bool IsSkip { get; }
        public System.Action Callback { get; }

        public GuideArrowData(GuideType guideType, Vector2 target, bool isSkip, System.Action callback)
        {
            GuideType = guideType;
            Target = target;
            IsSkip = isSkip;
            Callback = callback;
        }
    }
}
