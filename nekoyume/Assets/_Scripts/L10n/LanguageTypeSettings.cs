using System;
using TMPro;

namespace Nekoyume.L10n
{
    [Serializable]
    public struct LanguageTypeSettings
    {
        public LanguageType languageType;
        public TMP_FontAsset fontAsset;
        public float fontSizeOffset;
    }
}
