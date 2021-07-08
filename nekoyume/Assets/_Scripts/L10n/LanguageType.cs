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
        /// font file: Assets/Font/TTF/PoorStory-Regular.ttf
        /// font asset file: Assets/Resources/Font/SDF/English SDF.asset
        ///     - Sampling Font Size: 80
        ///     - Padding: 9
        ///     - Packing Method: Fast
        ///     - Atlas Resolution: 1024x1024
        ///     - Character Set: Unicode Range (Hex)
        ///     - Character File: Assets/Font/CharacterFiles/English-unicode-hex-range-01.txt
        ///     - Render Mode: SDFAA
        ///     - Get Kerning Pairs: true
        /// </summary>
        English,

        /// <summary>
        /// font file: Assets/Font/TTF/PoorStory-Regular.ttf
        /// font asset file: Assets/Resources/Font/SDF/Korean SDF.asset
        ///     - Sampling Font Size: 80
        ///     - Padding: 9
        ///     - Packing Method: Fast
        ///     - Atlas Resolution: 4096x4096
        ///     - Character Set: Unicode Range (Hex)
        ///     - Character File: Assets/Font/CharacterFiles/Korean-unicode-hex-range-01.txt
        ///     - Render Mode: SDFAA
        ///     - Get Kerning Pairs: true
        /// </summary>
        Korean,

        /// <summary>
        /// font file: Assets/Font/TTF/PoorStory-Latin.otf
        /// font asset file: Assets/Resources/Font/SDF/PortugueseBrazil SDF.asset
        ///     - Sampling Font Size: 80
        ///     - Padding: 9
        ///     - Packing Method: Fast
        ///     - Atlas Resolution: 1024x1024
        ///     - Character Set: Unicode Range (Hex)
        ///     - Character File: Assets/Font/CharacterFiles/PortugueseBrazil-unicode-hex-range-01.txt
        ///     - Render Mode: SDFAA
        ///     - Get Kerning Pairs: true
        /// </summary>
        PortugueseBrazil,

        /// <summary>
        /// This is same with PortugueseBrazil.
        /// </summary>
        Polish,

        /// <summary>
        /// font file: Assets/Font/TTF/NotoSansCJKjp-Regular.otf
        /// font asset file 1: Assets/Resources/Font/SDF/Japanese SDF.asset
        ///     - Sampling Font Size: 80
        ///     - Padding: 7
        ///     - Packing Method: Fast
        ///     - Atlas Resolution: 4096x4096
        ///     - Character Set: Unicode Range (Hex)
        ///     - Character File: Assets/Font/CharacterFiles/Japanese-unicode-hex-range-01.txt
        ///     - Render Mode: SDFAA
        ///     - Get Kerning Pairs: true
        /// </summary>
        Japanese,

        /// <summary>
        /// font file: Assets/Font/TTF/NotoSansCJKsc-Regular.otf
        /// font asset file: Assets/Resources/Font/SDF/ChineseSimplified SDF.asset
        ///     - Sampling Font Size: 80
        ///     - Padding: 7
        ///     - Packing Method: Fast
        ///     - Atlas Resolution: 4096x4096
        ///     - Character Set: Unicode Range (Hex)
        ///     - Character File: Assets/Font/CharacterFiles/ChineseSimplified-unicode-hex-range-01.txt
        ///     - Render Mode: SDFAA
        ///     - Get Kerning Pairs: true
        /// </summary>
        ChineseSimplified,

        /// <summary>
        /// font file: Assets/Font/TTF/kanit-regular.otf
        /// font asset file: Assets/Resources/Font/SDF/Thai SDF.asset
        ///     - Sampling Font Size: 80
        ///     - Padding: 7
        ///     - Packing Method: Fast
        ///     - Atlas Resolution: 1024x1024
        ///     - Character Set: Unicode Range (Hex)
        ///     - Character File: Assets/Font/CharacterFiles/Thai-unicode-hex-range-01.txt
        ///     - Render Mode: SDFAA
        ///     - Get Kerning Pairs: true
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

    public class LanguageTypeMapper
    {
        /** FIXME
         * 현재 Unity Player에서는 iso396 표준이 아닌, 일종의 방언을 사용하고 있습니다.
         * 따라서 인자로 받는 iso396-1 코드를 방언으로 변환시켜주는 해당 매퍼가 필요한데,
         * LanguageType enum을 CultureInfo(https://docs.microsoft.com/en-us/dotnet/api/system.globalization.cultureinfo?view=netstandard-2.0)
         * 로 리팩토링한다면 훨씬 깔끔해 질 것 같습니다.
         * https://github.com/planetarium/nekoyume-unity/pull/2835#discussion_r493197244
         */

        public static LanguageType ISO396(string iso396)
        {
            iso396 = iso396.Replace("_", "-");
            switch (iso396)
            {
                case "ko":
                    return LanguageType.Korean;
                case "en":
                    return LanguageType.English;
                case "pt-BR":
                    return LanguageType.PortugueseBrazil;
                case "pl":
                    return LanguageType.Polish;
                case "ja":
                    return LanguageType.Japanese;
                case "zh-Hans":
                    return LanguageType.ChineseSimplified;
                case "th":
                    return LanguageType.Thai;
                default:
                    Debug.LogWarning($"Does not support LanguageType for {iso396}");
                    return LanguageType.English;
            }
        }
    }
}
