using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Reflection;

namespace Anima2D
{
	public class AnimationWindowImpl_56 : AnimationWindowImpl_55
	{
		PropertyInfo m_CurrentFrameProperty = null;
		MethodInfo m_StartRecording = null;
		MethodInfo m_StopRecording = null;
		

		public override void InitializeReflection()
		{
			base.InitializeReflection();

			m_CurrentFrameProperty = m_AnimationWindowStateType.GetProperty("currentFrame",BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			m_StartRecording = m_AnimationWindowStateType.GetMethod("StartRecording",BindingFlags.Public | BindingFlags.Static);
			m_StopRecording = m_AnimationWindowStateType.GetMethod("StopRecording",BindingFlags.Public | BindingFlags.Static);
		}

		public override bool recording {
			get {
				return base.recording;
			}

			set {
				if(value)
				{
					if(m_StartRecording != null)
						m_StartRecording.Invoke(null,null);
				} else {
					if(m_StopRecording != null)
						m_StopRecording.Invoke(null,null);
				}
			}
		}

		public override int frame {
			get {
				if(state != null && m_CurrentFrameProperty != null)
				{
					return (int)m_CurrentFrameProperty.GetValue(state,null);
				}

				return 0;
			}

			set {
				if(state != null && m_CurrentFrameProperty != null)
				{
					m_CurrentFrameProperty.SetValue(state, value, null);
				}
			}
		}
	}
}
