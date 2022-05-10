#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Nekoyume.Editor
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ShowOnAttribute : PropertyAttribute
    {
        public enum PropertyDrawOption
        {
            Disable = 0,
            Hide,
        }

        public string Condition { get; private set; }

        public PropertyDrawOption DrawOption { get; private set; }

        public ShowOnAttribute(string condition, PropertyDrawOption drawOption = PropertyDrawOption.Disable)
        {
            Condition = condition;
            DrawOption = drawOption;
        }
    }
}
#endif
