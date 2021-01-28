using UnityEngine;

namespace Nekoyume.UI
{
    public class GuideArrowData : ITutorialData
    {
        public TutorialIemType Type { get; } = TutorialIemType.Arrow;
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
