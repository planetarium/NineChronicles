using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Reflection;

namespace Anima2D 
{
	public class HandlesExtra
	{
		public delegate void CapFunction(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType);

		public class Styles
		{
			public readonly GUIStyle dragDot = "U2D.dragDot";
			public readonly GUIStyle dragDotActive = "U2D.dragDotActive";
			public readonly GUIStyle pivotDot = "U2D.pivotDot";
			public readonly GUIStyle pivotDotActive = "U2D.pivotDotActive";
			public readonly GUIStyle dotCyan;
			public readonly GUIStyle dotYellow;
			public readonly GUIStyle dotRed;
			public readonly GUIStyle dotBlackBig;
			public readonly GUIStyle dotYellowBig;

			public Styles()
			{
				dotCyan = new GUIStyle();
				dotYellow = new GUIStyle();
				dotRed = new GUIStyle();
				dotBlackBig = new GUIStyle();
				dotYellowBig = new GUIStyle();

				dotCyan.fixedWidth = 8f;
				dotCyan.fixedHeight = 8f;
				dotCyan.normal.background = EditorGUIUtility.LoadRequired("Anima2D/dotCyan.png") as Texture2D;

				dotYellow.fixedWidth = 8f;
				dotYellow.fixedHeight = 8f;
				dotYellow.normal.background = EditorGUIUtility.LoadRequired("Anima2D/dotYellow.png") as Texture2D;

				dotRed.fixedWidth = 8f;
				dotRed.fixedHeight = 8f;
				dotRed.normal.background = EditorGUIUtility.LoadRequired("Anima2D/dotRed.png") as Texture2D;

				dotBlackBig.fixedWidth = 23.5f;
				dotBlackBig.fixedHeight = 23.5f;
				dotBlackBig.normal.background = EditorGUIUtility.LoadRequired("Anima2D/dotBlackBig.png") as Texture2D;

				dotYellowBig.fixedWidth = 23.5f;
				dotYellowBig.fixedHeight = 23.5f;
				dotYellowBig.normal.background = EditorGUIUtility.LoadRequired("Anima2D/dotYellowBig.png") as Texture2D;
			}
		}

		static Styles s_Styles;
		public static Styles styles
		{
			get {
				if (s_Styles == null)
				{
					s_Styles = new Styles();
				}

				return s_Styles;
			}	
		}

		static Material s_HandleWireMaterial;
		static Material s_HandleWireMaterial2D;
		static MethodInfo s_ApplyWireMaterialMethodInfo;

		private static Vector2 s_CurrentMousePosition;
		private static Vector2 s_DragStartScreenPosition;
		private static Vector2 s_DragScreenOffset;

		static Vector3[] s_circleArray;

		public static Vector3 GUIToWorld(Vector3 guiPosition)
		{
			return GUIToWorld(guiPosition, Vector3.forward, Vector3.zero);
		}

		public static Vector3 GUIToWorld(Vector3 guiPosition, Vector3 planeNormal, Vector3 planePos)
		{
			Vector3 worldPos = Handles.inverseMatrix.MultiplyPoint(guiPosition);

			if(Camera.current)
			{
				Ray ray = HandleUtility.GUIPointToWorldRay(guiPosition);

				planeNormal = Handles.matrix.MultiplyVector(planeNormal);

				planePos = Handles.matrix.MultiplyPoint(planePos);

				Plane plane = new Plane(planeNormal,planePos);

				float distance = 0f;

				if(plane.Raycast(ray, out distance))
				{
					worldPos = Handles.inverseMatrix.MultiplyPoint(ray.GetPoint(distance));
				}
			}

			return worldPos;
		}

		public static Vector2 Slider2D(int id, Vector2 position, CapFunction drawCapFunction)
		{
			return Slider2D(id, position, drawCapFunction, Vector3.forward, Vector3.zero);
		}

