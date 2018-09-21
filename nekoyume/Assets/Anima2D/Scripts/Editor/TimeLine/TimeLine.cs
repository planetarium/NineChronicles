using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace Anima2D
{
	internal class Timeline
	{
		private enum DragStates
		{
			None,
			Playhead,
			TimeLine,
			Element
		}
		private class Styles
		{
			public readonly GUIStyle block = new GUIStyle("MeTransitionBlock");
			public GUIStyle leftBlock = new GUIStyle("MeTransitionBlock");
			public GUIStyle rightBlock = new GUIStyle("MeTransitionBlock");
			public GUIStyle timeBlockRight = new GUIStyle("MeTimeLabel");
			public GUIStyle timeBlockLeft = new GUIStyle("MeTimeLabel");
			public readonly GUIStyle offLeft = new GUIStyle("MeTransOffLeft");
			public readonly GUIStyle offRight = new GUIStyle("MeTransOffRight");
			public readonly GUIStyle onLeft = new GUIStyle("MeTransOnLeft");
			public readonly GUIStyle onRight = new GUIStyle("MeTransOnRight");
			public readonly GUIStyle offOn = new GUIStyle("MeTransOff2On");
			public readonly GUIStyle onOff = new GUIStyle("MeTransOn2Off");
			public readonly GUIStyle background = new GUIStyle("MeTransitionBack");
			public readonly GUIStyle header = new GUIStyle("MeTransitionHead");
			public readonly GUIStyle handLeft = new GUIStyle("MeTransitionHandleLeft");
			public readonly GUIStyle handRight = new GUIStyle("MeTransitionHandleRight");
			public readonly GUIStyle handLeftPrev = new GUIStyle("MeTransitionHandleLeftPrev");
			public readonly GUIStyle playhead = new GUIStyle("MeTransPlayhead");
			public readonly GUIStyle selectHead = new GUIStyle("MeTransitionSelectHead");
			public readonly GUIStyle select = new GUIStyle("MeTransitionSelect");
			public readonly GUIStyle overlay = new GUIStyle("MeTransBGOver");
			public readonly GUIContent defaultDopeKeyIcon;

			public Styles()
			{
				timeBlockRight.alignment = TextAnchor.MiddleRight;
				timeBlockRight.normal.background = null;
				timeBlockLeft.normal.background = null;

				defaultDopeKeyIcon = EditorGUIUtility.IconContent("blendKey");
			}
		}

		TimeArea m_TimeArea;

		float m_Time = 0f;
		float m_FrameRate = 1f;

		Timeline.DragStates m_DragState;

		int id = -1;

		float m_StartValue = 0f;

		IEnumerable<IDopeElement> m_DopeElements = null;

		public IDopeElement selectedElement { get; set; }

		bool m_CanEditDopeElements = true;
		public bool canEditDopeElements {
			get { return m_CanEditDopeElements; }
			set { m_CanEditDopeElements = value; }
		}

		Timeline.Styles styles;

		Rect m_HeaderRect;
		Rect m_ContentRect;

		public float Time
		{
			get
			{
				return m_Time;
			}
			set
			{
				m_Time = value;
			}
		}

		public float FrameRate
		{
			get
			{
				return m_FrameRate;
			}
			set
			{
				if(value > 0f)
				{
					m_FrameRate = value;
				}
			}
		}

		public IEnumerable<IDopeElement> dopeElements {
			get {
				return m_DopeElements;
			}
			set {
				m_DopeElements = value;
			}
		}

		public Timeline()
		{
			Init();
		}

		public void ResetRange()
		{
			this.m_TimeArea.SetShownHRangeInsideMargins(0f, 100f);
		}

		void Init()
		{
			if(id == -1)
			{
				id = EditorGUIExtra.GetPermanentControlID();
			}

			if(m_TimeArea == null)
			{
				m_TimeArea = new TimeArea(false);
				m_TimeArea.hRangeLocked = false;
				m_TimeArea.vRangeLocked = false;
				m_TimeArea.hSlider = false;
				m_TimeArea.vSlider = false;
				m_TimeArea.margin = 10f;
				m_TimeArea.scaleWithWindow = true;
				m_TimeArea.hTicks.SetTickModulosForFrameRate(30f);
				m_TimeArea.OnEnable();
			}

			if (styles == null)
			{
				styles = new Timeline.Styles();
			}
		}

		public void DoTimeline(Rect rectArea)
		{
			Init();

			if (Event.current.type == EventType.Repaint)
			{
				m_TimeArea.rect = rectArea;
			}

			m_TimeArea.BeginViewGUI();
			m_TimeArea.EndViewGUI();

			GUI.BeginGroup(rectArea);

			Rect rect = new Rect(0f, 0f, rectArea.width, rectArea.height);

			m_HeaderRect = new Rect(0f, 0f, rectArea.width, 18f);

			m_ContentRect = new Rect(0f, 18f, rectArea.width, rectArea.height - m_HeaderRect.height);

			float playHeadPosX = m_TimeArea.TimeToPixel(Time, rectArea);

			Rect playHeadRect = new Rect(playHeadPosX - styles.playhead.fixedWidth * 0.5f - 4f, 4f, styles.playhead.fixedWidth, styles.playhead.fixedHeight);

			if(Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
			{
				GUIUtility.hotControl = id;
				GUIUtility.keyboardControl = id;

				if(playHeadRect.Contains(Event.current.mousePosition))
				{
					m_StartValue = playHeadPosX;

					m_DragState = Timeline.DragStates.Playhead;

				}else if(m_HeaderRect.Contains(Event.current.mousePosition))
				{
					m_DragState = Timeline.DragStates.TimeLine;

					Time = SnapToFrame( m_TimeArea.PixelToTime(Event.current.mousePosition.x, rectArea) );

					GUI.changed = true;
				}else{
					
					for (var i = dopeElements.GetEnumerator (); i.MoveNext ();)
					{
						IDopeElement element = i.Current;

						Rect position = GetDopeElementRect (element);

						if (position.Contains (Event.current.mousePosition))
						{
							m_DragState = DragStates.Element;

							m_StartValue = m_TimeArea.TimeToPixel (element.time, rectArea);

							Time = SnapToFrame (element.time);

							selectedElement = element;

							GUI.changed = true;
						}
					}

				}

				Event.current.Use();
			}

			if(Event.current.type == EventType.MouseDrag && GUIUtility.hotControl == this.id)
			{
				switch (m_DragState)
				{
				case Timeline.DragStates.Playhead:
					
					m_StartValue += Event.current.delta.x;

					Time = SnapToFrame( m_TimeArea.PixelToTime(m_StartValue, rectArea) );

					break;

				case Timeline.DragStates.TimeLine:
					Time = SnapToFrame( m_TimeArea.PixelToTime(Event.current.mousePosition.x, rectArea) );

					break;

				case Timeline.DragStates.Element:

					if (canEditDopeElements)
					{
						m_StartValue += Event.current.delta.x;

						selectedElement.time = SnapToFrame (m_TimeArea.PixelToTime (m_StartValue, rectArea));
					}

					break;
				}

				Event.current.Use();
				GUI.changed = true;
			}

			if(Event.current.type == EventType.MouseUp && GUIUtility.hotControl == this.id)
			{
				switch (m_DragState)
				{
				case Timeline.DragStates.Playhead:
					break;

				case Timeline.DragStates.TimeLine:
					break;

				case Timeline.DragStates.Element:
					if (canEditDopeElements)
					{
						selectedElement.Flush ();
						selectedElement = null;
					}
					break;
				}

				GUI.changed = true;

				m_DragState = Timeline.DragStates.None;

				GUIUtility.hotControl = 0;

				Event.current.Use();
			}

			GUI.Box(m_HeaderRect, GUIContent.none, this.styles.header);

			GUI.Box(m_ContentRect, GUIContent.none, this.styles.background);

			m_TimeArea.TimeRuler(m_HeaderRect, FrameRate);

			GUI.Box(playHeadRect, GUIContent.none, styles.playhead);

			DopeLineRepaint();

			GUI.EndGroup();
		}

		void DopeLineRepaint()
		{
			if(Event.current.type != EventType.Repaint || m_DopeElements == null)
			{
				return;
			}
				
			Color color = GUI.color;

			if (canEditDopeElements) {
				GUI.color = Color.white;
			} else {
				GUI.color = Color.gray;
			}

			foreach(IDopeElement dopeElement in m_DopeElements)
			{
				if(dopeElement != null)
				{
					Rect position = GetDopeElementRect(dopeElement);

					GUI.DrawTexture(position, styles.defaultDopeKeyIcon.image, ScaleMode.ScaleToFit, true, 1f);
				}
			}

			GUI.color = color;
		}

		Rect GetDopeElementRect(IDopeElement dopeElement)
		{
			Texture defaultDopeKeyIcon = styles.defaultDopeKeyIcon.image;

			float time = m_TimeArea.TimeToPixel( SnapToFrame(dopeElement.time), m_ContentRect);

			return new Rect(time - (defaultDopeKeyIcon.width / 2), m_ContentRect.y, defaultDopeKeyIcon.width, defaultDopeKeyIcon.height);
		}

		float SnapToFrame(float time)
		{
			return Mathf.Round(time * FrameRate) / FrameRate;
		}
	}
}