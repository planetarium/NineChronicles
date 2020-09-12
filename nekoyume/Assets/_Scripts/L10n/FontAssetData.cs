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

        [SerializeField]
        private float fontSizeOffset = default;

        [SerializeField]
        private float characterSpacing = default;

        [SerializeField]
        private float wordSpacing = default;

        [SerializeField]
        private float lineSpacing = default;

        public TMP_FontAsset FontAsset => fontAsset;

        public IReadOnlyList<FontMaterialData> FontMaterialDataList => fontMaterialDataList;

        public float FontSizeOffset => fontSizeOffset;

        public float CharacterSpacing => characterSpacing;

        public float WordSpacing => wordSpacing;

        public float LineSpacing => lineSpacing;

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
