using System;
using System.Reflection;
using UnityEngine;
using UnityEditor;

namespace Anima2D
{
	[Serializable]
	public class ZoomableArea
	{
		[Serializable]
		public class Styles
		{
			public GUIStyle horizontalScrollbar;
			public GUIStyle horizontalMinMaxScrollbarThumb;
			public GUIStyle horizontalScrollbarLeftButton;
			public GUIStyle horizontalScrollbarRightButton;
			public GUIStyle verticalScrollbar;
			public GUIStyle verticalMinMaxScrollbarThumb;
			public GUIStyle verticalScrollbarUpButton;
			public GUIStyle verticalScrollbarDownButton;
			public float sliderWidth;
			public float visualSliderWidth;
			public Styles(bool minimalGUI)
			{
				if (minimalGUI)
				{
					this.visualSliderWidth = 0f;
					this.sliderWidth = 15f;
				}
				else
				{
					this.visualSliderWidth = 15f;
					this.sliderWidth = 15f;
				}
			}
			public void InitGUIStyles(bool minimalGUI)
			{
				if (minimalGUI)
				{
					this.horizontalMinMaxScrollbarThumb = "MiniMinMaxSliderHorizontal";
					this.horizontalScrollbarLeftButton = GUIStyle.none;
					this.horizontalScrollbarRightButton = GUIStyle.none;
					this.horizontalScrollbar = GUIStyle.none;
					this.verticalMinMaxScrollbarThumb = "MiniMinMaxSlidervertical";
					this.verticalScrollbarUpButton = GUIStyle.none;
					this.verticalScrollbarDownButton = GUIStyle.none;
					this.verticalScrollbar = GUIStyle.none;
				}
				else
				{
					this.horizontalMinMaxScrollbarThumb = "horizontalMinMaxScrollbarThumb";
					this.horizontalScrollbarLeftButton = "horizontalScrollbarLeftbutton";
					this.horizontalScrollbarRightButton = "horizontalScrollbarRightbutton";
					this.horizontalScrollbar = GUI.skin.horizontalScrollbar;
					this.verticalMinMaxScrollbarThumb = "verticalMinMaxScrollbarThumb";
					this.verticalScrollbarUpButton = "verticalScrollbarUpbutton";
					this.verticalScrollbarDownButton = "verticalScrollbarDownbutton";
					this.verticalScrollbar = GUI.skin.verticalScrollbar;
				}
			}
		}
		private static Vector2 m_MouseDownPosition = new Vector2(-1000000f, -1000000f);
		private static int zoomableAreaHash = "ZoomableArea".GetHashCode();
		private bool m_HRangeLocked;
		private bool m_VRangeLocked;
		private float m_HBaseRangeMin;
		private float m_HBaseRangeMax = 1f;
		private float m_VBaseRangeMin;
		private float m_VBaseRangeMax = 1f;
		private bool m_HAllowExceedBaseRangeMin = true;
		private bool m_HAllowExceedBaseRangeMax = true;
		private bool m_VAllowExceedBaseRangeMin = true;
		private bool m_VAllowExceedBaseRangeMax = true;
		private float m_HScaleMin = 0.001f;
		private float m_HScaleMax = 100000f;
		private float m_VScaleMin = 0.001f;
		private float m_VScaleMax = 100000f;
		private bool m_ScaleWithWindow;
		private bool m_HSlider = true;
		private bool m_VSlider = true;
		public bool m_UniformScale;
		private bool m_IgnoreScrollWheelUntilClicked;
		private Rect m_DrawArea = new Rect(0f, 0f, 100f, 100f);
		internal Vector2 m_Scale = new Vector2(1f, -1f);
		internal Vector2 m_Translation = new Vector2(0f, 0f);
		private float m_MarginLeft;
		private float m_MarginRight;
		private float m_MarginTop;
		private float m_MarginBottom;
		private Rect m_LastShownAreaInsideMargins = new Rect(0f, 0f, 100f, 100f);
		private int verticalScrollbarID;
		private int horizontalScrollbarID;
		private bool m_MinimalGUI;
		private ZoomableArea.Styles styles;
		public bool hRangeLocked
		{
			get
			{
				return this.m_HRangeLocked;
			}
			set
			{
				this.m_HRangeLocked = value;
			}
		}
		public bool vRangeLocked
		{
			get
			{
				return this.m_VRangeLocked;
			}
			set
			{
				this.m_VRangeLocked = value;
			}
		}
		public float hBaseRangeMin
		{
			get
			{
				return this.m_HBaseRangeMin;
			}
			set
			{
				this.m_HBaseRangeMin = value;
			}
		}
		public float hBaseRangeMax
		{
			get
			{
				return this.m_HBaseRangeMax;
			}
			set
			{
				this.m_HBaseRangeMax = value;
			}
		}
		public float vBaseRangeMin
		{
			get
			{
				return this.m_VBaseRangeMin;
			}
			set
			{
				this.m_VBaseRangeMin = value;
			}
		}
		public float vBaseRangeMax
		{
			get
			{
				return this.m_VBaseRangeMax;
			}
			set
			{
				this.m_VBaseRangeMax = value;
			}
		}
		public bool hAllowExceedBaseRangeMin
		{
			get
			{
				return this.m_HAllowExceedBaseRangeMin;
			}
			set
			{
				this.m_HAllowExceedBaseRangeMin = value;
			}
		}
		public bool hAllowExceedBaseRangeMax
		{
			get
			{
				return this.m_HAllowExceedBaseRangeMax;
			}
			set
			{
				this.m_HAllowExceedBaseRangeMax = value;
			}
		}
		public bool vAllowExceedBaseRangeMin
		{
			get
			{
				return this.m_VAllowExceedBaseRangeMin;
			}
			set
			{
				this.m_VAllowExceedBaseRangeMin = value;
			}
		}
		public bool vAllowExceedBaseRangeMax
		{
			get
			{
				return this.m_VAllowExceedBaseRangeMax;
			}
			set
			{
				this.m_VAllowExceedBaseRangeMax = value;
			}
		}
		public float hRangeMin
		{
			get
			{
				return (!this.hAllowExceedBaseRangeMin) ? this.hBaseRangeMin : float.NegativeInfinity;
			}
			set
			{
				this.SetAllowExceed(ref this.m_HBaseRangeMin, ref this.m_HAllowExceedBaseRangeMin, value);
			}
		}
		public float hRangeMax
		{
			get
			{
				return (!this.hAllowExceedBaseRangeMax) ? this.hBaseRangeMax : float.PositiveInfinity;
			}
			set
			{
				this.SetAllowExceed(ref this.m_HBaseRangeMax, ref this.m_HAllowExceedBaseRangeMax, value);
			}
		}
		public float vRangeMin
		{
			get
			{
				return (!this.vAllowExceedBaseRangeMin) ? this.vBaseRangeMin : float.NegativeInfinity;
			}
			set
			{
				this.SetAllowExceed(ref this.m_VBaseRangeMin, ref this.m_VAllowExceedBaseRangeMin, value);
			}
		}
		public float vRangeMax
		{
			get
			{
				return (!this.vAllowExceedBaseRangeMax) ? this.vBaseRangeMax : float.PositiveInfinity;
			}
			set
			{
				this.SetAllowExceed(ref this.m_VBaseRangeMax, ref this.m_VAllowExceedBaseRangeMax, value);
			}
		}
		public bool scaleWithWindow
		{
			get
			{
				return this.m_ScaleWithWindow;
			}
			set
			{
				this.m_ScaleWithWindow = value;
			}
		}
		public bool hSlider
		{
			get
			{
				return this.m_HSlider;
			}
			set
			{
				Rect rect = this.rect;
				this.m_HSlider = value;
				this.rect = rect;
			}
		}
		public bool vSlider
		{
			get
			{
				return this.m_VSlider;
			}
			set
			{
				Rect rect = this.rect;
				this.m_VSlider = value;
				this.rect = rect;
			}
		}
		public bool uniformScale
		{
			get
			{
				return this.m_UniformScale;
			}
			set
			{
				this.m_UniformScale = value;
			}
		}
		public bool ignoreScrollWheelUntilClicked
		{
			get
			{
				return this.m_IgnoreScrollWheelUntilClicked;
			}
			set
			{
				this.m_IgnoreScrollWheelUntilClicked = value;
			}
		}
		public Vector2 scale
		{
			get
			{
				return this.m_Scale;
			}
		}
		public float margin
		{
			set
			{
				this.m_MarginBottom = value;
				this.m_MarginTop = value;
				this.m_MarginRight = value;
				this.m_MarginLeft = value;
			}
		}
		public float leftmargin
		{
			get
			{
				return this.m_MarginLeft;
			}
			set
			{
				this.m_MarginLeft = value;
			}
		}
		public float rightmargin
		{
			get
			{
				return this.m_MarginRight;
			}
			set
			{
				this.m_MarginRight = value;
			}
		}
		public float topmargin
		{
			get
			{
				return this.m_MarginTop;
			}
			set
			{
				this.m_MarginTop = value;
			}
		}
		public float bottommargin
		{
			get
			{
				return this.m_MarginBottom;
			}
			set
			{
				this.m_MarginBottom = value;
			}
		}
		public Rect rect
		{
			get
			{
				return new Rect(this.drawRect.x, this.drawRect.y, this.drawRect.width + ((!this.m_VSlider) ? 0f : this.styles.visualSliderWidth), this.drawRect.height + ((!this.m_HSlider) ? 0f : this.styles.visualSliderWidth));
			}
			set
			{
				Rect rect = new Rect(value.x, value.y, value.width - ((!this.m_VSlider) ? 0f : this.styles.visualSliderWidth), value.height - ((!this.m_HSlider) ? 0f : this.styles.visualSliderWidth));
				if (rect != this.m_DrawArea)
				{
					if (this.m_ScaleWithWindow)
					{
						this.m_DrawArea = rect;
						this.shownAreaInsideMargins = this.m_LastShownAreaInsideMargins;
					}
					else
					{
						this.m_Translation += new Vector2((rect.width - this.m_DrawArea.width) / 2f, (rect.height - this.m_DrawArea.height) / 2f);
						this.m_DrawArea = rect;
					}
				}
				this.EnforceScaleAndRange();
			}
		}
		public Rect drawRect
		{
			get
			{
				return this.m_DrawArea;
			}
		}
		public Rect shownArea
		{
			get
			{
				return new Rect(-this.m_Translation.x / this.m_Scale.x, -(this.m_Translation.y - this.drawRect.height) / this.m_Scale.y, this.drawRect.width / this.m_Scale.x, this.drawRect.height / -this.m_Scale.y);
			}
			set
			{
				this.m_Scale.x = this.drawRect.width / value.width;
				this.m_Scale.y = -this.drawRect.height / value.height;
				this.m_Translation.x = -value.x * this.m_Scale.x;
				this.m_Translation.y = this.drawRect.height - value.y * this.m_Scale.y;
				this.EnforceScaleAndRange();
			}
		}
		public Rect shownAreaInsideMargins
		{
			get
			{
				return this.shownAreaInsideMarginsInternal;
			}
			set
			{
				this.shownAreaInsideMarginsInternal = value;
				this.EnforceScaleAndRange();
			}
		}
		private Rect shownAreaInsideMarginsInternal
		{
			get
			{
				float num = this.leftmargin / this.m_Scale.x;
				float num2 = this.rightmargin / this.m_Scale.x;
				float num3 = this.topmargin / this.m_Scale.y;
				float num4 = this.bottommargin / this.m_Scale.y;
				Rect shownArea = this.shownArea;
				shownArea.x += num;
				shownArea.y -= num3;
				shownArea.width -= num + num2;
				shownArea.height += num3 + num4;
				return shownArea;
			}
			set
			{
				this.m_Scale.x = (this.drawRect.width - this.leftmargin - this.rightmargin) / value.width;
				this.m_Scale.y = -(this.drawRect.height - this.topmargin - this.bottommargin) / value.height;
				this.m_Translation.x = -value.x * this.m_Scale.x + this.leftmargin;
				this.m_Translation.y = this.drawRect.height - value.y * this.m_Scale.y - this.topmargin;
			}
		}
		public virtual Bounds drawingBounds
		{
			get
			{
				return new Bounds(new Vector3((this.hBaseRangeMin + this.hBaseRangeMax) * 0.5f, (this.vBaseRangeMin + this.vBaseRangeMax) * 0.5f, 0f), new Vector3(this.hBaseRangeMax - this.hBaseRangeMin, this.vBaseRangeMax - this.vBaseRangeMin, 1f));
			}
		}
		public Matrix4x4 drawingToViewMatrix
		{
			get
			{
				return Matrix4x4.TRS(this.m_Translation, Quaternion.identity, new Vector3(this.m_Scale.x, this.m_Scale.y, 1f));
			}
		}
		public Vector2 mousePositionInDrawing
		{
			get
			{
				return this.ViewToDrawingTransformPoint(Event.current.mousePosition);
			}
		}
		public ZoomableArea()
		{
			this.m_MinimalGUI = false;
			this.styles = new ZoomableArea.Styles(false);
		}
		public ZoomableArea(bool minimalGUI)
		{
			this.m_MinimalGUI = minimalGUI;
			this.styles = new ZoomableArea.Styles(minimalGUI);
		}
		private void SetAllowExceed(ref float rangeEnd, ref bool allowExceed, float value)
		{
			if (value == float.NegativeInfinity || value == float.PositiveInfinity)
			{
				rangeEnd = (float)((value != float.NegativeInfinity) ? 1 : 0);
				allowExceed = true;
			}
			else
			{
				rangeEnd = value;
				allowExceed = false;
			}
		}
		internal void SetDrawRectHack(Rect r, bool scrollbars)
		{
			this.m_DrawArea = r;
			this.m_VSlider = scrollbars;
			this.m_HSlider = scrollbars;
		}
		public void OnEnable()
		{
			this.styles = new ZoomableArea.Styles(this.m_MinimalGUI);
		}
		public void SetShownHRangeInsideMargins(float min, float max)
		{
			this.m_Scale.x = (this.drawRect.width - this.leftmargin - this.rightmargin) / (max - min);
			this.m_Translation.x = -min * this.m_Scale.x + this.leftmargin;
			this.EnforceScaleAndRange();
		}
		public void SetShownHRange(float min, float max)
		{
			this.m_Scale.x = this.drawRect.width / (max - min);
			this.m_Translation.x = -min * this.m_Scale.x;
			this.EnforceScaleAndRange();
		}
		public void SetShownVRangeInsideMargins(float min, float max)
		{
			this.m_Scale.y = -(this.drawRect.height - this.topmargin - this.bottommargin) / (max - min);
			this.m_Translation.y = this.drawRect.height - min * this.m_Scale.y - this.topmargin;
			this.EnforceScaleAndRange();
		}
		public void SetShownVRange(float min, float max)
		{
			this.m_Scale.y = -this.drawRect.height / (max - min);
			this.m_Translation.y = this.drawRect.height - min * this.m_Scale.y;
			this.EnforceScaleAndRange();
		}
		public Vector2 DrawingToViewTransformPoint(Vector2 lhs)
		{
			return new Vector2(lhs.x * this.m_Scale.x + this.m_Translation.x, lhs.y * this.m_Scale.y + this.m_Translation.y);
		}
		public Vector3 DrawingToViewTransformPoint(Vector3 lhs)
		{
			return new Vector3(lhs.x * this.m_Scale.x + this.m_Translation.x, lhs.y * this.m_Scale.y + this.m_Translation.y, 0f);
		}
		public Vector2 ViewToDrawingTransformPoint(Vector2 lhs)
		{
			return new Vector2((lhs.x - this.m_Translation.x) / this.m_Scale.x, (lhs.y - this.m_Translation.y) / this.m_Scale.y);
		}
		public Vector3 ViewToDrawingTransformPoint(Vector3 lhs)
		{
			return new Vector3((lhs.x - this.m_Translation.x) / this.m_Scale.x, (lhs.y - this.m_Translation.y) / this.m_Scale.y, 0f);
		}
		public Vector2 DrawingToViewTransformVector(Vector2 lhs)
		{
			return new Vector2(lhs.x * this.m_Scale.x, lhs.y * this.m_Scale.y);
		}
		public Vector3 DrawingToViewTransformVector(Vector3 lhs)
		{
			return new Vector3(lhs.x * this.m_Scale.x, lhs.y * this.m_Scale.y, 0f);
		}
		public Vector2 ViewToDrawingTransformVector(Vector2 lhs)
		{
			return new Vector2(lhs.x / this.m_Scale.x, lhs.y / this.m_Scale.y);
		}
		public Vector3 ViewToDrawingTransformVector(Vector3 lhs)
		{
			return new Vector3(lhs.x / this.m_Scale.x, lhs.y / this.m_Scale.y, 0f);
		}
		public Vector2 NormalizeInViewSpace(Vector2 vec)
		{
			vec = Vector2.Scale(vec, this.m_Scale);
			vec /= vec.magnitude;
			return Vector2.Scale(vec, new Vector2(1f / this.m_Scale.x, 1f / this.m_Scale.y));
		}
		private bool IsZoomEvent()
		{
			return Event.current.button == 1 && Event.current.alt;
		}
		private bool IsPanEvent()
		{
			return (Event.current.button == 0 && Event.current.alt) || (Event.current.button == 2 && !Event.current.command);
		}
		public void BeginViewGUI()
		{
			if (this.styles.horizontalScrollbar == null)
			{
				this.styles.InitGUIStyles(this.m_MinimalGUI);
			}
			this.HandleZoomAndPanEvents(this.m_DrawArea);
			this.horizontalScrollbarID = GUIUtility.GetControlID(EditorGUIExtra.s_MinMaxSliderHash, FocusType.Passive);
			this.verticalScrollbarID = GUIUtility.GetControlID(EditorGUIExtra.s_MinMaxSliderHash, FocusType.Passive);
			if (!this.m_MinimalGUI || Event.current.type != EventType.Repaint)
			{
				this.SliderGUI();
			}
		}
		public void HandleZoomAndPanEvents(Rect area)
		{
			GUILayout.BeginArea(area);
			area.x = 0f;
			area.y = 0f;
			int controlID = GUIUtility.GetControlID(ZoomableArea.zoomableAreaHash, FocusType.Passive, area);
			switch (Event.current.GetTypeForControl(controlID))
			{
			case EventType.MouseDown:
				if (area.Contains(Event.current.mousePosition))
				{
					GUIUtility.keyboardControl = controlID;
					if (this.IsZoomEvent() || this.IsPanEvent())
					{
						GUIUtility.hotControl = controlID;
						ZoomableArea.m_MouseDownPosition = this.mousePositionInDrawing;
						Event.current.Use();
					}
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == controlID)
				{
					GUIUtility.hotControl = 0;
					ZoomableArea.m_MouseDownPosition = new Vector2(-1000000f, -1000000f);
				}
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == controlID)
				{
					if (this.IsZoomEvent())
					{
						this.Zoom(ZoomableArea.m_MouseDownPosition, false);
						Event.current.Use();
					}
					else
					{
						if (this.IsPanEvent())
						{
							this.Pan();
							Event.current.Use();
						}
					}
				}
				break;
			case EventType.ScrollWheel:
				if (area.Contains(Event.current.mousePosition))
				{
					if (!this.m_IgnoreScrollWheelUntilClicked || GUIUtility.keyboardControl == controlID)
					{
						this.Zoom(this.mousePositionInDrawing, true);
						Event.current.Use();
					}
				}
				break;
			}
			GUILayout.EndArea();
		}
		public void EndViewGUI()
		{
			if (this.m_MinimalGUI && Event.current.type == EventType.Repaint)
			{
				this.SliderGUI();
			}
		}
		private void SliderGUI()
		{
			if (!this.m_HSlider && !this.m_VSlider)
			{
				return;
			}
			Bounds drawingBounds = this.drawingBounds;
			Rect shownAreaInsideMargins = this.shownAreaInsideMargins;
			float num = this.styles.sliderWidth - this.styles.visualSliderWidth;
			float num2 = (!this.vSlider || !this.hSlider) ? 0f : num;
			Vector2 a = this.m_Scale;
			if (this.m_HSlider)
			{
				Rect position = new Rect(this.drawRect.x + 1f, this.drawRect.yMax - num, this.drawRect.width - num2, this.styles.sliderWidth);
				float width = shownAreaInsideMargins.width;
				float xMin = shownAreaInsideMargins.xMin;
				EditorGUIExtra.MinMaxScroller(position, this.horizontalScrollbarID, ref xMin, ref width, drawingBounds.min.x, drawingBounds.max.x, float.NegativeInfinity, float.PositiveInfinity, this.styles.horizontalScrollbar, this.styles.horizontalMinMaxScrollbarThumb, this.styles.horizontalScrollbarLeftButton, this.styles.horizontalScrollbarRightButton, true);
				float num3 = xMin;
				float num4 = xMin + width;
				if (num3 > shownAreaInsideMargins.xMin)
				{
					num3 = Mathf.Min(num3, num4 - this.m_HScaleMin);
				}
				if (num4 < shownAreaInsideMargins.xMax)
				{
					num4 = Mathf.Max(num4, num3 + this.m_HScaleMin);
				}
				this.SetShownHRangeInsideMargins(num3, num4);
			}
			if (this.m_VSlider)
			{
				Rect position2 = new Rect(this.drawRect.xMax - num, this.drawRect.y, this.styles.sliderWidth, this.drawRect.height - num2);
				float height = shownAreaInsideMargins.height;
				float num5 = -shownAreaInsideMargins.yMax;
				EditorGUIExtra.MinMaxScroller(position2, this.verticalScrollbarID, ref num5, ref height, -drawingBounds.max.y, -drawingBounds.min.y, float.NegativeInfinity, float.PositiveInfinity, this.styles.verticalScrollbar, this.styles.verticalMinMaxScrollbarThumb, this.styles.verticalScrollbarUpButton, this.styles.verticalScrollbarDownButton, false);
				float num3 = -(num5 + height);
				float num4 = -num5;
				if (num3 > shownAreaInsideMargins.yMin)
				{
					num3 = Mathf.Min(num3, num4 - this.m_VScaleMin);
				}
				if (num4 < shownAreaInsideMargins.yMax)
				{
					num4 = Mathf.Max(num4, num3 + this.m_VScaleMin);
				}
				this.SetShownVRangeInsideMargins(num3, num4);
			}
			if (this.uniformScale)
			{
				float num6 = this.drawRect.width / this.drawRect.height;
				a -= this.m_Scale;
				Vector2 b = new Vector2(-a.y * num6, -a.x / num6);
				this.m_Scale -= b;
				this.m_Translation.x = this.m_Translation.x - a.y / 2f;
				this.m_Translation.y = this.m_Translation.y - a.x / 2f;
				this.EnforceScaleAndRange();
			}
		}
		private void Pan()
		{
			if (!this.m_HRangeLocked)
			{
				this.m_Translation.x = this.m_Translation.x + Event.current.delta.x;
			}
			if (!this.m_VRangeLocked)
			{
				this.m_Translation.y = this.m_Translation.y + Event.current.delta.y;
			}
			this.EnforceScaleAndRange();
		}
		private void Zoom(Vector2 zoomAround, bool scrollwhell)
		{
			float num = Event.current.delta.x + Event.current.delta.y;
			if (scrollwhell)
			{
				num = -num;
			}
			float num2 = Mathf.Max(0.01f, 1f + num * 0.01f);
			if (!this.m_HRangeLocked)
			{
				this.m_Translation.x = this.m_Translation.x - zoomAround.x * (num2 - 1f) * this.m_Scale.x;
				this.m_Scale.x = this.m_Scale.x * num2;
			}
			if (!this.m_VRangeLocked)
			{
				this.m_Translation.y = this.m_Translation.y - zoomAround.y * (num2 - 1f) * this.m_Scale.y;
				this.m_Scale.y = this.m_Scale.y * num2;
			}
			this.EnforceScaleAndRange();
		}
		private void EnforceScaleAndRange()
		{
			float hScaleMin = this.m_HScaleMin;
			float vScaleMin = this.m_VScaleMin;
			float value = this.m_HScaleMax;
			float value2 = this.m_VScaleMax;
			if (this.hRangeMax != float.PositiveInfinity && this.hRangeMin != float.NegativeInfinity)
			{
				value = Mathf.Min(this.m_HScaleMax, this.hRangeMax - this.hRangeMin);
			}
			if (this.vRangeMax != float.PositiveInfinity && this.vRangeMin != float.NegativeInfinity)
			{
				value2 = Mathf.Min(this.m_VScaleMax, this.vRangeMax - this.vRangeMin);
			}
			Rect lastShownAreaInsideMargins = this.m_LastShownAreaInsideMargins;
			Rect shownAreaInsideMargins = this.shownAreaInsideMargins;
			if (shownAreaInsideMargins == lastShownAreaInsideMargins)
			{
				return;
			}
			float num = 1E-05f;
			if (shownAreaInsideMargins.width < lastShownAreaInsideMargins.width - num)
			{
				float t = Mathf.InverseLerp(lastShownAreaInsideMargins.width, shownAreaInsideMargins.width, hScaleMin);
				shownAreaInsideMargins = new Rect(Mathf.Lerp(lastShownAreaInsideMargins.x, shownAreaInsideMargins.x, t), shownAreaInsideMargins.y, Mathf.Lerp(lastShownAreaInsideMargins.width, shownAreaInsideMargins.width, t), shownAreaInsideMargins.height);
			}
			if (shownAreaInsideMargins.height < lastShownAreaInsideMargins.height - num)
			{
				float t2 = Mathf.InverseLerp(lastShownAreaInsideMargins.height, shownAreaInsideMargins.height, vScaleMin);
				shownAreaInsideMargins = new Rect(shownAreaInsideMargins.x, Mathf.Lerp(lastShownAreaInsideMargins.y, shownAreaInsideMargins.y, t2), shownAreaInsideMargins.width, Mathf.Lerp(lastShownAreaInsideMargins.height, shownAreaInsideMargins.height, t2));
			}
			if (shownAreaInsideMargins.width > lastShownAreaInsideMargins.width + num)
			{
				float t3 = Mathf.InverseLerp(lastShownAreaInsideMargins.width, shownAreaInsideMargins.width, value);
				shownAreaInsideMargins = new Rect(Mathf.Lerp(lastShownAreaInsideMargins.x, shownAreaInsideMargins.x, t3), shownAreaInsideMargins.y, Mathf.Lerp(lastShownAreaInsideMargins.width, shownAreaInsideMargins.width, t3), shownAreaInsideMargins.height);
			}
			if (shownAreaInsideMargins.height > lastShownAreaInsideMargins.height + num)
			{
				float t4 = Mathf.InverseLerp(lastShownAreaInsideMargins.height, shownAreaInsideMargins.height, value2);
				shownAreaInsideMargins = new Rect(shownAreaInsideMargins.x, Mathf.Lerp(lastShownAreaInsideMargins.y, shownAreaInsideMargins.y, t4), shownAreaInsideMargins.width, Mathf.Lerp(lastShownAreaInsideMargins.height, shownAreaInsideMargins.height, t4));
			}
			if (shownAreaInsideMargins.xMin < this.hRangeMin)
			{
				shownAreaInsideMargins.x = this.hRangeMin;
			}
			if (shownAreaInsideMargins.xMax > this.hRangeMax)
			{
				shownAreaInsideMargins.x = this.hRangeMax - shownAreaInsideMargins.width;
			}
			if (shownAreaInsideMargins.yMin < this.vRangeMin)
			{
				shownAreaInsideMargins.y = this.vRangeMin;
			}
			if (shownAreaInsideMargins.yMax > this.vRangeMax)
			{
				shownAreaInsideMargins.y = this.vRangeMax - shownAreaInsideMargins.height;
			}
			this.shownAreaInsideMarginsInternal = shownAreaInsideMargins;
			this.m_LastShownAreaInsideMargins = shownAreaInsideMargins;
		}
		public float PixelToTime(float pixelX, Rect rect)
		{
			return (pixelX - rect.x) * this.shownArea.width / rect.width + this.shownArea.x;
		}
		public float TimeToPixel(float time, Rect rect)
		{
			return (time - this.shownArea.x) / this.shownArea.width * rect.width + rect.x;
		}
		public float PixelDeltaToTime(Rect rect)
		{
			return this.shownArea.width / rect.width;
		}
	}
}