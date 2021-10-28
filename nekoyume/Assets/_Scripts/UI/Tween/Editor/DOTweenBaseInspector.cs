using System;
using System.Linq;
using System.Collections;
using DG.Tweening;
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
            if (tween.tweenType == DOTweenBase.TweenType.Forward
                || tween.tweenType == DOTweenBase.TweenType.Reverse
                || tween.tweenType == DOTweenBase.TweenType.PingPongOnce)
            {
                GUILayout.Space(4.0f);
                GUILayout.Label ("[Tween Complete Callback]");
                tween.target = (GameObject)EditorGUILayout.ObjectField("GameObject", tween.target, typeof(GameObject), true);
                if (tween.target)
                {
                    DrawGUIComponents();
                }
                else
                {
                    tween.completeMethod = "";
                }
                tween.completeDelay = EditorGUILayout.FloatField("CompleteDelay", tween.completeDelay);
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Easing Options", EditorStyles.boldLabel);
            tween.useCustomEaseCurve =
                EditorGUILayout.Toggle("Use Custom Ease Curve", tween.useCustomEaseCurve);
            if (tween.useCustomEaseCurve)
            {
                tween.customEaseCurve = EditorGUILayout.CurveField("Custom Easing Curve", tween.customEaseCurve);
            }
            else
            {
                tween.ease = (Ease) EditorGUILayout.EnumPopup("Easing function", tween.ease);
            }

            DrawGUIRectTransformMoveTo();
        }

        private void DrawGUIComponents()
        {
            var tween = target as DOTweenBase;
            var components = tween.target.GetComponents(typeof(Component));
            if (components.Length == 0)
                return;
            string[] options = components.Select((x, i) => $"[{i}] " + x.GetType().ToString()).ToArray();
            int selected = tween.componentIndex;
            selected = selected < 0 ? 0 : selected;
            selected = EditorGUILayout.Popup("Component", selected, options);
            tween.componentIndex = selected;

            if (tween.componentIndex >= 0)
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
            int selected = Array.IndexOf(options, tween.completeMethod);
            selected = selected < 0 ? 0 : selected;
            selected = EditorGUILayout.Popup("Method", selected, options);
            if (options.Length == 0)
                return;
            tween.completeMethod = options[selected];
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
                rectMoveToTween.beginValue = rectMoveToTween.transform.position;
            }
            if (GUILayout.Button("Current -> EndValue"))
            {
                rectMoveToTween.endValue = rectMoveToTween.transform.position;
            }

            if (GUILayout.Button("BeginValue -> Current"))
            {
                rectMoveToTween.transform.position = rectMoveToTween.beginValue;
            }
            if (GUILayout.Button("EndValue -> Current"))
            {
                rectMoveToTween.transform.position = rectMoveToTween.endValue;
            }
        }
    }
}
