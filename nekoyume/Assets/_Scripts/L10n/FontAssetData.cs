using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace Nekoyume.L10n
{
    [CreateAssetMenu(
        fileName = "FontAssetData",
        menuName = "ScriptableObjects/L10n/Spawn FontAssetData",
        order = 1)]
    public class FontAssetData : ScriptableObject
    {
        [SerializeField]
        private TMP_FontAsset fontAsset = null;

        [SerializeField]
        private List<FontMaterialData> fontMaterialDataList = null;

        [Header("Main Settings")]
        [SerializeField]
        private bool setFontStyleBoldToDisabledAsForced = default;

        [SerializeField]
        private float fontSizeOffset = default;

        [Header("Spacing Offsets")]
        [SerializeField]
        private float characterSpacingOffset = default;

        [SerializeField]
        private float wordSpacingOffset = default;

        [SerializeField]
        private float lineSpacingOffset = default;

        [Header("Extra Settings")]
        [SerializeField]
        private float marginBottom = default;

        public TMP_FontAsset FontAsset => fontAsset;

        public IReadOnlyList<FontMaterialData> FontMaterialDataList => fontMaterialDataList;

        public bool SetFontStyleBoldToDisabledAsForced => setFontStyleBoldToDisabledAsForced;

        public float FontSizeOffset => fontSizeOffset;

        public float CharacterSpacingOffset => characterSpacingOffset;

        public float WordSpacingOffset => wordSpacingOffset;

        public float LineSpacingOffset => lineSpacingOffset;

        public float MarginBottom => marginBottom;

        public FontAssetData()
        {
            fontMaterialDataList = new List<FontMaterialData>();

            var fontMaterialType = typeof(FontMaterialType);
            var fontMaterialTypes = Enum
                .GetNames(fontMaterialType)
                .Select(fontMaterialTypeName =>
                    (FontMaterialType) Enum.Parse(fontMaterialType, fontMaterialTypeName))
                .ToList();
            for (var j = 0; j < fontMaterialTypes.Count; j++)
            {
                fontMaterialDataList.Add(new FontMaterialData
                {
                    type = fontMaterialTypes[j]
                });
            }
        }
    }
}
