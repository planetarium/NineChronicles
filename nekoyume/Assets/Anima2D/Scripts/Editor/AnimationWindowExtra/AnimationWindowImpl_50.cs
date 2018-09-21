using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Reflection;

namespace Anima2D
{
	public class AnimationWindowImpl_50 : IAnimationWindowImpl
	{
		Type m_AnimationWindowType = typeof(EditorWindow).Assembly.GetType("UnityEditor.AnimationWindow");
		Type m_AnimationWindowStateType = typeof(EditorWindow).Assembly.GetType("UnityEditorInternal.AnimationWindowState");

		FieldInfo m_FrameField = null;
		FieldInfo m_ActiveAnimationClipField = null;
		FieldInfo m_ActiveGameObjectField = null;
		FieldInfo m_RootGameObjectField = null;
		FieldInfo m_RefreshField = null;

		PropertyInfo m_StateProperty = null;

		MethodInfo m_GetAllAnimationWindows = null;
		MethodInfo m_PreviewFrameMethod = null;
		MethodInfo m_GetAutoRecordModeMethod = null;
		MethodInfo m_SetAutoRecordModeMethod = null;
		MethodInfo m_GetTimeSecondsMethod = null;
		MethodInfo m_FrameToTimeMethod = null;
		MethodInfo m_TimeToFrameMethod = null;

		public void InitializeReflection()
		{
			m_GetAllAnimationWindows = m_AnimationWindowType.GetMethod("GetAllAnimationWindows", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			m_StateProperty = m_AnimationWindowType.GetProperty("state", BindingFlags.Instance | BindingFlags.Public);
			m_FrameField = m_AnimationWindowStateType.GetField("m_Frame", BindingFlags.Instance | BindingFlags.Public);
			m_PreviewFrameMethod = m_AnimationWindowType.GetMethod("PreviewFrame",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			m_GetAutoRecordModeMethod = m_AnimationWindowType.GetMethod("GetAutoRecordMode",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			m_SetAutoRecordModeMethod = m_AnimationWindowType.GetMethod("SetAutoRecordMode",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			m_ActiveAnimationClipField = m_AnimationWindowStateType.GetField("m_ActiveAnimationClip", BindingFlags.Instance | BindingFlags.Public);
			m_ActiveGameObjectField = m_AnimationWindowStateType.GetField("m_ActiveGameObject", BindingFlags.Instance | BindingFlags.Public);
			m_RootGameObjectField = m_AnimationWindowStateType.GetField("m_RootGameObject", BindingFlags.Instance | BindingFlags.Public);
			m_RefreshField = m_AnimationWindowStateType.GetField("m_Refresh", BindingFlags.Instance | BindingFlags.NonPublic);
			m_FrameToTimeMethod = m_AnimationWindowStateType.GetMethod("FrameToTime",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			m_TimeToFrameMethod = m_AnimationWindowStateType.GetMethod("TimeToFrame",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			m_GetTimeSecondsMethod = m_AnimationWindowStateType.GetMethod("GetTimeSeconds",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
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

		object state {
			get {
				if(animationWindow != null && m_StateProperty != null)
				{
					return m_StateProperty.GetValue(animationWindow,null);
				}

				return null;
			} 
		}
		
		public int frame {
			get {
				if(state != null && m_FrameField != null)
				{
					return (int)m_FrameField.GetValue(state);
				}
				
				return 0;
			}
			
			set {
				if(m_PreviewFrameMethod != null)
				{
					object[] parameters = { value };
					m_PreviewFrameMethod.Invoke(animationWindow, parameters);
				}
			}
		}
		
		public bool recording {
			get {
				if(animationWindow && m_GetAutoRecordModeMethod != null)
				{
					return (bool)m_GetAutoRecordModeMethod.Invoke(animationWindow, null);
				}
				
				return false;
			}
			
			set {
				if(animationWindow && m_SetAutoRecordModeMethod != null)
				{
					object[] parameters = { value };
					m_SetAutoRecordModeMethod.Invoke(animationWindow, parameters);
				}
			}
		}
		
		public AnimationClip activeAnimationClip {
			get {
				if(state != null)
				{
					return m_ActiveAnimationClipField.GetValue(state) as AnimationClip;
				}
				
				return null;
			}
		}
		
		public GameObject activeGameObject {
			get {
				if(state != null)
				{
					return m_ActiveGameObjectField.GetValue(state) as GameObject;
				}
				
				return null;
			}
		}
		
		public GameObject rootGameObject {
			get {
				if(state != null)
				{
					return m_RootGameObjectField.GetValue(state) as GameObject;
				}
				
				return null;
			}
		}

		public int refresh {
			get {
			    if(state != null)
				{
					return (int)m_RefreshField.GetValue(state);
				}

				return 0;
			}
		}
		
		public float currentTime {
			get {
				if(state != null && m_GetTimeSecondsMethod != null)
				{
					return (float)m_GetTimeSecondsMethod.Invoke(state,null);
				}
				return 0f;
			}
		}

		public bool playing {
			get {
				Debug.Log("Anima2D: playing property not needed in 5.0");

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
		
		public void CreateDefaultCurve(EditorCurveBinding binding)
		{
			Debug.Log("Anima2D: CreateDefaultCurve method not needed in 5.0");
		}
		
		public void AddKey(EditorCurveBinding binding, float time)
		{
			Debug.Log("Anima2D: AddKey method not needed in 5.0");
		}
	}
}
