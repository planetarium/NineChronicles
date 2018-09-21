using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Anima2D 
{
	[InitializeOnLoad]
	public class Gizmos
	{
		static List<Bone2D> s_Bones = new List<Bone2D>();
		static List<Control> s_Controls = new List<Control>();

		static Gizmos()
		{
			EditorApplication.hierarchyWindowChanged += HierarchyWindowChanged;
		}

		static bool IsVisible(Bone2D bone)
		{
			return IsVisible(bone.gameObject);
		}

		static bool IsVisible(GameObject go)
		{
			return (Tools.visibleLayers & 1 << go.layer) != 0;
		}

		static bool IsLocked(Bone2D bone)
		{
			return IsLocked(bone.gameObject);
		}

		static bool IsLocked(GameObject go)
		{
			return (Tools.lockedLayers & 1 << go.layer) != 0;
		}

		[UnityEditor.Callbacks.DidReloadScripts]
		static void HierarchyWindowChanged()
		{
			s_Bones = GameObject.FindObjectsOfType<Bone2D>().ToList();
			s_Controls = GameObject.FindObjectsOfType<Control>().ToList();

			SceneView.RepaintAll();
		}

		static float AngleAroundAxis(Vector3 dirA, Vector3 dirB, Vector3 axis)
		{
			dirA = Vector3.ProjectOnPlane(dirA, axis);
			dirB = Vector3.ProjectOnPlane(dirB, axis);
			float num = Vector3.Angle(dirA, dirB);
			return num * (float)((Vector3.Dot(axis, Vector3.Cross(dirA, dirB)) >= 0f) ? 1 : -1);
		}

		public static void OnSceneGUI(SceneView sceneview)
		{
			for (int i = 0; i < s_Bones.Count; i++)
			{
				Bone2D bone = s_Bones[i];
				
				if(bone && IsVisible(bone))
				{
					int controlID = GUIUtility.GetControlID("BoneHandle".GetHashCode(),FocusType.Passive);
					EventType eventType = Event.current.GetTypeForControl(controlID);

					if(!IsLocked(bone))
					{
						if(eventType == EventType.MouseDown)
						{
							if(HandleUtility.nearestControl == controlID &&
							   Event.current.button == 0)
							{
								GUIUtility.hotControl = controlID;
								Event.current.Use();
							}
						}

						if(eventType == EventType.MouseUp)
						{
							if(GUIUtility.hotControl == controlID &&
							   Event.current.button == 0)
							{
								if(EditorGUI.actionKey)
								{
									List<Object> objects = new List<Object>(Selection.objects);
									objects.Add(bone.gameObject);
									Selection.objects = objects.ToArray();
								}else{
									Selection.activeObject = bone;
								}

								GUIUtility.hotControl = 0;
								Event.current.Use();
							}
						}

						if(eventType == EventType.MouseDrag)
						{
							if(GUIUtility.hotControl == controlID &&
							   Event.current.button == 0)
							{
								Handles.matrix = bone.transform.localToWorldMatrix;
								Vector3 position = HandlesExtra.GUIToWorld(Event.current.mousePosition);

								BoneUtils.OrientToLocalPosition(bone,position,Event.current.shift,"Rotate",true);

								EditorUpdater.SetDirty("Rotate");
								
								GUI.changed = true;
								Event.current.Use();
							}
						}
					}

					if(eventType == EventType.Repaint)
					{
						if((HandleUtility.nearestControl == controlID && GUIUtility.hotControl == 0) ||
						   GUIUtility.hotControl == controlID ||
						   Selection.gameObjects.Contains(bone.gameObject))
						{
							Color color = Color.yellow;

							float outlineSize = HandleUtility.GetHandleSize(bone.transform.position) * 0.015f * bone.color.a;
							BoneUtils.DrawBoneOutline(bone,outlineSize,color);

							Bone2D outlineBone = bone.child;
							color.a *= 0.5f;

							while(outlineBone)
							{
								if(Selection.gameObjects.Contains(outlineBone.gameObject))
								{
									outlineBone = null;
								}else{
									if(outlineBone.color.a == 0f)
									{
										outlineSize = HandleUtility.GetHandleSize(outlineBone.transform.position) * 0.015f * outlineBone.color.a;
										BoneUtils.DrawBoneOutline(outlineBone,outlineSize,color);
										outlineBone = outlineBone.child;
										color.a *= 0.5f;
									}else{
										outlineBone = null;
									}
								}
							}
						}

						if(bone.parentBone && !bone.linkedParentBone)
						{
							Color color = bone.color;
							color.a *= 0.25f;
							Handles.matrix = Matrix4x4.identity;
							BoneUtils.DrawBoneBody(bone.transform.position,bone.parentBone.transform.position,BoneUtils.GetBoneRadius(bone),color);
						}

						BoneUtils.DrawBoneBody(bone);

						Color innerColor = bone.color * 0.25f;
						
						if(bone.attachedIK && bone.attachedIK.isActiveAndEnabled)
						{
							innerColor = new Color (0f, 0.75f, 0.75f, 1f);
						}
						
						innerColor.a = bone.color.a;

						BoneUtils.DrawBoneCap(bone,innerColor);
					}

					if(!IsLocked(bone) && eventType == EventType.Layout)
					{
						HandleUtility.AddControl(controlID, HandleUtility.DistanceToLine(bone.transform.position,bone.endPosition));
					}
				}
			}

			foreach(Control control in s_Controls)
			{
				if(control && control.isActiveAndEnabled && IsVisible(control.gameObject))
				{
					Transform transform = control.transform;

					if(Selection.activeTransform != transform)
					{
						if(!control.bone ||
						   (control.bone && !Selection.transforms.Contains(control.bone.transform)))
						{
							Handles.matrix = Matrix4x4.identity;
							Handles.color = control.color;

							if(Tools.current == Tool.Move)
							{
								EditorGUI.BeginChangeCheck();

								Quaternion cameraRotation = Camera.current.transform.rotation;

								if(Event.current.type == EventType.Repaint)
								{
									Camera.current.transform.rotation = transform.rotation;
								}

								float size = HandleUtility.GetHandleSize(transform.position) / 5f;
								//Handles.DrawLine(transform.position + transform.rotation * new Vector3(size,0f,0f), transform.position + transform.rotation * new Vector3(-size,0f,0f));
								//Handles.DrawLine(transform.position + transform.rotation * new Vector3(0f,size,0f), transform.position + transform.rotation * new Vector3(0f,-size,0f));

								bool guiEnabled = GUI.enabled;
								GUI.enabled = !IsLocked(control.gameObject);

#if UNITY_5_6_OR_NEWER
								Vector3 newPosition = Handles.FreeMoveHandle(transform.position, transform.rotation, size, Vector3.zero, Handles.RectangleHandleCap);
#else
								Vector3 newPosition = Handles.FreeMoveHandle(transform.position, transform.rotation, size, Vector3.zero, Handles.RectangleCap);
#endif

								GUI.enabled = guiEnabled;

								if(Event.current.type == EventType.Repaint)
								{
									Camera.current.transform.rotation = cameraRotation;
								}

								if(EditorGUI.EndChangeCheck())
								{
									Undo.RecordObject(transform,"Move");
									transform.position = newPosition;

									if(control.bone)
									{
										Undo.RecordObject(control.bone.transform,"Move");

										control.bone.transform.position = newPosition;

										BoneUtils.OrientToChild(control.bone.parentBone,Event.current.shift,"Move",true);

										EditorUpdater.SetDirty("Move");
									}

								}

							}else if(Tools.current == Tool.Rotate)
							{
								EditorGUI.BeginChangeCheck();

								float size = HandleUtility.GetHandleSize(transform.position) * 0.5f;
								//Handles.DrawLine(transform.position + transform.rotation * new Vector3(size,0f,0f), transform.position + transform.rotation * new Vector3(-size,0f,0f));
								//Handles.DrawLine(transform.position + transform.rotation * new Vector3(0f,size,0f), transform.position + transform.rotation * new Vector3(0f,-size,0f));

								bool guiEnabled = GUI.enabled;
								GUI.enabled = !IsLocked(control.gameObject);

								Quaternion newRotation = Handles.Disc(transform.rotation, transform.position, transform.forward, size, false, 0f);

								GUI.enabled = guiEnabled;

								if(EditorGUI.EndChangeCheck())
								{
									Undo.RecordObject(transform,"Rotate");
									transform.rotation = newRotation;

									if(control.bone)
									{
										Undo.RecordObject(control.bone.transform,"Rotate");
										
										control.bone.transform.rotation = newRotation;

										EditorUpdater.SetDirty("Rotate");
									}
								}
							}
						}
					}
				}
			}
		}
	}
}