		public static Vector2 Slider2D(int id, Vector2 position, CapFunction drawCapFunction, Vector3 planeNormal, Vector3 planePosition)
		{
			EventType eventType = Event.current.GetTypeForControl(id);
			
			switch(eventType)
			{
			case EventType.MouseDown:
				if (Event.current.button == 0 && HandleUtility.nearestControl == id && !Event.current.alt)
				{
					GUIUtility.keyboardControl = id;
					GUIUtility.hotControl = id;
					s_CurrentMousePosition = Event.current.mousePosition;
					s_DragStartScreenPosition = Event.current.mousePosition;
					Vector2 b = HandleUtility.WorldToGUIPoint(position);
					s_DragScreenOffset = s_CurrentMousePosition - b;
					EditorGUIUtility.SetWantsMouseJumping(1);
					
					Event.current.Use();
				}
				break;
			case EventType.MouseUp:
				if (GUIUtility.hotControl == id && (Event.current.button == 0 || Event.current.button == 2))
				{
					GUIUtility.hotControl = 0;
					Event.current.Use();
					EditorGUIUtility.SetWantsMouseJumping(0);
				}
				break;
			case EventType.MouseDrag:
				if (GUIUtility.hotControl == id)
				{
					s_CurrentMousePosition = Event.current.mousePosition;
					Vector2 center = position;
					position = GUIToWorld(s_CurrentMousePosition - s_DragScreenOffset, planeNormal, planePosition);
					if (!Mathf.Approximately((center - position).magnitude, 0f))
					{
						GUI.changed = true;
					}
					Event.current.Use();
				}
				break;
			case EventType.KeyDown:
				if (GUIUtility.hotControl == id && Event.current.keyCode == KeyCode.Escape)
				{
					position = GUIToWorld(s_DragStartScreenPosition - s_DragScreenOffset);
					GUIUtility.hotControl = 0;
					GUI.changed = true;
					Event.current.Use();
				}
				break;
			}

			if(drawCapFunction != null)
			{
				drawCapFunction(id,position, Quaternion.identity, 1f, eventType);
			}

			return position;
		}

