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
        public Button button;

        public TutorialItemType Type => type;

        public GuideBackgroundData(
            bool isExistFadeIn,
            bool isEnableMask,
            RectTransform target,
            Button button)
        {
            this.isExistFadeIn = isExistFadeIn;
            this.isEnableMask = isEnableMask;
            this.target = target;
            this.button = button;
        }
    }
}
