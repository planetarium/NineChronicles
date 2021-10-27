using System;
using System.Linq;
using System.Collections;
using UnityEditor;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
[CustomEditor(typeof(DOTweenBase), true)]
    public class DOTweenBaseInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var tween = target as DOTweenBase;
            if (tween.TweenType_ == DOTweenBase.TweenType.Forward
                || tween.TweenType_ == DOTweenBase.TweenType.Reverse
                || tween.TweenType_ == DOTweenBase.TweenType.PingPongOnce)
            {
                GUILayout.Space(4.0f);
                GUILayout.Label ("[Tween Complete Callback]");
                tween.Target = (GameObject)EditorGUILayout.ObjectField("GameObject", tween.Target, typeof(GameObject), true);
                if (tween.Target)
                {
                    DrawGUIComponents();
                }
                else
                {
                    tween.CompleteMethod = "";
                }
                tween.CompleteDelay = EditorGUILayout.FloatField("CompleteDelay", tween.CompleteDelay);
            }

            DrawGUIRectTransformMoveTo();
        }

        private void DrawGUIComponents()
        {
            var tween = target as DOTweenBase;
            var components = tween.Target.GetComponents(typeof(Component));
            if (components.Length == 0)
                return;
            string[] options = components.Select((x, i) => $"[{i}] " + x.GetType().ToString()).ToArray();
            int selected = tween.ComponentIndex;
            selected = selected < 0 ? 0 : selected;
            selected = EditorGUILayout.Popup("Component", selected, options);
            tween.ComponentIndex = selected;

            if (tween.ComponentIndex >= 0)
            {
                DrawGUIComponentMethods(components[selected]);
            }
        }

        private void DrawGUIComponentMethods(Component component)
        {
            var tween = target as DOTweenBase;
            var methodInfos = component.GetType().GetMethods(
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.DeclaredOnly);
            string[] options = methodInfos.Select(x => x.Name).ToArray();
            int selected = Array.IndexOf(options, tween.CompleteMethod);
            selected = selected < 0 ? 0 : selected;
            selected = EditorGUILayout.Popup("Method", selected, options);
            if (options.Length == 0)
                return;
            tween.CompleteMethod = options[selected];
        }

        private void DrawGUIRectTransformMoveTo()
        {
            var rectMoveToTween = target as DOTweenRectTransformMoveTo;
            if (!rectMoveToTween)
                return;

            GUILayout.Space(4.0f);
            GUILayout.Label ("[MoveTo Control]");
            if (GUILayout.Button("Current -> BeginValue"))
            {
                rectMoveToTween.BeginValue = rectMoveToTween.transform.position;
            }
            if (GUILayout.Button("Current -> EndValue"))
            {
                rectMoveToTween.EndValue = rectMoveToTween.transform.position;
            }

            if (GUILayout.Button("BeginValue -> Current"))
            {
                rectMoveToTween.transform.position = rectMoveToTween.BeginValue;
            }
            if (GUILayout.Button("EndValue -> Current"))
            {
                rectMoveToTween.transform.position = rectMoveToTween.EndValue;
            }
        }
    }
}