		public static void RectangleHandleCap (int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
		{
#if UNITY_5_6_OR_NEWER
			Handles.RectangleHandleCap(controlID, position, rotation, size, eventType);
#else
			if(eventType != EventType.Repaint) return;
			Handles.RectangleCap(controlID, position, rotation, size);
#endif
		}

		public static void PivotCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
		{
			DrawImageBasedCap(controlID, position, rotation, size, styles.pivotDot, styles.pivotDotActive);
		}

		public static void DotCap(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
		{
			DrawImageBasedCap(controlID, position, rotation, size, styles.dragDot, styles.dragDotActive);
		}

		public static void DrawDotCyan(Vector3 position)
		{
			if(Event.current.type != EventType.Repaint) return;

			Handles.BeginGUI();
			styles.dotCyan.Draw(GetGUIStyleRect(styles.dotCyan,position), GUIContent.none, 0);
			Handles.EndGUI();
		}

		public static void DrawDotYellow(Vector3 position)
		{
			if(Event.current.type != EventType.Repaint) return;

			Handles.BeginGUI();
			styles.dotYellow.Draw(GetGUIStyleRect(styles.dotCyan,position), GUIContent.none, 0);
			Handles.EndGUI();
		}

		public static void DrawDotRed(Vector3 position)
		{
			if(Event.current.type != EventType.Repaint) return;

			Handles.BeginGUI();
			styles.dotRed.Draw(GetGUIStyleRect(styles.dotRed,position), GUIContent.none, 0);
			Handles.EndGUI();
		}

		public static void DrawDotBlackBig(Vector3 position)
		{
			if(Event.current.type != EventType.Repaint) return;
			
			Handles.BeginGUI();
			styles.dotBlackBig.Draw(GetGUIStyleRect(styles.dotBlackBig,position), GUIContent.none, 0);
			Handles.EndGUI();
		}

		public static void DrawDotYellowBig(Vector3 position)
		{
			if(Event.current.type != EventType.Repaint) return;
			
			Handles.BeginGUI();
			styles.dotYellowBig.Draw(GetGUIStyleRect(styles.dotYellowBig,position), GUIContent.none, 0);
			Handles.EndGUI();
		}

		public static void VertexCap(int controlID, Vector3 position, Quaternion rotation, float size)
		{
			DrawImageBasedCap(controlID, position, rotation, size, styles.dotCyan, styles.dotYellow);
		}

		static Rect GetGUIStyleRect(GUIStyle style, Vector3 position)
		{
			Vector3 vector = HandleUtility.WorldToGUIPoint(position);

			float fixedWidth = style.fixedWidth;
			float fixedHeight = style.fixedHeight;

			return new Rect(vector.x - fixedWidth / 2f, vector.y - fixedHeight / 2f, fixedWidth, fixedHeight);
		}

		static void DrawImageBasedCap(int controlID, Vector3 position, Quaternion rotation, float size, GUIStyle normal, GUIStyle active)
		{
			if(Event.current.type != EventType.Repaint)
				return;

			if (Camera.current && Vector3.Dot(position - Camera.current.transform.position, Camera.current.transform.forward) < 0f)
				return;
			
			Handles.BeginGUI();
			if(GUIUtility.hotControl == controlID)
			{
				active.Draw(GetGUIStyleRect(normal,position), GUIContent.none, controlID);
			}
			else
			{
				normal.Draw(GetGUIStyleRect(active,position), GUIContent.none, controlID);
			}
			Handles.EndGUI();
		}

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

		public static void ApplyWireMaterial()
		{
			if (s_ApplyWireMaterialMethodInfo == null)
			{
				s_ApplyWireMaterialMethodInfo = typeof(HandleUtility).GetMethod ("ApplyWireMaterial", BindingFlags.Static | BindingFlags.NonPublic, null, new Type[] { }, null);
			}

			if (s_ApplyWireMaterialMethodInfo != null)
			{
				s_ApplyWireMaterialMethodInfo.Invoke (null, null);
			}
		}

		static void SetDiscSectionPoints(Vector3[] dest, int count, Vector3 normal, Vector3 from, float angle)
		{
			from.Normalize();
			Quaternion rotation = Quaternion.AngleAxis(angle / (float)(count - 1), normal);
			Vector3 vector = from;
			for (int i = 0; i < count; i++)
			{
				dest[i] = vector;
				vector = rotation * vector;
			}
		}

		public static void DrawCircle(Vector3 center, float radius)
		{
			DrawCircle(center,radius,0f);
		}

		public static void DrawCircle(Vector3 center, float radius, float innerRadius)
		{
			if (Event.current.type != EventType.Repaint || Handles.color.a == 0f)
			{
				return;
			}
			
			innerRadius = Mathf.Clamp01(innerRadius);
			
			if(s_circleArray == null)
			{
				s_circleArray = new Vector3[12];
				SetDiscSectionPoints(s_circleArray, 12, Vector3.forward, Vector3.right, 360f);
			}

			Shader.SetGlobalColor("_HandleColor", Handles.color * new Color(1f, 1f, 1f, 0.5f));
			Shader.SetGlobalFloat("_HandleSize", 1f);
			handleWireMaterial.SetPass(0);
			GL.PushMatrix();
			GL.MultMatrix(Handles.matrix);
			GL.Begin(4);
			for (int i = 1; i < s_circleArray.Length; i++)
			{
				GL.Color(Handles.color);
				GL.Vertex(center + s_circleArray[i - 1]* radius * innerRadius);
				GL.Vertex(center + s_circleArray[i - 1]*radius);
				GL.Vertex(center + s_circleArray[i]*radius);
				GL.Vertex(center + s_circleArray[i - 1]* radius * innerRadius);
				GL.Vertex(center + s_circleArray[i]*radius);
				GL.Vertex(center + s_circleArray[i]* radius * innerRadius);
			}
			GL.End();
			GL.PopMatrix();
		}

		public static void DrawTriangle(Vector3 center, Vector3 normal, float radius, float innerRadius)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			
			innerRadius = Mathf.Clamp01(innerRadius);
			
			Vector3[] array = new Vector3[4];
			SetDiscSectionPoints(array, 4, normal, Vector3.up, 360f);
			Shader.SetGlobalColor("_HandleColor", Handles.color * new Color(1f, 1f, 1f, 0.5f));
			Shader.SetGlobalFloat("_HandleSize", 1f);
			handleWireMaterial.SetPass(0);
			GL.PushMatrix();
			GL.MultMatrix(Handles.matrix);
			GL.Begin(4);
			for (int i = 1; i < array.Length; i++)
			{
				GL.Color(Handles.color);
				GL.Vertex(center + array[i - 1]*innerRadius);
				GL.Vertex(center + array[i - 1]*radius);
				GL.Vertex(center + array[i]*radius);
				GL.Vertex(center + array[i - 1]*innerRadius);
				GL.Vertex(center + array[i]*radius);
				GL.Vertex(center + array[i]*innerRadius);
			}
			GL.End();
			GL.PopMatrix();
		}

		public static void DrawSquare(Vector3 center, Vector3 normal, float radius, float innerRadius)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			
			innerRadius = Mathf.Clamp01(innerRadius);
			
			Vector3[] array = new Vector3[5];
			SetDiscSectionPoints(array, 5, normal, Vector3.left + Vector3.up, 360f);
			Shader.SetGlobalColor("_HandleColor", Handles.color * new Color(1f, 1f, 1f, 0.5f));
			Shader.SetGlobalFloat("_HandleSize", 1f);
			handleWireMaterial.SetPass(0);
			GL.PushMatrix();
			GL.MultMatrix(Handles.matrix);
			GL.Begin(4);
			for (int i = 1; i < array.Length; i++)
			{
				GL.Color(Handles.color);
				GL.Vertex(center + array[i - 1]*innerRadius);
				GL.Vertex(center + array[i - 1]*radius);
				GL.Vertex(center + array[i]*radius);
			}
			GL.End();
			GL.PopMatrix();
		}

