#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Nekoyume.Editor
{
    [CustomPropertyDrawer(typeof(ShowOnAttribute), true)]
    public class ShowOnAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attribute = this.attribute as ShowOnAttribute;
            var show = GetCondition(property.serializedObject.targetObject);
            if (show)
            {
                EditorGUI.PropertyField(position, property, label, true);
                return;
            }

            if (attribute.DrawOption == ShowOnAttribute.PropertyDrawOption.Disable)
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.PropertyField(position, property, label, true);
                EditorGUI.EndDisabledGroup();
            }
        }

        private bool GetCondition(object target)
        {
            var attribute = this.attribute as ShowOnAttribute;
            var field = GetField(target, x => x.Name.Equals(attribute.Condition));
            return field == null || (bool) field.GetValue(target);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var show = GetCondition(property.serializedObject.targetObject);
            if (show)
            {
                return base.GetPropertyHeight(property, label);
            }

            var attribute = this.attribute as ShowOnAttribute;
            return attribute.DrawOption == ShowOnAttribute.PropertyDrawOption.Disable
                ? base.GetPropertyHeight(property, label) : 0;
        }


        private static FieldInfo GetField(
            object target,
            Func<FieldInfo, bool> predicate)
        {
            List<Type> types = new List<Type>()
            {
                target.GetType()
            };

            while (types.Last().BaseType != null)
            {
                types.Add(types.Last().BaseType);
            }

            for (int i = types.Count - 1; i >= 0; i--)
            {
                IEnumerable<FieldInfo> fieldInfos = types[i].GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.NonPublic |
                    BindingFlags.Public |
                    BindingFlags.DeclaredOnly)
                    .Where(predicate);

                if (fieldInfos.Any())
                {
                    return fieldInfos.First();
                }
            }
            return null;
        }

    }
}
#endif
