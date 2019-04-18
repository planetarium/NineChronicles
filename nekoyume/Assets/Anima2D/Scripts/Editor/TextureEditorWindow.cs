using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine;
using UnityEditor;
using UnityEngine.Sprites;
using UnityEditor.Sprites;

namespace Anima2D 
{
	public class TextureEditorWindow : EditorWindow
	{
		public Color textureColor = Color.white;

		protected class Styles
		{
			public readonly GUIStyle dragdot = "U2D.dragDot";
			public readonly GUIStyle dragdotDimmed = "U2D.dragDotDimmed";
			public readonly GUIStyle dragdotactive = "U2D.dragDotActive";
			public readonly GUIStyle createRect = "U2D.createRect";
			public readonly GUIStyle preToolbar = "preToolbar";
			public readonly GUIStyle preButton = "preButton";
			public readonly GUIStyle preLabel = "preLabel";
			public readonly GUIStyle preSlider = "preSlider";
			public readonly GUIStyle preSliderThumb = "preSliderThumb";
			public readonly GUIStyle preBackground = "preBackground";
			public readonly GUIStyle pivotdotactive = "U2D.pivotDotActive";
			public readonly GUIStyle pivotdot = "U2D.pivotDot";
			public readonly GUIStyle dragBorderdot = new GUIStyle();
			public readonly GUIStyle dragBorderDotActive = new GUIStyle();
			public readonly GUIStyle toolbar;
			public readonly GUIContent alphaIcon;
			public readonly GUIContent RGBIcon;
			public readonly GUIStyle notice;
			public readonly GUIContent smallMip;
			public readonly GUIContent largeMip;
			public readonly GUIContent spriteIcon;
			public readonly GUIContent showBonesIcon;

			Texture2D mShowBonesImage;
			Texture2D showBonesImage {
				get {
					if(!mShowBonesImage)
					{
						mShowBonesImage = EditorGUIUtility.Load("Anima2D/showBonesIcon.png") as Texture2D;
						mShowBonesImage.hideFlags = HideFlags.DontSave;
					}
					
					return mShowBonesImage;
				}
			}

			public Styles()
			{
				this.toolbar = new GUIStyle(EditorStyles.inspectorDefaultMargins);
				this.toolbar.margin.top = 0;
				this.toolbar.margin.bottom = 0;
				this.alphaIcon = EditorGUIUtility.IconContent("PreTextureAlpha");
				this.RGBIcon = EditorGUIUtility.IconContent("PreTextureRGB");
				this.preToolbar.border.top = 0;
				this.createRect.border = new RectOffset(3, 3, 3, 3);
				this.notice = new GUIStyle(GUI.skin.label);
				this.notice.alignment = TextAnchor.MiddleCenter;
				this.notice.normal.textColor = Color.yellow;
				this.dragBorderdot.fixedHeight = 5f;
				this.dragBorderdot.fixedWidth = 5f;
				this.dragBorderdot.normal.background = EditorGUIUtility.whiteTexture;
				this.dragBorderDotActive.fixedHeight = this.dragBorderdot.fixedHeight;
				this.dragBorderDotActive.fixedWidth = this.dragBorderdot.fixedWidth;
				this.dragBorderDotActive.normal.background = EditorGUIUtility.whiteTexture;
				this.smallMip = EditorGUIUtility.IconContent("PreTextureMipMapLow");
				this.largeMip = EditorGUIUtility.IconContent("PreTextureMipMapHigh");
				this.spriteIcon = EditorGUIUtility.IconContent("Sprite Icon");
				this.spriteIcon.tooltip = "Reset Sprite";
				this.showBonesIcon = new GUIContent(showBonesImage);
				this.showBonesIcon.tooltip = "Show Bones"; 
			}
		}

		public static string s_NoSelectionWarning = "No sprite selected";