		public static void DrawLine (Vector3 p1, Vector3 p2, Vector3 normal, float width)
		{
			DrawLine(p1,p2,normal,width,width);
		}

		public static void DrawLine (Vector3 p1, Vector3 p2, Vector3 normal, float widthP1, float widthP2)
		{
			DrawLine(p1,p2,normal,widthP1,widthP2,Handles.color);
		}

		public static void DrawLine (Vector3 p1, Vector3 p2, Vector3 normal, float widthP1, float widthP2, Color color)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			Vector3 right = Vector3.Cross(normal,p2-p1).normalized;
			handleWireMaterial.SetPass(0);
			GL.PushMatrix ();
			GL.MultMatrix (Handles.matrix);
			GL.Begin (4);
			GL.Color (color);
			GL.Vertex (p1 + right * widthP1 * 0.5f);
			GL.Vertex (p1 - right * widthP1 * 0.5f);
			GL.Vertex (p2 - right * widthP2 * 0.5f);
			GL.Vertex (p1 + right * widthP1 * 0.5f);
			GL.Vertex (p2 - right * widthP2 * 0.5f);
			GL.Vertex (p2 + right * widthP2 * 0.5f);
			GL.End ();
			GL.PopMatrix ();
		}

		static Vector3[] s_array;
		public static void DrawSolidArc(Vector3 center, Vector3 normal, Vector3 from, float angle, float radius, Color color)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			if(s_array == null)
			{
				s_array = new Vector3[60];
			}

			SetDiscSectionPoints(s_array, 60, normal, from, angle);
			handleWireMaterial.SetPass(0);
			GL.PushMatrix();
			GL.MultMatrix(Handles.matrix);
			GL.Begin(4);
			for (int i = 1; i < s_array.Length; i++)
			{
				GL.Color(color);
				GL.Vertex(center);
				GL.Vertex(center + s_array[i - 1]*radius);
				GL.Vertex(center + s_array[i]*radius);
			}
			GL.End();
			GL.PopMatrix();
		}

		public static void DrawAAPolyLine(Color[] colors, Vector3[] points)
		{
			Type type = typeof(Handles);

			MethodInfo method = type.GetMethod("DrawAAPolyLine", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, new [] { typeof(Color[]), typeof(Vector3[]) } , null);

			if(method != null)
			{
				object[] parameters = new object[] { colors, points };
				method.Invoke(null,parameters);
			}
		}
	}
}
