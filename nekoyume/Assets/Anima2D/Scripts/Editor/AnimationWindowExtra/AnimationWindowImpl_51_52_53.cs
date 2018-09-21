using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Reflection;

namespace Anima2D
{
	public class AnimationWindowImpl_51_52_53 : IAnimationWindowImpl
	{
		protected Type m_AnimationWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.AnimationWindow");
		protected Type m_AnimationWindowStateType = typeof(EditorWindow).Assembly.GetType("UnityEditorInternal.AnimationWindowState");
		protected Type m_AnimationWindowUtilityType = typeof(EditorWindow).Assembly.GetType("UnityEditorInternal.AnimationWindowUtility");
		protected Type m_AnimationWindowCurveType = typeof(EditorWindow).Assembly.GetType("UnityEditorInternal.AnimationWindowCurve");
		protected Type m_AnimationKeyTimeType = typeof(EditorWindow).Assembly.GetType("UnityEditorInternal.AnimationKeyTime");
		protected Type m_AnimEditorType = typeof(EditorWindow).Assembly.GetType("UnityEditor.AnimEditor");

		FieldInfo m_AnimEditorField = null;
		FieldInfo m_StateField = null;
		FieldInfo m_AllCurvesCacheField = null;

		PropertyInfo m_FrameProperty = null;
		PropertyInfo m_RecordingProperty = null;
		PropertyInfo m_ActiveAnimationClipProperty = null;
		PropertyInfo m_ActiveGameObjectProperty = null;
		PropertyInfo m_ActiveRootGameObjectProperty = null;
		PropertyInfo m_RefreshProperty = null;
		PropertyInfo m_CurrentTimeProperty = null;
		PropertyInfo m_PlayingProperty = null;
		PropertyInfo m_BindingProperty = null;

		MethodInfo m_GetAllAnimationWindows = null;
		MethodInfo m_FrameToTimeMethod = null;
		MethodInfo m_TimeToFrameMethod = null;
		MethodInfo m_TimeMethod = null;
		MethodInfo m_CreateDefaultCurvesMethod = null;
		MethodInfo m_AddKeyframeToCurveMethod = null;
		MethodInfo m_SaveCurveMethod = null;

