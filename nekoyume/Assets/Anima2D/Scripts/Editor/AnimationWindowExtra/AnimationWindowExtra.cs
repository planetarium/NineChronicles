using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Reflection;

namespace Anima2D
{
	[InitializeOnLoad]
	public class AnimationWindowExtra
	{
		static IAnimationWindowImpl s_Impl;

		static AnimationWindowExtra()
		{
#if UNITY_5_0
			s_Impl = new AnimationWindowImpl_50();
#elif UNITY_5_1 || UNITY_5_2 || UNITY_5_3
			s_Impl = new AnimationWindowImpl_51_52_53();
#elif UNITY_5_4
			s_Impl = new AnimationWindowImpl_54();
#elif UNITY_5_5
			s_Impl = new AnimationWindowImpl_55();
#elif UNITY_5_6
			s_Impl = new AnimationWindowImpl_56();
#elif UNITY_2017_1_OR_NEWER
			s_Impl = new AnimationWindowImpl_2017_1();
#endif
			s_Impl.InitializeReflection();
		}

		public static EditorWindow animationWindow
		{
			get {
				return s_Impl.animationWindow;
			}
		}

		public static int frame {
			get { return s_Impl.frame; }
			set { s_Impl.frame = value; }
		}

		public static bool recording {
			get { return s_Impl.recording; }
			set {s_Impl.recording = value; }
		}

		public static AnimationClip activeAnimationClip {
			get { return s_Impl.activeAnimationClip; }
		}

		public static GameObject activeGameObject {
			get { return s_Impl.activeGameObject; }
		}

		public static GameObject rootGameObject {
			get { return s_Impl.rootGameObject; }
		}

		public static int refresh {
			get { return s_Impl.refresh; }
		}

		public static float currentTime {
			get { return s_Impl.currentTime; }
		}

		public static bool playing {
			get { return s_Impl.playing; }
		}
			
		public static float FrameToTime(int frame)
		{
			return s_Impl.FrameToTime(frame);	
		}

		public static float TimeToFrame(float time)
		{
			return s_Impl.TimeToFrame(time);
		}

		public static void CreateDefaultCurve(EditorCurveBinding binding)
		{
			s_Impl.CreateDefaultCurve(binding);	
		}

		public static void AddKey(EditorCurveBinding binding, float time)
		{
			s_Impl.AddKey(binding,time);
		}
	}
}
