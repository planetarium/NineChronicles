namespace RedBlueGames.Tools.TextTyper
{
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.UI;

    [RequireComponent(typeof(TextMeshProUGUI))]
    public class ShakeAnimation : TextAnimation
    {
        [SerializeField]
        [Tooltip("The library of ShakePresets that can be used by this component.")]
        private ShakeLibrary shakeLibrary;

        [SerializeField]
        [Tooltip("The name (key) of the ShakePreset this animation should use.")]
        private string shakePresetKey;

        private ShakePreset shakePreset;

        /// <summary>
        /// Load a particular ShakePreset animation into this Component
        /// </summary>
        /// <param name="library">The library of ShakePresets that can be used by this component</param>
        /// <param name="presetKey">The name (key) of the ShakePreset this animation should use</param>
        public void LoadPreset(ShakeLibrary library, string presetKey)
        {
            this.shakeLibrary = library;
            this.shakePresetKey = presetKey;
            this.shakePreset = library[presetKey];
        }

        protected override void OnEnable()
        {
            if (this.shakeLibrary != null && !string.IsNullOrEmpty(this.shakePresetKey))
            {
                LoadPreset(this.shakeLibrary, this.shakePresetKey);
            }

            base.OnEnable();
        }

        protected override void Animate(int characterIndex, out Vector2 translation, out float rotation, out float scale)
        {
            translation = Vector2.zero;
            rotation = 0f;
            scale = 1f;

            // Do nothing if a ShakePreset has not been configured yet
            if (this.shakePreset == null)
            {
                return;
            }

            if (characterIndex >= this.FirstCharToAnimate && characterIndex <= this.LastCharToAnimate)
            {
                float randomX = Random.Range(-this.shakePreset.xPosStrength, this.shakePreset.xPosStrength);
                float randomY = Random.Range(-this.shakePreset.yPosStrength, this.shakePreset.yPosStrength);
                translation = new Vector2(randomX, randomY);

                rotation = Random.Range(-this.shakePreset.RotationStrength, this.shakePreset.RotationStrength);

                scale = 1f + Random.Range(-this.shakePreset.ScaleStrength, this.shakePreset.ScaleStrength);
            }
        }
    }
}
