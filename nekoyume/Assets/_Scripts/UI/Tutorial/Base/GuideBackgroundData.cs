using System;
using UnityEngine;

namespace Nekoyume.UI
{
    [Serializable]
    public class GuideBackgroundData : ITutorialData
    {
        public TutorialItemType type = TutorialItemType.Background;

        public bool isExistFadeIn;

        public bool isEnableMask;

        public RectTransform target;

        public TutorialItemType Type => type;

        public GuideBackgroundData(
            bool isExistFadeIn,
            bool isEnableMask,
            RectTransform target)
        {
            this.isExistFadeIn = isExistFadeIn;
            this.isEnableMask = isEnableMask;
            this.target = target;
        }
    }
}
