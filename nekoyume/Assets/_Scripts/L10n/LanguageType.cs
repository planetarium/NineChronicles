using System;
using System.Collections.Generic;

namespace Nekoyume.L10n
{
    /// <summary>
    /// UnityEngine.SystemLanguage 안에서 선택적으로 포함합니다.
    /// </summary>
    [Serializable]
    public enum LanguageType
    {
        /// <summary>
        /// font file: Assets/Resources/Font/TTF/PoorStory-Regular.ttf
        /// character file: Assets/Resources/Font/CharacterFiles/KS1001.txt
        /// font asset file: Assets/Resources/Font/SDF/PoorStory-Regular SDF.asset
        /// </summary>
        English,

        /// <summary>
        /// This is same with English.
        /// </summary>
        Korean,

        /// <summary>
        /// font file: Assets/Resources/Font/TTF/PoorStory-Latin.otf
        /// unicode hex range:
        ///     1. 00C0-00D6,00D8-00F6,00F8-00FF
        ///         https://en.wikipedia.org/wiki/Latin-1_Supplement_(Unicode_block)
        /// font asset file: Assets/Resources/Font/SDF/PoorStory-Latin SDF.asset
        /// </summary>
        PortugueseBrazil,

        /// <summary>
        /// This is same with Portuguese.
        /// </summary>
        Polish,

        /// <summary>
        /// font file: Assets/Resources/Font/TTF/NotoSansCJKjp-Regular.otf
        /// unicode hex range:
        ///     1. 20-7E,A0,2026,25A1
        ///     2. ascii・ひらがな・カタカナ・頻出漢字(custom range)
        ///         https://www.youtube.com/watch?v=Dj4XaZJTEQM
        ///         https://gist.github.com/boscohyun/9ca2fc65b0e042bab999c9adce4d4094
        /// font asset file: Assets/Resources/Font/SDF/NotoSansCJKjp-Regular-00-ASCII(98) SDF.asset
        /// </summary>
        Japanese,

        /// <summary>
        /// font file: Assets/Resources/Font/TTF/NotoSansCJKsc-Regular.otf
        /// unicode hex range:
        ///     1. 20-7E,A0,2026,25A1,3001,3002
        ///     2. Assets/Resources/Font/CharacterFiles/simplified-chinese-8105-unicode-range-{00}-{0000}.txt
        ///         http://hanzidb.org/character-list/general-standard
        /// font asset file: Assets/Resources/Font/SDF/NotoSansCJKsc-Regular-00-ASCII(98) SDF.asset
        /// </summary>
        ChineseSimplified,

        /// <summary>
        /// font file: Assets/Resources/Font/TTF/kanit-regular.otf
        /// unicode hex range:
        ///     1. 20-7E,A0,2026,25A1
        ///     2. 0E01-0E3A,0E3F-0E5B
        ///         https://en.wikipedia.org/wiki/Thai_(Unicode_block)
        /// font asset file: Assets/Resources/Font/SDF/kanit-regular-00-ASCII(97) SDF.asset
        /// </summary>
        Thai,
    }

    public class LanguageTypeComparer : IEqualityComparer<LanguageType>
    {
        public static readonly LanguageTypeComparer Instance = new LanguageTypeComparer();

        public bool Equals(LanguageType x, LanguageType y)
        {
            return x == y;
        }

        public int GetHashCode(LanguageType obj)
        {
            return obj.GetHashCode();
        }
    }
}
