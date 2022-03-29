using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Nekoyume.L10n
{
    [CreateAssetMenu(
        fileName = "FontAssetGenerationSettings",
        menuName = "ScriptableObjects/L10n/Spawn FontAssetGenerationSettings",
        order = 1)]
    public class FontAssetGenerationSettings : ScriptableObject
    {
        [SerializeField] 
        public List<FontAssetGenerationSetting> settings;
    }

    [Serializable]
    public struct FontAssetGenerationSetting
    {
        public LanguageType language;
        public Font sourceFontFile;
        public int pointSizeSamplingMode; // 0: Auto, 1: Custom
        public int pointSize;
        public int padding;
        public int packingMode; // 0: Fast, 4: Optimum
        public int atlasWidth;
        public int atlasHeight;
        public int characterSetSelectionMode; // 6: Unicode Range (Hex)
        public TMP_FontAsset referencedFontAsset;
        public string characterSequence; // Fill Auto
        public GlyphRenderMode renderMode;
        public bool includeFontFeatures;
    }
}