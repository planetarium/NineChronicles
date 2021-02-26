namespace RedBlueGames.Tools.TextTyper
{
    using UnityEngine;
    using System;
    using System.Collections;
    using System.Collections.Generic;

    [Serializable]
    public class ShakePreset
    {
        [Tooltip("Name identifying this preset. Can also be used as a ShakeLibrary indexer key.")]
        public string Name;

        [Range(0, 20)]
        [Tooltip("Amount of x-axis shake to apply during animation")]
        public float xPosStrength = 0f;

        [Range(0, 20)]
        [Tooltip("Amount of y-axis shake to apply during animation")]
        public float yPosStrength = 0f;

        [Range(0, 90)]
        [Tooltip("Amount of rotational shake to apply during animation")]
        public float RotationStrength = 0f;

        [Range(0, 10)]
        [Tooltip("Amount of scale shake to apply during animation")]
        public float ScaleStrength = 0f;
    }

    [CreateAssetMenu(fileName = "ShakeLibrary", menuName = "Text Typer/Shake Library", order = 1)]
    public class ShakeLibrary : ScriptableObject
    {
        public List<ShakePreset> ShakePresets;

        /// <summary>
        /// Get the ShakePreset from this library with the provided key/name
        /// </summary>
        /// <param name="key">Key/name identifying the desired ShakePreset</param>
        /// <returns>Matching ShakePreset</returns>
        public ShakePreset this[string key]
        {
            get
            {
                var preset = this.FindPresetOrNull(key);
                if (preset == null)
                {
                    throw new KeyNotFoundException();
                }
                else
                {
                    return preset;
                }
            }
        }

        public bool ContainsKey(string key)
        {
            return this.FindPresetOrNull(key) != null;
        }

        private ShakePreset FindPresetOrNull(string key)
        {
            foreach (var preset in this.ShakePresets)
            {
                if (preset.Name.ToUpper() == key.ToUpper())
                {
                    return preset;
                }
            }

            return null;
        }
    }
}