		protected const float k_BorderMargin = 10f;
		protected const float k_ScrollbarMargin = 16f;
		protected const float k_InspectorWindowMargin = 8f;
		protected const float k_InspectorWidth = 330f;
		protected const float k_InspectorHeight = 148f;
		protected const float k_MinZoomPercentage = 0.9f;
		protected const float k_MaxZoom = 10f;
		protected const float k_WheelZoomSpeed = 0.03f;
		protected const float k_MouseZoomSpeed = 0.005f;
		protected static Styles s_Styles;
		protected Texture2D m_Texture;
		protected Rect m_TextureViewRect;
		protected Rect m_TextureRect;
		protected bool m_ShowAlpha;
		protected float m_Zoom = -1f;
		protected float m_MipLevel;
		protected Vector2 m_ScrollPosition = default(Vector2);

		private static Material s_HandleWireMaterial;
		private static Material s_HandleWireMaterial2D;
		
		static Material handleWireMaterial
		{
			get
			{
				if (!s_HandleWireMaterial)
				{
					s_HandleWireMaterial = (Material)EditorGUIUtility.LoadRequired("SceneView/HandleLines.mat");
					s_HandleWireMaterial2D = (Material)EditorGUIUtility.LoadRequired("SceneView/2DHandleLines.mat");
				}
				return (!Camera.current) ? s_HandleWireMaterial2D : s_HandleWireMaterial;
			}
		}

		static Texture2D transparentCheckerTexture
		{
			get
			{
				if (EditorGUIUtility.isProSkin)
				{
					return EditorGUIUtility.LoadRequired("Previews/Textures/textureCheckerDark.png") as Texture2D;
				}
				return EditorGUIUtility.LoadRequired("Previews/Textures/textureChecker.png") as Texture2D;
			}
		}

		protected Rect maxScrollRect
		{
			get
			{
				float num = (float)this.m_Texture.width * 0.5f * this.m_Zoom;
				float num2 = (float)this.m_Texture.height * 0.5f * this.m_Zoom;
				return new Rect(-num, -num2, this.m_TextureViewRect.width + num * 2f, this.m_TextureViewRect.height + num2 * 2f);
			}
		}
		protected Rect maxRect
		{
			get
			{
				float num = this.m_TextureViewRect.width * 0.5f / this.GetMinZoom();
				float num2 = this.m_TextureViewRect.height * 0.5f / this.GetMinZoom();
				float left = -num;
				float top = -num2;
				float width = (float)this.m_Texture.width + num * 2f;
				float height = (float)this.m_Texture.height + num2 * 2f;
				return new Rect(left, top, width, height);
			}
		}
		protected void InitStyles()
		{
			if (s_Styles == null)
			{
				s_Styles = new Styles();
			}
		}
		protected float GetMinZoom()
		{
			if (this.m_Texture == null)
			{
				return 1f;
			}
			return Mathf.Min(this.m_TextureViewRect.width / (float)this.m_Texture.width, this.m_TextureViewRect.height / (float)this.m_Texture.height) * 0.9f;
		}
		protected virtual void HandleZoom()
		{
			bool flag = Event.current.alt && Event.current.button == 1;
			if (flag)
			{
				EditorGUIUtility.AddCursorRect(this.m_TextureViewRect, MouseCursor.Zoom);
			}
			if (((Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseDown) && flag) || ((Event.current.type == EventType.KeyUp || Event.current.type == EventType.KeyDown) && Event.current.keyCode == KeyCode.LeftAlt))
			{
				base.Repaint();
			}
			if (Event.current.type == EventType.ScrollWheel || (Event.current.type == EventType.MouseDrag && Event.current.alt && Event.current.button == 1))
			{
				float num = 1f - Event.current.delta.y * ((Event.current.type != EventType.ScrollWheel) ? -0.005f : 0.03f);
				float num2 = this.m_Zoom * num;
				float num3 = Mathf.Clamp(num2, this.GetMinZoom(), 10f);
				if (num3 != this.m_Zoom)
				{
					this.m_Zoom = num3;
					if (num2 != num3)
					{
						num /= num2 / num3;
					}
					this.m_ScrollPosition *= num;
					Event.current.Use();
				}
			}
		}
		protected void HandlePanning()
		{
			bool flag = (!Event.current.alt && Event.current.button > 0) || (Event.current.alt && Event.current.button <= 0);
			if (flag && GUIUtility.hotControl == 0)
			{
				EditorGUIUtility.AddCursorRect(this.m_TextureViewRect, MouseCursor.Pan);
				if (Event.current.type == EventType.MouseDrag)
				{
					this.m_ScrollPosition -= Event.current.delta;
					Event.current.Use();
				}
			}
			if (((Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseDown) && flag) || ((Event.current.type == EventType.KeyUp || Event.current.type == EventType.KeyDown) && Event.current.keyCode == KeyCode.LeftAlt))
			{
				base.Repaint();
			}
		}

