using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Reflection;

namespace Anima2D
{
	public class AnimationWindowImpl_2017_1 : AnimationWindowImpl_56
	{
		MethodInfo m_StartRecording = null;
		MethodInfo m_StopRecording = null;
		

		public override void InitializeReflection()
		{
			base.InitializeReflection();

			m_StartRecording = m_AnimationWindowStateType.GetMethod("StartRecording",BindingFlags.Public | BindingFlags.Instance);
			m_StopRecording = m_AnimationWindowStateType.GetMethod("StopRecording",BindingFlags.Public | BindingFlags.Instance);
		}

		public override bool recording {
			get {
				return base.recording;
			}

			set {
				if(value)
				{
					if(m_StartRecording != null)
						m_StartRecording.Invoke(state,null);
				} else {
					if(m_StopRecording != null)
						m_StopRecording.Invoke(state,null);
				}
			}
		}
	}
}
