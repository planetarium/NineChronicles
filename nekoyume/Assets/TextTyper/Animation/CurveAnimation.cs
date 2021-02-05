namespace RedBlueGames.Tools.TextTyper
{
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    [RequireComponent(typeof(TextMeshProUGUI))]
    public class CurveAnimation : TextAnimation
    {
        [SerializeField]
        [Tooltip("The library of CurvePresets that can be used by this component.")]
        private CurveLibrary curveLibrary;

        [SerializeField]
        [Tooltip("The name (key) of the CurvePreset this animation should use.")]
        private string curvePresetKey;

        private CurvePreset curvePreset;

        private float timeAnimationStarted;

        /// <summary>
        /// Load a particular CurvePreset animation into this Component
        /// </summary>
        /// <param name="library">The library of CurvePresets that can be used by this component</param>
        /// <param name="presetKey">The name (key) of the CurvePreset this animation should use</param>
        public void LoadPreset(CurveLibrary library, string presetKey)
        {
            this.curveLibrary = library;
            this.curvePresetKey = presetKey;
            this.curvePreset = library[presetKey];
        }

        protected override void OnEnable()
        {
            if (this.curveLibrary != null && !string.IsNullOrEmpty(this.curvePresetKey))
            {
                LoadPreset(this.curveLibrary, this.curvePresetKey);
            }

            this.timeAnimationStarted = this.TimeForTimeScale;
            base.OnEnable();
        }

        protected override void Animate(int characterIndex, out Vector2 translation, out float rotation, out float scale)
        {
            translation = Vector2.zero;
            rotation = 0f;
            scale = 1f;

            // Do nothing if a CurvePreset has not been configured yet
            if (this.curvePreset == null)
            {
                return;
            }

            if (characterIndex >= this.FirstCharToAnimate && characterIndex <= this.LastCharToAnimate)
            {
                // Calculate a t based on time since the animation started, 
                // but offset per character (to produce wave effects)
                float t = this.TimeForTimeScale - this.timeAnimationStarted + (characterIndex * this.curvePreset.timeOffsetPerChar);

                float xPos = this.curvePreset.xPosCurve.Evaluate(t) * this.curvePreset.xPosMultiplier;
                float yPos = this.curvePreset.yPosCurve.Evaluate(t) * this.curvePreset.yPosMultiplier;

                translation = new Vector2(xPos, yPos);

                rotation = this.curvePreset.rotationCurve.Evaluate(t) * this.curvePreset.rotationMultiplier;
                scale += this.curvePreset.scaleCurve.Evaluate(t) * this.curvePreset.scaleMultiplier;
            }
        }
    }
}