		public virtual void InitializeReflection()
		{
			m_GetAllAnimationWindows = m_AnimationWindowType.GetMethod("GetAllAnimationWindows", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			m_AnimEditorField = m_AnimationWindowType.GetField( "m_AnimEditor", BindingFlags.Instance | BindingFlags.NonPublic );
			m_StateField = m_AnimEditorType.GetField("m_State", BindingFlags.Instance | BindingFlags.NonPublic);
			m_AllCurvesCacheField = m_AnimationWindowStateType.GetField("m_AllCurvesCache", BindingFlags.Instance | BindingFlags.NonPublic);

			m_FrameProperty = m_AnimationWindowStateType.GetProperty("frame",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			m_RecordingProperty = m_AnimationWindowStateType.GetProperty("recording",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			m_ActiveAnimationClipProperty = m_AnimationWindowStateType.GetProperty("activeAnimationClip", BindingFlags.Instance | BindingFlags.Public);
			m_ActiveGameObjectProperty = m_AnimationWindowStateType.GetProperty("activeGameObject", BindingFlags.Instance | BindingFlags.Public);
			m_ActiveRootGameObjectProperty = m_AnimationWindowStateType.GetProperty("activeRootGameObject", BindingFlags.Instance | BindingFlags.Public);
			m_RefreshProperty = m_AnimationWindowStateType.GetProperty("refresh",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			m_CurrentTimeProperty = m_AnimationWindowStateType.GetProperty("currentTime", BindingFlags.Instance | BindingFlags.Public);
			m_PlayingProperty = m_AnimationWindowStateType.GetProperty("playing", BindingFlags.Instance | BindingFlags.Public);
			m_BindingProperty = m_AnimationWindowCurveType.GetProperty("binding", BindingFlags.Instance | BindingFlags.Public );

			m_FrameToTimeMethod = m_AnimationWindowStateType.GetMethod("FrameToTime",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			m_TimeToFrameMethod = m_AnimationWindowStateType.GetMethod("TimeToFrame",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			m_TimeMethod = m_AnimationKeyTimeType.GetMethod("Time",BindingFlags.Public | BindingFlags.Static);
			m_CreateDefaultCurvesMethod = m_AnimationWindowUtilityType.GetMethod("CreateDefaultCurves",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

			Type[] l_AddKeyframeToCurveTypes = { m_AnimationWindowStateType, m_AnimationWindowCurveType, m_AnimationKeyTimeType };
			m_AddKeyframeToCurveMethod = m_AnimationWindowUtilityType.GetMethod("AddKeyframeToCurve",BindingFlags.Public | BindingFlags.Static, null, l_AddKeyframeToCurveTypes, null);

			Type[] l_SaveCurveTypes = { m_AnimationWindowCurveType };
			m_SaveCurveMethod = m_AnimationWindowStateType.GetMethod("SaveCurve",BindingFlags.Public | BindingFlags.Instance, null, l_SaveCurveTypes, null);
		}

		public EditorWindow animationWindow
		{
			get {
				if(m_GetAllAnimationWindows != null)
				{
					var list = m_GetAllAnimationWindows.Invoke(null, null);
					int numElements = (int)list.GetType().GetProperty("Count").GetValue(list, null);  
 
					if(numElements > 0)
					{
						object[] index = { 0 };  
                        return list.GetType().GetProperty("Item").GetValue(list, index) as EditorWindow;  
					}
				}

				return null;
			}
		}

		ScriptableObject animEditor {
			get {
				if(AnimationWindowExtra.animationWindow != null && m_AnimEditorField != null)
				{
					return (ScriptableObject)m_AnimEditorField.GetValue( AnimationWindowExtra.animationWindow );
				}
				return null;
			}
		}
		
		protected object state {
			get {
				if(animEditor && m_StateField != null)
				{
					return m_StateField.GetValue(animEditor);
				}
				return null;
			}
			
		}
		
		public virtual int frame {
			get {
				if(state != null && m_FrameProperty != null)
				{
					return (int)m_FrameProperty.GetValue(state,null);
				}
				
				return 0;
			}
			
			set {
				if(state != null && m_FrameProperty != null)
				{
					m_FrameProperty.SetValue(state, value, null);
				}
			}
		}

		public virtual bool recording {
			get {
				if(state != null && m_RecordingProperty != null)
				{
					return (bool)m_RecordingProperty.GetValue(state,null);
				}
				
				return false;
			}
			
			set {
				if(state != null && m_RecordingProperty != null)
				{
					m_RecordingProperty.SetValue(state, value, null);
				}
			}
		}
		
		public AnimationClip activeAnimationClip {
			get {
				if(state != null && m_ActiveAnimationClipProperty != null)
				{
					return (AnimationClip)m_ActiveAnimationClipProperty.GetValue(state,null);
				}
				return null;
			}
		}
		
		public GameObject activeGameObject {
			get {
				if(state != null && m_ActiveGameObjectProperty != null)
				{
					return (GameObject)m_ActiveGameObjectProperty.GetValue(state,null);
				}
				
				return null;
			}
		}
		
		public GameObject rootGameObject {
			get {
				if(state != null && m_ActiveRootGameObjectProperty != null)
				{
					return (GameObject)m_ActiveRootGameObjectProperty.GetValue(state,null);
				}
				
				return null;
			}
		}
		
		public int refresh {
			get {
				if(state != null && m_RefreshProperty != null)
				{
					return (int)m_RefreshProperty.GetValue(state,null);
				}
				return 0;
			}
		}
		
		public float currentTime {
			get {
				if(state != null && m_CurrentTimeProperty != null)
				{
					return (float)m_CurrentTimeProperty.GetValue(state,null);
				}
				return 0f;
			}
		}
		
		public bool playing {
			get {
				if(state != null && m_PlayingProperty != null)
				{
					return (bool)m_PlayingProperty.GetValue(state,null);
				}
				return false;
			}
		}

		public float FrameToTime(int frame)
		{
			if(state != null && m_FrameToTimeMethod != null)
			{
				object[] parameters = { (float)frame };
				return (float) m_FrameToTimeMethod.Invoke(state,parameters);
			}
			return 0f;
		}
		
		public float TimeToFrame(float time)
		{
			if(state != null && m_TimeToFrameMethod != null)
			{
				object[] parameters = { (float)time };
				return (float) m_TimeToFrameMethod.Invoke(state,parameters);
			}
			return 0f;
		}

		public virtual void CreateDefaultCurve(EditorCurveBinding binding)
		{
			if(m_CreateDefaultCurvesMethod != null)
			{
				object[] parameters = { state, new EditorCurveBinding[] { binding } };
				m_CreateDefaultCurvesMethod.Invoke(null,parameters);
			}
		}
		
		public virtual void AddKey(EditorCurveBinding binding, float time)
		{
			IList l_curves = GetAllCurves();
			
			if(l_curves != null)
			{
				object l_animationWindowCurve = GetCurve(binding);
				
				if(m_AddKeyframeToCurveMethod != null && l_animationWindowCurve != null)
				{
					object[] parameters = { state, l_animationWindowCurve, AnimationKeyTime(time, activeAnimationClip.frameRate) };
					m_AddKeyframeToCurveMethod.Invoke(null,parameters);
				}
			}
		}

		protected object AnimationKeyTime(float time, float frameRate)
		{
			if(m_TimeMethod != null)
			{
				object[] parameters = { time, frameRate };
				return m_TimeMethod.Invoke(null,parameters);
			}

			return null;
		}

		protected EditorCurveBinding GetBinding(object curveObject)
		{
			if(m_BindingProperty != null)
			{
				return (EditorCurveBinding)m_BindingProperty.GetValue(curveObject,null);
			}

			return default(EditorCurveBinding);
		}

		protected object GetCurve(EditorCurveBinding binding)
		{
			IList l_curves = GetAllCurves();

			if(l_curves != null)
			{
				foreach(var curve in l_curves)
				{
					if(GetBinding(curve) == binding)
					{
						return curve as object;
					}
				}
			}

			return null;
		}
		
		protected void SaveCurve(object _curve)
		{
			if(state != null &&m_SaveCurveMethod != null)
			{
				object[] parameters = { _curve };
				m_SaveCurveMethod.Invoke(state,parameters);
			}
		}

		protected virtual IList GetAllCurves()
		{
			if(state != null)
			{ 
				return m_AllCurvesCacheField.GetValue( state ) as IList;
			}

			return null;
		}
	}
}

