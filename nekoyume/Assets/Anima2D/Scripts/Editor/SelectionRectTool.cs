using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	public class SelectionRectTool
	{
		static Vector2 s_StartPosition = Vector2.zero;
		static Vector2 s_EndPosition = Vector2.zero;
		static Rect s_currentRect = new Rect(0f,0f,0f,0f);

		public static Rect Do()
		{
			int controlID = GUIUtility.GetControlID("SelectionRect".GetHashCode(), FocusType.Passive);

			return Do(controlID);
		}

		public static Rect Do(int controlID)
		{
			EventType eventType = Event.current.GetTypeForControl(controlID);

			if(eventType == EventType.MouseDown)
			{
				s_StartPosition = HandlesExtra.GUIToWorld(Event.current.mousePosition);
				s_EndPosition = s_StartPosition;
				s_currentRect.position = s_StartPosition;
				s_currentRect.size = Vector2.zero;
			}

			if(eventType == EventType.Layout)
			{
				HandleUtility.AddDefaultControl(controlID);
			}

			if (eventType == EventType.Repaint)
			{
				if(GUIUtility.hotControl == controlID)
				{
					RectHandles.RenderRect(s_currentRect,Vector3.zero,Quaternion.identity,new Color(0f, 1f, 1f, 1f), 0.05f, 0.8f);
				}
			}

			if(Camera.current)
			{
#if UNITY_5_6_OR_NEWER
				s_EndPosition = Handles.Slider2D(controlID,s_EndPosition, Vector3.forward, Vector3.right, Vector3.up, HandleUtility.GetHandleSize(s_EndPosition) / 4f, (id,pos,rot,size,evt) => {}, Vector2.zero);
#else
				s_EndPosition = Handles.Slider2D(controlID,s_EndPosition, Vector3.forward, Vector3.right, Vector3.up, HandleUtility.GetHandleSize(s_EndPosition) / 4f, (id,pos,rot,size) => {}, Vector2.zero);
#endif
			}else{
				s_EndPosition = HandlesExtra.Slider2D(controlID, s_EndPosition, null);
			}

			s_currentRect.min = s_StartPosition;
			s_currentRect.max = s_EndPosition;

			return s_currentRect;
		}
	}
}
