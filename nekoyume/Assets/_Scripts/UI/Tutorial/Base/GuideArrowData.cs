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

        public Vector2 targetPositionOffset;

        public Vector2 targetSizeOffset;

        public float arrowAdditionalDelay;

        public bool isSkip;

        public TutorialItemType Type => type;

        public GuideArrowData(
            GuideType guideType,
            RectTransform target,
            Vector2 targetPositionOffset,
            Vector2 targetSizeOffset,
            float arrowAdditionalDelay,
            bool isSkip)
        {
            this.guideType = guideType;
            this.target = target;
            this.targetPositionOffset = targetPositionOffset;
            this.targetSizeOffset = targetSizeOffset;
            this.arrowAdditionalDelay = arrowAdditionalDelay;
            this.isSkip = isSkip;
        }
    }
}