		public void DrawLine(Vector3 p1, Vector3 p2)
		{
			GL.Vertex(p1);
			GL.Vertex(p2);
		}
		public void BeginLines(Color color)
		{
			handleWireMaterial.SetPass(0);
			GL.PushMatrix();
			GL.MultMatrix(Handles.matrix);
			GL.Begin(1);
			GL.Color(color);
		}
		public void EndLines()
		{
			GL.End();
			GL.PopMatrix();
		}

		protected void DrawTexturespaceBackground()
		{
			float num = Mathf.Max(this.maxRect.width, this.maxRect.height);
			Vector2 b = new Vector2(this.maxRect.xMin, this.maxRect.yMin);
			float num2 = num * 0.5f;
			float a = (!EditorGUIUtility.isProSkin) ? 0.08f : 0.15f;
			float num3 = 8f;
			BeginLines(new Color(0f, 0f, 0f, a));
			for (float num4 = 0f; num4 <= num; num4 += num3)
			{
				float x = -num2 + num4 + b.x;
				float y = num2 + num4 + b.y;
				Vector2 p1 = new Vector2(x,y);
			
				x = num2 + num4 + b.x;
				y = -num2 + num4 + b.y;;
				Vector2 p2 = new Vector2(x, y);
				DrawLine(p1, p2);
			}
			EndLines();
		}
		private float Log2(float x)
		{
			return (float)(Math.Log((double)x) / Math.Log(2.0));
		}
		protected void DrawTexture()
		{
			int num = Mathf.Max(this.m_Texture.width, 1);
			float num2 = Mathf.Min(this.m_MipLevel, (float)(m_Texture.mipmapCount - 1));
			//float mipMapBias = this.m_Texture.mipMapBias;
			m_Texture.mipMapBias = (num2 - this.Log2((float)num / this.m_TextureRect.width));
			//FilterMode filterMode = this.m_Texture.filterMode;
			//m_Texture.filterMode = FilterMode.Point;
			Rect r = m_TextureRect;
			r.position -= m_ScrollPosition;

			if (this.m_ShowAlpha)
			{
				EditorGUI.DrawTextureAlpha(r, this.m_Texture);
			}
			else
			{
				GUI.DrawTextureWithTexCoords(r, transparentCheckerTexture,
				                             new Rect(r.width * -0.5f / (float)transparentCheckerTexture.width,
												         r.height * -0.5f / (float)transparentCheckerTexture.height,
												         r.width / (float)transparentCheckerTexture.width,
												         r.height / (float)transparentCheckerTexture.height), false);

				GUI.color = textureColor;
				GUI.DrawTexture(r, this.m_Texture);
			}
			//m_Texture.filterMode = filterMode;
			//m_Texture.mipMapBias = mipMapBias;
		}
		protected void DrawScreenspaceBackground()
		{
			if (Event.current.type == EventType.Repaint)
			{

				s_Styles.preBackground.Draw(this.m_TextureViewRect, false, false, false, false);
			}
		}
		protected void HandleScrollbars()
		{
			Rect position = new Rect(this.m_TextureViewRect.xMin, this.m_TextureViewRect.yMax, this.m_TextureViewRect.width, 16f);
			this.m_ScrollPosition.x = GUI.HorizontalScrollbar(position, this.m_ScrollPosition.x, this.m_TextureViewRect.width, this.maxScrollRect.xMin, this.maxScrollRect.xMax);
			Rect position2 = new Rect(this.m_TextureViewRect.xMax, this.m_TextureViewRect.yMin, 16f, this.m_TextureViewRect.height);
			this.m_ScrollPosition.y = GUI.VerticalScrollbar(position2, this.m_ScrollPosition.y, this.m_TextureViewRect.height, this.maxScrollRect.yMin, this.maxScrollRect.yMax);
		}
		protected void SetupHandlesMatrix()
		{
			Vector3 pos = new Vector3(this.m_TextureRect.x - m_ScrollPosition.x, this.m_TextureRect.yMax - m_ScrollPosition.y, 0f);
			Vector3 s = new Vector3(this.m_Zoom, -this.m_Zoom, 1f);
			Handles.matrix = Matrix4x4.TRS(pos, Quaternion.identity, s);
		}
		protected void DoAlphaZoomToolbarGUI()
		{
			this.m_ShowAlpha = GUILayout.Toggle(this.m_ShowAlpha, (!this.m_ShowAlpha) ? s_Styles.RGBIcon : s_Styles.alphaIcon, "toolbarButton", new GUILayoutOption[0]);
			this.m_Zoom = GUILayout.HorizontalSlider(this.m_Zoom, this.GetMinZoom(), 10f, s_Styles.preSlider, s_Styles.preSliderThumb, new GUILayoutOption[]
			{
				GUILayout.MaxWidth(64f)
			});
			int num = 1;

			if (this.m_Texture != null)
			{
				num = Mathf.Max(num, m_Texture.mipmapCount);
			}

			EditorGUI.BeginDisabledGroup(num == 1);
			GUILayout.Box(s_Styles.smallMip, s_Styles.preLabel, new GUILayoutOption[0]);
			this.m_MipLevel = Mathf.Round(GUILayout.HorizontalSlider(this.m_MipLevel, (float)(num - 1), 0f, s_Styles.preSlider, s_Styles.preSliderThumb, new GUILayoutOption[]
			{
				GUILayout.MaxWidth(64f)
			}));
			GUILayout.Box(s_Styles.largeMip, s_Styles.preLabel, new GUILayoutOption[0]);
			EditorGUI.EndDisabledGroup();

		}
		
