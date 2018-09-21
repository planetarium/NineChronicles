using System;
using UnityEngine;
using UnityEditor;

namespace Anima2D
{
	[Serializable]
	internal class TimeArea : ZoomableArea
	{
		private class Styles2
		{
			public GUIStyle TimelineTick = "AnimationTimelineTick";
			public GUIStyle labelTickMarks = "CurveEditorLabelTickMarks";
		}
		public enum TimeRulerDragMode
		{
			None,
			Start,
			End,
			Dragging,
			Cancel
		}
		internal const int kTickRulerDistMin = 3;
		internal const int kTickRulerDistFull = 80;
		internal const int kTickRulerDistLabel = 40;
		internal const float kTickRulerHeightMax = 0.7f;
		internal const float kTickRulerFatThreshold = 0.5f;
		private TickHandler m_HTicks;
		private TickHandler m_VTicks;
		private static TimeArea.Styles2 styles;
		private static float s_OriginalTime;
		private static float s_PickOffset;
		public TickHandler hTicks
		{
			get
			{
				return this.m_HTicks;
			}
			set
			{
				this.m_HTicks = value;
			}
		}
		public TickHandler vTicks
		{
			get
			{
				return this.m_VTicks;
			}
			set
			{
				this.m_VTicks = value;
			}
		}

