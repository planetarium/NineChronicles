using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Reflection;

namespace Anima2D
{
	public class AnimationWindowImpl_55 : AnimationWindowImpl_54
	{
		protected Type m_CurveBindingUtilityType = typeof(EditorWindow).Assembly.GetType("UnityEditorInternal.CurveBindingUtility");

		MethodInfo m_GetCurrentValueMethod = null;
		MethodInfo m_AddKeyframeToCurveMethod = null;

		public override void InitializeReflection()
		{
			base.InitializeReflection();

			Type[] l_GetCurrentValueTypes = { typeof(GameObject), typeof(EditorCurveBinding) };
			m_GetCurrentValueMethod = m_CurveBindingUtilityType.GetMethod("GetCurrentValue",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, l_GetCurrentValueTypes, null);

			m_AddKeyframeToCurveMethod = m_AnimationWindowUtilityType.GetMethod("AddKeyframeToCurve",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
		}

		public override void CreateDefaultCurve(EditorCurveBinding binding)
		{
			System.Type editorCurveValueType = AnimationUtility.GetEditorCurveValueType(rootGameObject, binding);

			object curve = Activator.CreateInstance(m_AnimationWindowCurveType, new object[]{ activeAnimationClip, binding, editorCurveValueType });

			object currentValue = GetCurrentValue(rootGameObject, binding);

			if(activeAnimationClip.length == 0f)
			{
				AddKeyframeToCurve(curve, currentValue, editorCurveValueType, AnimationKeyTime(0f, activeAnimationClip.frameRate));
			}
			else
			{
				AddKeyframeToCurve(curve, currentValue, editorCurveValueType, AnimationKeyTime(0f, activeAnimationClip.frameRate));
				AddKeyframeToCurve(curve, currentValue, editorCurveValueType, AnimationKeyTime(activeAnimationClip.length, activeAnimationClip.frameRate));
			}

			SaveCurve(curve);
		}

		public override void AddKey(EditorCurveBinding binding, float time)
		{
			object curve = GetCurve(binding);

			if(curve != null)
			{
				System.Type editorCurveValueType = AnimationUtility.GetEditorCurveValueType(rootGameObject, binding);
				object currentValue = GetCurrentValue(rootGameObject, binding);

				AddKeyframeToCurve(curve,currentValue,editorCurveValueType,AnimationKeyTime(time, activeAnimationClip.frameRate));

				SaveCurve(curve);
			}
		}

		protected void AddKeyframeToCurve(object curve, object value, System.Type type, object time)
		{
			if(m_AddKeyframeToCurveMethod != null)
			{
				object[] parameters = { curve, value, type, time };
				m_AddKeyframeToCurveMethod.Invoke(null,parameters);
			}
		}

		protected object GetCurrentValue(GameObject rootGameObject, EditorCurveBinding curveBinding)
		{
			if(m_GetCurrentValueMethod != null)
			{
				object[] parameters = { rootGameObject, curveBinding };
				return (object) m_GetCurrentValueMethod.Invoke(null, parameters);
			}

			return null;
		}
	}
}
