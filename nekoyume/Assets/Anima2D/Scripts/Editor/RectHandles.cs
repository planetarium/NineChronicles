using UnityEngine;
using UnityEditor;
using System.Collections;

namespace Anima2D
{
	public class RectHandles
	{
		private static Rect s_StartRect;
		private static Rect s_CurrentRect;
		private static Vector3 s_StartPosition;
		private static Vector2 s_StartPivot;
		private static Vector3[] s_TempVectors = new Vector3[0];

		public static void Do(ref Rect handleRect,ref Vector3 handlePosition, ref Quaternion handleRectRotation, bool usePivot = true, bool anchorPivot = false)
		{
			GUI.color = Color.white;

			RenderRect(handleRect,handlePosition,handleRectRotation,new Color(0.25f, 0.5f, 1f, 1f), 0.05f, 0.8f);

			handleRect = MoveHandlesGUI(handleRect, ref handlePosition, handleRectRotation, anchorPivot || !usePivot);

			if(usePivot)
			{
				handleRect = PivotHandleGUI(handleRect, ref handlePosition, handleRectRotation);
			}

			handleRect = ResizeHandlesGUI(handleRect,ref handlePosition, handleRectRotation, anchorPivot || !usePivot);

			if(usePivot)
			{
				handleRectRotation = RotationHandlesGUI(handleRect, handlePosition, handleRectRotation);
			}
		}

		static Vector2 GetLocalRectPoint(Rect rect, int index)
		{
			switch (index)
			{
			case 0:
				return new Vector2(rect.xMin, rect.yMax);
			case 1:
				return new Vector2(rect.xMax, rect.yMax);
			case 2:
				return new Vector2(rect.xMax, rect.yMin);
			case 3:
				return new Vector2(rect.xMin, rect.yMin);
			default:
				return Vector3.zero;
			}
		}

		static Vector3 GetRectPointInWorld(Rect rect, Vector3 pivot, Quaternion rotation, int xHandle, int yHandle)
		{
			Vector3 point = new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, (float)xHandle * 0.5f), Mathf.Lerp(rect.yMin, rect.yMax, (float)yHandle * 0.5f));