		public TimeArea(bool minimalGUI) : base(minimalGUI)
		{
			float[] tickModulos = new float[]
			{
				1E-07f,
				5E-07f,
				1E-06f,
				5E-06f,
				1E-05f,
				5E-05f,
				0.0001f,
				0.0005f,
				0.001f,
				0.005f,
				0.01f,
				0.05f,
				0.1f,
				0.5f,
				1f,
				5f,
				10f,
				50f,
				100f,
				500f,
				1000f,
				5000f,
				10000f,
				50000f,
				100000f,
				500000f,
				1000000f,
				5000000f,
				1E+07f
			};
			this.hTicks = new TickHandler();
			this.hTicks.SetTickModulos(tickModulos);
			this.vTicks = new TickHandler();
			this.vTicks.SetTickModulos(tickModulos);
		}
		private static void InitStyles()
		{
			if (TimeArea.styles == null)
			{
				TimeArea.styles = new TimeArea.Styles2();
			}
		}
		private void SetTickMarkerRanges()
		{
			this.hTicks.SetRanges(base.shownArea.xMin, base.shownArea.xMax, base.drawRect.xMin, base.drawRect.xMax);
			this.vTicks.SetRanges(base.shownArea.yMin, base.shownArea.yMax, base.drawRect.yMin, base.drawRect.yMax);
		}
		public void DrawMajorTicks(Rect position, float frameRate)
		{
			Color color = Handles.color;
			GUI.BeginGroup(position);
			if (Event.current.type != EventType.Repaint)
			{
				GUI.EndGroup();
				return;
			}
			TimeArea.InitStyles();
			this.SetTickMarkerRanges();
			this.hTicks.SetTickStrengths(3f, 80f, true);
			Color textColor = TimeArea.styles.TimelineTick.normal.textColor;
			textColor.a = 0.1f;
			Handles.color = textColor;
			for (int i = 0; i < this.hTicks.tickLevels; i++)
			{
				float num = this.hTicks.GetStrengthOfLevel(i) * 0.9f;
				if (num > 0.5f)
				{
					float[] ticksAtLevel = this.hTicks.GetTicksAtLevel(i, true);
					for (int j = 0; j < ticksAtLevel.Length; j++)
					{
						if (ticksAtLevel[j] >= 0f)
						{
							int num2 = Mathf.RoundToInt(ticksAtLevel[j] * frameRate);
							float x = this.FrameToPixel((float)num2, frameRate, position);
							Handles.DrawLine(new Vector3(x, 0f, 0f), new Vector3(x, position.height, 0f));
						}
					}
				}
			}
			GUI.EndGroup();
			Handles.color = color;
		}
		public void TimeRuler(Rect position, float frameRate)
		{
			Color color = GUI.color;
			GUI.BeginGroup(position);
			if (Event.current.type != EventType.Repaint)
			{
				GUI.EndGroup();
				return;
			}
			TimeArea.InitStyles();
			HandlesExtra.ApplyWireMaterial();
			GL.Begin(1);
			Color backgroundColor = GUI.backgroundColor;
			this.SetTickMarkerRanges();
			this.hTicks.SetTickStrengths(3f, 80f, true);
			Color textColor = TimeArea.styles.TimelineTick.normal.textColor;
			textColor.a = 0.75f;
			for (int i = 0; i < this.hTicks.tickLevels; i++)
			{
				float num = this.hTicks.GetStrengthOfLevel(i) * 0.9f;
				float[] ticksAtLevel = this.hTicks.GetTicksAtLevel(i, true);
				for (int j = 0; j < ticksAtLevel.Length; j++)
				{
					if (ticksAtLevel[j] >= base.hRangeMin && ticksAtLevel[j] <= base.hRangeMax)
					{
						int num2 = Mathf.RoundToInt(ticksAtLevel[j] * frameRate);
						float num3 = position.height * Mathf.Min(1f, num) * 0.7f;
						float num4 = this.FrameToPixel((float)num2, frameRate, position);
						GL.Color(new Color(1f, 1f, 1f, num / 0.5f) * textColor);
						GL.Vertex(new Vector3(num4, position.height - num3 + 0.5f, 0f));
						GL.Vertex(new Vector3(num4, position.height - 0.5f, 0f));
						if (num > 0.5f)
						{
							GL.Color(new Color(1f, 1f, 1f, num / 0.5f - 1f) * textColor);
							GL.Vertex(new Vector3(num4 + 1f, position.height - num3 + 0.5f, 0f));
							GL.Vertex(new Vector3(num4 + 1f, position.height - 0.5f, 0f));
						}
					}
				}
			}
			GL.End();
			int levelWithMinSeparation = this.hTicks.GetLevelWithMinSeparation(40f);
			float[] ticksAtLevel2 = this.hTicks.GetTicksAtLevel(levelWithMinSeparation, false);
			for (int k = 0; k < ticksAtLevel2.Length; k++)
			{
				if (ticksAtLevel2[k] >= base.hRangeMin && ticksAtLevel2[k] <= base.hRangeMax)
				{
					int num5 = Mathf.RoundToInt(ticksAtLevel2[k] * frameRate);
					float num6 = Mathf.Floor(this.FrameToPixel((float)num5, frameRate, base.rect));
					string text = this.FormatFrame(num5, frameRate);
					GUI.Label(new Rect(num6 + 3f, -3f, 40f, 20f), text, TimeArea.styles.TimelineTick);
				}
			}
			GUI.EndGroup();
			GUI.backgroundColor = backgroundColor;
			GUI.color = color;
		}
		public TimeArea.TimeRulerDragMode BrowseRuler(Rect position, ref float time, float frameRate, bool pickAnywhere, GUIStyle thumbStyle)
		{
			int controlID = GUIUtility.GetControlID(3126789, FocusType.Passive);
			return this.BrowseRuler(position, controlID, ref time, frameRate, pickAnywhere, thumbStyle);
		}
		public TimeArea.TimeRulerDragMode BrowseRuler(Rect position, int id, ref float time, float frameRate, bool pickAnywhere, GUIStyle thumbStyle)
		{
			Event current = Event.current;
			Rect position2 = position;
			if (time != -1f)
			{
				position2.x = Mathf.Round(base.TimeToPixel(time, position)) - (float)thumbStyle.overflow.left;
				position2.width = thumbStyle.fixedWidth + (float)thumbStyle.overflow.horizontal;
			}
			switch (current.GetTypeForControl(id))
			{
			case EventType.MouseDown:
				if (position2.Contains(current.mousePosition))
				{
					GUIUtility.hotControl = id;
					TimeArea.s_PickOffset = current.mousePosition.x - base.TimeToPixel(time, position);
					current.Use();
					return TimeArea.TimeRulerDragMode.Start;
				}
				if (pickAnywhere && position.Contains(current.mousePosition))
				{
					GUIUtility.hotControl = id;
					float num = TimeArea.SnapTimeToWholeFPS(base.PixelToTime(current.mousePosition.x, position), frameRate);
					TimeArea.s_OriginalTime = time;
					if (num != time)
					{
						GUI.changed = true;
					}
					time = num;
					TimeArea.s_PickOffset = 0f;
					current.Use();
					return TimeArea.TimeRulerDragMode.Start;
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == id)
				{
					GUIUtility.hotControl = 0;
					current.Use();
					return TimeArea.TimeRulerDragMode.End;
				}
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == id)
				{
					float num2 = TimeArea.SnapTimeToWholeFPS(base.PixelToTime(current.mousePosition.x - TimeArea.s_PickOffset, position), frameRate);
					if (num2 != time)
					{
						GUI.changed = true;
					}
					time = num2;
					current.Use();
					return TimeArea.TimeRulerDragMode.Dragging;
				}
				break;
			case EventType.KeyDown:
				if (GUIUtility.hotControl == id && current.keyCode == KeyCode.Escape)
				{
					if (time != TimeArea.s_OriginalTime)
					{
						GUI.changed = true;
					}
					time = TimeArea.s_OriginalTime;
					GUIUtility.hotControl = 0;
					current.Use();
					return TimeArea.TimeRulerDragMode.Cancel;
				}
				break;
			case EventType.Repaint:
				if (time != -1f)
				{
					bool flag = position.Contains(current.mousePosition);
					position2.x += (float)thumbStyle.overflow.left;
					thumbStyle.Draw(position2, id == GUIUtility.hotControl, flag || id == GUIUtility.hotControl, false, false);
				}
				break;
			}
			return TimeArea.TimeRulerDragMode.None;
		}
		private void DrawLine(Vector2 lhs, Vector2 rhs)
		{
			GL.Vertex(base.DrawingToViewTransformPoint(new Vector3(lhs.x, lhs.y, 0f)));
			GL.Vertex(base.DrawingToViewTransformPoint(new Vector3(rhs.x, rhs.y, 0f)));
		}
		public float FrameToPixel(float i, float frameRate, Rect rect)
		{
			return (i - base.shownArea.xMin * frameRate) * rect.width / (base.shownArea.width * frameRate);
		}
		public string FormatFrame(int frame, float frameRate)
		{
			int length = ((int)frameRate).ToString().Length;
			string str = string.Empty;
			if (frame < 0)
			{
				str = "-";
				frame = -frame;
			}
			return str + (frame / (int)frameRate).ToString() + ":" + ((float)frame % frameRate).ToString().PadLeft(length, '0');
		}
		public static float SnapTimeToWholeFPS(float time, float frameRate)
		{
			if (frameRate == 0f)
			{
				return time;
			}
			return Mathf.Round(time * frameRate) / frameRate;
		}
	}
}