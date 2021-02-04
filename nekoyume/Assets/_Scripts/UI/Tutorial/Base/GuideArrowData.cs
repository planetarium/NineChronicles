using System;
using UnityEngine;

namespace Nekoyume.UI
{
    [Serializable]
    public class GuideArrowData : ITutorialData
    {
        public TutorialItemType type = TutorialItemType.Arrow;

        public GuideType guideType;

        public RectTransform target;

        public bool isSkip;

        public TutorialItemType Type => type;

        public GuideArrowData(GuideType guideType, RectTransform target, bool isSkip)
        {
            this.guideType = guideType;
            this.target = target;
            this.isSkip = isSkip;
        }
    }
}