			return rotation * point + pivot;
		}
	
		static Vector2 GetNormalizedPivot(Rect rect)
		{
			return new Vector2(-rect.position.x/rect.width, -rect.position.y/rect.height);
		}

		static Rect ResizeHandlesGUI(Rect rect,ref Vector3 position, Quaternion rotation, bool anchorPivot = false)
		{
			if (Event.current.type == EventType.MouseDown)
			{
				s_StartRect = rect;
				s_CurrentRect = rect;
				s_StartPosition = position;
				s_StartPivot = GetNormalizedPivot(rect);
			}

			Vector3 scale = Vector3.one;
			Quaternion inverseRotation = Quaternion.Inverse(rotation);

			for (int i = 0; i <= 2; i++)
			{
				for (int j = 0; j <= 2; j++)
				{
					if (i != 1 || j != 1)
					{
						Vector3 startWorldPoint = GetRectPointInWorld(s_StartRect, s_StartPosition, rotation, i, j);
						Vector3 currentWorldPoint = GetRectPointInWorld(s_CurrentRect, s_StartPosition, rotation, i, j);
						Vector3 rectWorldPoint = GetRectPointInWorld(rect, position, rotation, i, j);

						int controlID = GUIUtility.GetControlID("RectResizeHandles".GetHashCode(), FocusType.Passive);

						EventType eventType = Event.current.GetTypeForControl(controlID);

						if (GUI.color.a > 0f || GUIUtility.hotControl == controlID)
						{
							EditorGUI.BeginChangeCheck();
							Vector3 newPosition = Vector3.zero;

							MouseCursor cursor = MouseCursor.Arrow;
						
							if (i == 1 || j == 1)
							{
								Vector3 sideVector = (i != 1) ? (rotation * Vector3.up * rect.height) : (rotation * Vector3.right * rect.width);
								Vector3 direction = (i != 1) ? (rotation * Vector3.right) : (rotation * Vector3.up);
								newPosition = SideSlider(controlID, currentWorldPoint, sideVector, direction, null);

								if(!Event.current.alt && eventType == EventType.Layout)
								{
									Vector3 normalized2 = sideVector.normalized;

									Vector3 p1 = rectWorldPoint + sideVector * 0.5f;
									Vector3 p2 = rectWorldPoint - sideVector * 0.5f;
									Vector3 offset = - normalized2 * HandleUtility.GetHandleSize(p1) / 20f;
									Vector3 offset2 = normalized2 * HandleUtility.GetHandleSize(p2) / 20f;

									HandleUtility.AddControl(controlID, HandleUtility.DistanceToLine(p1 + offset, p2 + offset2));
								}

								cursor = GetScaleCursor(direction);
							}
							else
							{
								HandlesExtra.DotCap(controlID,rectWorldPoint, Quaternion.identity, 1f, eventType);
									
								newPosition = HandlesExtra.Slider2D(controlID, currentWorldPoint,null);

								if(!Event.current.alt && eventType == EventType.Layout)
								{
									HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(rectWorldPoint, HandleUtility.GetHandleSize(rectWorldPoint) / 20f));
								}

								Vector3 outwardsDir = rotation * Vector3.right * (float)(i - 1);
								Vector3 outwardsDir2 = rotation * Vector3.up * (float)(j - 1);

								cursor = GetScaleCursor(outwardsDir + outwardsDir2);
							}
							
							if(eventType == EventType.Repaint)
							{
								if ((HandleUtility.nearestControl == controlID && GUIUtility.hotControl == 0) || GUIUtility.hotControl == controlID)
								{
									Rect cursorRect = new Rect(0,0, 20f, 20f);

									cursorRect.center = Event.current.mousePosition;
									
									EditorGUIUtility.AddCursorRect(cursorRect, cursor, controlID);
								}
							}

							if (EditorGUI.EndChangeCheck())
							{
								Vector3 scalePivot = Vector3.zero;
								Vector2 scalePivotLocal = Vector2.one;

								bool alt = Event.current.alt;
								bool actionKey = EditorGUI.actionKey;
								bool shiftDown = Event.current.shift && !actionKey;
								if (!alt)
								{
									scalePivot = GetRectPointInWorld(s_StartRect, s_StartPosition, rotation, 2 - i, 2 - j);
									scalePivotLocal = inverseRotation * (scalePivot - s_StartPosition);
								}

								Vector3 localRectPoint = inverseRotation * (startWorldPoint - scalePivot);
								Vector3 localNewPosition = inverseRotation * (newPosition - scalePivot);

								if (i != 1)
								{
									scale.x = localNewPosition.x / localRectPoint.x;
								}
								if (j != 1)
								{
									scale.y = localNewPosition.y / localRectPoint.y;
								}
								if (shiftDown)
								{
									float d = (i != 1) ? scale.x : scale.y;
									scale = Vector3.one * d;
								}
								if (actionKey && i == 1)
								{
									if (Event.current.shift)
									{
										scale.x = (scale.z = 1f / Mathf.Sqrt(Mathf.Max(scale.y, 0.0001f)));
									}
									else
									{
										scale.x = 1f / Mathf.Max(scale.y, 0.0001f);
									}
								}
								if (shiftDown)
								{
									float d2 = (i != 1) ? scale.x : scale.y;
									scale = Vector3.one * d2;
								}
								if (actionKey && i == 1)
								{
									if (Event.current.shift)
									{
										scale.x = (scale.z = 1f / Mathf.Sqrt(Mathf.Max(scale.y, 0.0001f)));
									}
									else
									{
										scale.x = 1f / Mathf.Max(scale.y, 0.0001f);
									}
								}
								if (actionKey && j == 1)
								{
									if (Event.current.shift)
									{
										scale.y = (scale.z = 1f / Mathf.Sqrt(Mathf.Max(scale.x, 0.0001f)));
									}
									else
									{
										scale.y = 1f / Mathf.Max(scale.x, 0.0001f);
									}
								}

								s_CurrentRect.min = Vector2.Scale(scale,s_StartRect.min - scalePivotLocal) + scalePivotLocal;
								s_CurrentRect.max = Vector2.Scale(scale,s_StartRect.max - scalePivotLocal) + scalePivotLocal;

								if(anchorPivot)
								{
									rect.min = s_CurrentRect.min;
									rect.max = s_CurrentRect.max;
								}else{
									rect.position = Vector2.Scale(scale,s_StartRect.position);
									rect.size = Vector2.Scale(scale,s_StartRect.size);
									
									Vector2 newPivot = new Vector2(s_CurrentRect.xMin + (s_CurrentRect.xMax - s_CurrentRect.xMin) * s_StartPivot.x,
									                               s_CurrentRect.yMin + (s_CurrentRect.yMax - s_CurrentRect.yMin) * s_StartPivot.y);
									
									position =  s_StartPosition + rotation * newPivot;
								}
							}
						}
					}
				}
			}
			return rect;
		}

		static Rect MoveHandlesGUI(Rect rect, ref Vector3 position, Quaternion rotation, bool anchorPivot = false)
		{
			int controlID = GUIUtility.GetControlID("RectMoveHandles".GetHashCode(), FocusType.Passive);
			EventType eventType = Event.current.GetTypeForControl(controlID);

			if(eventType == EventType.MouseDown)
			{
				s_StartRect = rect;
			}

			EditorGUI.BeginChangeCheck();

			Vector3 newPosition = HandlesExtra.Slider2D(controlID, position, null);

			if(EditorGUI.EndChangeCheck())
			{
				if(anchorPivot)
				{
					Vector2 delta = Quaternion.Inverse(rotation) * (newPosition - position);

					rect.min = s_StartRect.min + delta;
					rect.max = s_StartRect.max + delta;
				}else{
					position = newPosition;
				}
			}

			if(eventType == EventType.Layout)
			{
				Vector2 mousePositionRectSpace = Vector2.zero;

				mousePositionRectSpace = Quaternion.Inverse(rotation) * (HandlesExtra.GUIToWorld(Event.current.mousePosition) - position);

				if(rect.Contains(mousePositionRectSpace,true))
				{
					HandleUtility.AddControl(controlID, 0f);
				}
			}

			if(eventType == EventType.Repaint)
			{
				if((!Event.current.alt &&
				    HandleUtility.nearestControl == controlID &&
				    GUIUtility.hotControl == 0) ||
				   GUIUtility.hotControl == controlID)
				{
					Rect cursorRect = new Rect(0f,0f,20f,20f);
					cursorRect.center = Event.current.mousePosition;
					EditorGUIUtility.AddCursorRect(cursorRect, MouseCursor.MoveArrow, controlID);
				}
			}

			return rect;
		}

		static Quaternion RotationHandlesGUI(Rect rect, Vector3 pivot, Quaternion rotation)
		{
			Vector3 eulerAngles = rotation.eulerAngles;
			for (int i = 0; i <= 2; i += 2)
			{
				for (int j = 0; j <= 2; j += 2)
				{
					Vector3 rectPointInWorld = GetRectPointInWorld(rect, pivot, rotation, i, j);
					float handleSize = 0.05f * Handles.matrix.m00;
					int controlID = GUIUtility.GetControlID("RectRotationHandles".GetHashCode(), FocusType.Passive);

					EditorGUI.BeginChangeCheck();
					Vector3 outwardsDir = rotation * Vector3.right * (float)(i - 1) * Mathf.Sign(rect.width);
					Vector3 outwardsDir2 = rotation * Vector3.up * (float)(j - 1) * Mathf.Sign(rect.height);
					float num = RotationSlider(controlID, rectPointInWorld, eulerAngles.z, pivot, rotation * Vector3.forward, outwardsDir, outwardsDir2, handleSize, null, Vector2.zero);
					if (EditorGUI.EndChangeCheck())
					{
						if (Event.current.shift)
						{
							num = Mathf.Round((num - eulerAngles.z) / 15f) * 15f + eulerAngles.z;
						}
						eulerAngles.z = num;
						rotation = Quaternion.Euler(eulerAngles);
					}
				}
			}
			return rotation;
		}

		public static void RenderRect(Rect rect, Vector3 position, Quaternion rotation, Color color, float rectAlpha, float outlineAlpha)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			Vector3[] corners = new Vector3[4];
			for (int i = 0; i < 4; i++)
			{
				Vector3 point = GetLocalRectPoint(rect, i);
				corners[i] = rotation * point + position;
			}

			Vector3[] points = new Vector3[]
			{
				corners[0],
				corners[1],
				corners[2],
				corners[3],
				corners[0]
			};

			Color l_color = Handles.color;
			Handles.color = color;

			Vector2 offset = new Vector2(1f, 1f);

			if(!Camera.current)
			{
				offset.y *= -1;
			}

			DrawPolyLineWithOffset(color * 0.5f, new Vector2(1f, 1f), points);
			Handles.DrawSolidRectangleWithOutline(points,new Color(1f,1f,1f,rectAlpha),new Color(1f,1f,1f,outlineAlpha));

			Handles.color = l_color;
		}

		static void DrawPolyLineWithOffset(Color lineColor, Vector2 screenOffset, params Vector3[] points)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}

			if (s_TempVectors.Length != points.Length)
			{
				s_TempVectors = new Vector3[points.Length];
			}

			for (int i = 0; i < points.Length; i++)
			{
				s_TempVectors[i] = (Vector3)HandlesExtra.GUIToWorld(HandleUtility.WorldToGUIPoint(points[i]) + screenOffset);
			}
			Color color = Handles.color;
			Handles.color = lineColor;
			DrawPolyLine(s_TempVectors);
			Handles.color = color;
		}

		static void DrawPolyLine(params Vector3[] points)
		{
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			Color c = Handles.color;

			HandlesExtra.ApplyWireMaterial();

			GL.PushMatrix ();
			GL.MultMatrix (Handles.matrix);
			GL.Begin (1);
			GL.Color (c);
			for (int i = 1; i < points.Length; i++)
			{
				GL.Vertex (points [i]);
				GL.Vertex (points [i - 1]);
			}
			GL.End ();
			GL.PopMatrix ();
		}

		static Rect PivotHandleGUI(Rect rect, ref Vector3 position, Quaternion rotation)
		{
			int controlID = GUIUtility.GetControlID("RectPivotHandle".GetHashCode(), FocusType.Passive);
			EventType eventType = Event.current.GetTypeForControl(controlID);
			
			EditorGUI.BeginChangeCheck();

			Vector3 newPosition = HandlesExtra.Slider2D(controlID, position, HandlesExtra.PivotCap);

			if (EditorGUI.EndChangeCheck())
			{
				Vector3 pivotDelta = (newPosition - position);
				Vector2 pivotDeltaLocal = Quaternion.Inverse(rotation) * pivotDelta;
				position = newPosition;
				rect.position -= pivotDeltaLocal;
			}

			if(eventType == EventType.Layout)
			{
				float radius = HandleUtility.GetHandleSize(position) / 2f;

				if(Camera.current)
				{
					radius = HandleUtility.GetHandleSize(position) / 10f;
				}

				HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position,radius));
			}

			if(eventType == EventType.Repaint)
			{
				if ((HandleUtility.nearestControl == controlID && GUIUtility.hotControl == 0) || GUIUtility.hotControl == controlID)
				{
					Rect cursorRect = new Rect(0,0, 20f, 20f);
					cursorRect.center = Event.current.mousePosition;

					EditorGUIUtility.AddCursorRect(cursorRect, MouseCursor.Arrow, controlID);
				}
			}

			return rect;
		}

		static float AngleAroundAxis(Vector3 dirA, Vector3 dirB, Vector3 axis)
		{
			dirA = Vector3.ProjectOnPlane(dirA, axis);
			dirB = Vector3.ProjectOnPlane(dirB, axis);
			float num = Vector3.Angle(dirA, dirB);
			return num * (float)((Vector3.Dot(axis, Vector3.Cross(dirA, dirB)) >= 0f) ? 1 : -1);
		}

		static float RotationSlider(int controlID, Vector3 cornerPos, float rotation, Vector3 pivot, Vector3 handleDir, Vector3 outwardsDir1, Vector3 outwardsDir2, float handleSize, HandlesExtra.CapFunction drawFunc, Vector2 snap)
		{
			EventType eventType = Event.current.GetTypeForControl(controlID);

			Vector3 b = outwardsDir1 + outwardsDir2;
			Vector3 guiCornerPos = HandleUtility.WorldToGUIPoint(cornerPos);
			Vector3 b2 = ((Vector3)HandleUtility.WorldToGUIPoint(cornerPos + b) - guiCornerPos).normalized * 15f;

			cornerPos = HandlesExtra.GUIToWorld(guiCornerPos + b2);

			Vector3 newPosition = HandlesExtra.Slider2D(controlID, cornerPos, drawFunc);

			rotation = rotation - AngleAroundAxis(newPosition - pivot, cornerPos - pivot, handleDir);

			if(eventType == EventType.Layout)
			{
				HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(cornerPos, HandleUtility.GetHandleSize(cornerPos + b) / 10f));
			}
			
			if(eventType == EventType.Repaint)
			{
				if((HandleUtility.nearestControl == controlID && GUIUtility.hotControl == 0) || GUIUtility.hotControl == controlID)
				{
					Rect cursorRect = new Rect(0, 0, 20f, 20f);
					cursorRect.center = Event.current.mousePosition;
					EditorGUIUtility.AddCursorRect(cursorRect, MouseCursor.RotateArrow, controlID);
				}
			}

			return rotation;
		}

		static Vector3 SideSlider(int controlID, Vector3 position, Vector3 sideVector, Vector3 direction, HandlesExtra.CapFunction drawFunc)
		{
			Vector3 vector = HandlesExtra.Slider2D(controlID, position,drawFunc);

			vector = position + Vector3.Project(vector - position, direction);

			return vector;
		}

		static MouseCursor GetScaleCursor(Vector2 direction)
		{
			float num = Mathf.Atan2(direction.x, direction.y) * 57.29578f;
			if (num < 0f)
			{
				num = 360f + num;
			}
			if (num < 27.5f)
			{
				return MouseCursor.ResizeVertical;
			}
			if (num < 72.5f)
			{
				return MouseCursor.ResizeUpRight;
			}
			if (num < 117.5f)
			{
				return MouseCursor.ResizeHorizontal;
			}
			if (num < 162.5f)
			{
				return MouseCursor.ResizeUpLeft;
			}
			if (num < 207.5f)
			{
				return MouseCursor.ResizeVertical;
			}
			if (num < 252.5f)
			{
				return MouseCursor.ResizeUpRight;
			}
			if (num < 297.5f)
			{
				return MouseCursor.ResizeHorizontal;
			}
			if (num < 342.5f)
			{
				return MouseCursor.ResizeUpLeft;
			}
			return MouseCursor.ResizeVertical;
		}
	}
}
