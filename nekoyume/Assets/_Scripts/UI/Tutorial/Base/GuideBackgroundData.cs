using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    [Serializable]
    public class GuideBackgroundData : ITutorialData
    {
        public TutorialItemType type = TutorialItemType.Background;

        public bool isExistFadeIn;

        public bool isEnableMask;

        public RectTransform target;
        public RectTransform buttonRectTransform;
        public bool fullScreenButton;

        public TutorialItemType Type => type;

        public GuideBackgroundData(bool isExistFadeIn,
            bool isEnableMask,
            RectTransform target,
            RectTransform buttonRectTransform,
            bool fullScreenButton)
        {
            this.isExistFadeIn = isExistFadeIn;
            this.isEnableMask = isEnableMask;
            this.target = target;
            this.buttonRectTransform = buttonRectTransform;
            this.fullScreenButton = fullScreenButton;
        }
    }
}
