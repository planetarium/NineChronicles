using UnityEngine;

namespace Nekoyume.UI
{
    public class GuideArrowData : ITutorialData
    {
        public TutorialItemType Type { get; } = TutorialItemType.Arrow;
        public GuideType GuideType { get; }
        public RectTransform Target { get; }
        public bool IsSkip { get; }

        public GuideArrowData(GuideType guideType, RectTransform target, bool isSkip)
        {
            GuideType = guideType;
            Target = target;
            IsSkip = isSkip;
        }
    }
}
