#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Nekoyume.Editor
{
    [CustomPropertyDrawer(typeof(EnumToStringAttribute))]
    public class EnumToStringDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var enumToStringAttribute = (EnumToStringAttribute)attribute;

            if (property.propertyType == SerializedPropertyType.String)
            {
                Enum selectedEnum = null;

                // 문자열이 비어있지 않은 경우 파싱 시도
                if (!string.IsNullOrEmpty(property.stringValue))
                {
                    selectedEnum = (Enum)Enum.Parse(enumToStringAttribute.EnumType, property.stringValue);
                }

                // 파싱에 실패하거나 문자열이 비어있는 경우, 기본값 설정
                if (selectedEnum == null)
                {
                    selectedEnum = (Enum)Enum.GetValues(enumToStringAttribute.EnumType).GetValue(0);
                    property.stringValue = selectedEnum.ToString();
                }

                selectedEnum = EditorGUI.EnumPopup(position, label, selectedEnum);
                property.stringValue = selectedEnum.ToString();
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use EnumToString with string.");
            }
        }
    }
}
#endif