		protected void DoTextureGUI()
		{
			if (m_Zoom < 0f)
				m_Zoom = GetMinZoom();

			m_TextureRect = new Rect(m_TextureViewRect.width / 2f - (float)m_Texture.width * m_Zoom / 2f,
			                         m_TextureViewRect.height / 2f - (float)m_Texture.height * m_Zoom / 2f,
			                         (float)m_Texture.width * m_Zoom,
			                         (float)m_Texture.height * m_Zoom);

			HandleScrollbars();
			SetupHandlesMatrix();

			DrawScreenspaceBackground();

			GUI.BeginGroup(m_TextureViewRect);

			HandleEvents();

			if (Event.current.type == EventType.Repaint)
			{
				DrawTexturespaceBackground();
				DrawTexture();
				DrawGizmos();
			}

			DoTextureGUIExtras();

			GUI.EndGroup();
		}

		protected virtual void HandleEvents()
		{
		}
		protected virtual void DoTextureGUIExtras()
		{
		}
		protected virtual void DrawGizmos()
		{
		}
		protected void SetNewTexture(Texture2D texture)
		{
			if (texture != this.m_Texture)
			{
				this.m_Texture = texture;
				this.m_Zoom = -1f;
			}
		}
		
		protected virtual void DoToolbarGUI()
		{
		}

		protected virtual void OnGUI()
		{
			if(m_Texture)
			{
				InitStyles();

				EditorGUILayout.BeginHorizontal((GUIStyle) "Toolbar");
				DoToolbarGUI();
				EditorGUILayout.EndHorizontal();

				m_TextureViewRect = new Rect(0f, 16f, base.position.width - 16f, base.position.height - 16f - 16f);

				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				DoTextureGUI();
				EditorGUILayout.EndHorizontal();
			}
		}
	}
}
