using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.L10n
{
    /// <summary>
    /// UnityEngine.SystemLanguage 안에서 선택적으로 포함합니다.
    /// </summary>
    [Serializable]
    public enum LanguageType
    {
        /// <summary>
        /// font file: Assets/Font/TTF/NotoSansCJKsc-Regular.ttf
        /// font asset file: Assets/Resources/Font/SDF/English SDF.asset
        ///     - Sampling Font Size: 40
        ///     - Padding: 5
        ///     - Packing Method: Fast
        ///     - Atlas Resolution: 512x512
        ///     - Character Set: Unicode Range (Hex)
        ///     - Character File: Assets/Font/CharacterFiles/English-unicode-hex-range-01.txt
        ///     - Render Mode: SDFAA
        ///     - Get Kerning Pairs: true
	///	- Size: 0.9
        /// </summary>
        English,

        /// <summary>
        /// font file: Assets/Font/TTF/goyang.ttf
        /// font asset file: Assets/Resources/Font/SDF/Korean SDF.asset
        ///     - Sampling Font Size: 40
        ///     - Padding: 5
        ///     - Packing Method: Fast
        ///     - Atlas Resolution: 2048x2048
        ///     - Character Set: Unicode Range (Hex)
        ///     - Character File: Assets/Font/CharacterFiles/Korean-unicode-hex-range-01.txt
        ///     - Render Mode: SDFAA
        ///     - Get Kerning Pairs: true
        /// </summary>
        Korean,

        /// <summary>
        /// font file: Assets/Font/TTF/NotoSans-Regular.ttf
        /// font asset file: Assets/Resources/Font/SDF/Portuguese SDF.asset
        ///     - Sampling Font Size: 40
        ///     - Padding: 5
        ///     - Packing Method: Fast
        ///     - Atlas Resolution: 512x512
        ///     - Character Set: Unicode Range (Hex)
        ///     - Character File: Assets/Font/CharacterFiles/PortugueseBrazil-unicode-hex-range-01.txt
        ///     - Render Mode: SDFAA
        ///     - Get Kerning Pairs: true
        /// </summary>
        Portuguese,

        /// <summary>
        /// font file: Assets/Font/TTF/NotoSansCJKjp-Regular.otf
        /// font asset file 1: Assets/Resources/Font/SDF/Japanese SDF.asset
        ///     - Sampling Font Size: 40
        ///     - Padding: 5
        ///     - Packing Method: Fast
        ///     - Atlas Resolution: 2048x2048
        ///     - Character Set: Unicode Range (Hex)
        ///     - Character File: Assets/Font/CharacterFiles/Japanese-unicode-hex-range-01.txt
        ///     - Render Mode: SDFAA
        ///     - Get Kerning Pairs: true
        /// </summary>
        Japanese,

        /// <summary>
        /// font file: Assets/Font/TTF/NotoSansCJKsc-Regular.otf
        /// font asset file: Assets/Resources/Font/SDF/ChineseSimplified SDF.asset
        ///     - Sampling Font Size: 40
        ///     - Padding: 5
        ///     - Packing Method: Fast
        ///     - Atlas Resolution: 2048x2048
        ///     - Character Set: Unicode Range (Hex)
        ///     - Character File: Assets/Font/CharacterFiles/ChineseSimplified-unicode-hex-range-01.txt
        ///     - Render Mode: SDFAA
        ///     - Get Kerning Pairs: true
        /// </summary>
        ChineseSimplified,

        /// <summary>
        /// font file: Assets/Font/TTF/NotoSansThai_Regular.orf
        /// font asset file: Assets/Resources/Font/SDF/Thai SDF.asset
        ///     - Sampling Font Size: 40
        ///     - Padding: 5
        ///     - Packing Method: Fast
        ///     - Atlas Resolution: 512x512
        ///     - Character Set: Unicode Range (Hex)
        ///     - Character File: Assets/Font/CharacterFiles/Thai-unicode-hex-range-01.txt
        ///     - Render Mode: SDFAA
        ///     - Get Kerning Pairs: true
        /// </summary>
        Thai,

        Spanish,

        Indonesian,

        Russian,

        ChineseTraditional,

        Tagalog,

        Vietnam,
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

    public class LanguageTypeMapper
    {
        /** FIXME
         * 현재 Unity Player에서는 iso639 표준이 아닌, 일종의 방언을 사용하고 있습니다.
         * 따라서 인자로 받는 iso639-1 코드를 방언으로 변환시켜주는 해당 매퍼가 필요한데,
         * LanguageType enum을 CultureInfo(https://docs.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo?view=netstandard-2.0)
         * 로 리팩토링한다면 훨씬 깔끔해 질 것 같습니다.
         * https://github.com/planetarium/nekoyume-unity/pull/2835#discussion_r493197244
         */

        public static LanguageType ISO639(string iso396)
        {
            iso396 = iso396.Replace("_", "-");
            switch (iso396)
            {
                case "ko":
                    return LanguageType.Korean;
                case "en":
                    return LanguageType.English;
                case "pt-BR":
                    return LanguageType.Portuguese;
                case "ja":
                    return LanguageType.Japanese;
                case "zh-Hans":
                    return LanguageType.ChineseSimplified;
                case "th":
                    return LanguageType.Thai;
                case "es":
                    return LanguageType.Spanish;
                case "id":
                    return LanguageType.Indonesian;
                case "ru":
                    return LanguageType.Russian;
                case "zh-Hant":
                    return LanguageType.ChineseTraditional;
                case "tl":
                    return LanguageType.Tagalog;
                case "vi":
                    return LanguageType.Vietnam;
                default:
                    NcDebug.LogWarning($"Does not support LanguageType for {iso396}");
                    return LanguageType.English;
            }
        }
    }
}
