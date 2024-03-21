using System;
using UnityEngine;

namespace Nekoyume.Editor
{
     // Enum을 문자열로 저장하기 위한 어트리뷰트
    public class EnumToStringAttribute : PropertyAttribute
    {
        public Type EnumType;

        public EnumToStringAttribute(Type enumType)
        {
            EnumType = enumType;
        }
    }
}
