using System;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    [Serializable]
    public class GuideBackgroundData : ITutorialData
    {
        public TutorialItemType type = TutorialItemType.Background;

        /// <summary>
        /// Padding to be applied to the masking
        /// X = Left
        /// Y = Bottom
        /// Z = Right
        /// W = Top
        /// </summary>
        public Vector4 buttonRaycastPadding;
        public Vector2 targetPositionOffset;
        public Vector2 targetSizeOffset;

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
            bool fullScreenButton,
            Vector4 buttonRaycastPadding,
            Vector2 targetPositionOffset,
            Vector2 targetSizeOffset)
        {
            this.isExistFadeIn = isExistFadeIn;
            this.isEnableMask = isEnableMask;
            this.target = target;
            this.buttonRectTransform = buttonRectTransform;
            this.fullScreenButton = fullScreenButton;
            this.buttonRaycastPadding = buttonRaycastPadding;
            this.targetPositionOffset = targetPositionOffset;
            this.targetSizeOffset = targetSizeOffset;
        }
    }
}
