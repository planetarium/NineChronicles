using UnityEngine;
using UnityEditor;
using UnityEditor.Sprites;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Anima2D 
{
	[CanEditMultipleObjects][CustomEditor(typeof(SpriteMesh))]
	public class SpriteMeshEditor : Editor
	{
		SerializedProperty m_SpriteProperty;
		SerializedProperty m_SharedMeshProperty;

		void OnEnable()
		{
			m_SpriteProperty = serializedObject.FindProperty("m_Sprite");
			m_SharedMeshProperty = serializedObject.FindProperty("m_SharedMesh");
		}

		override public void OnInspectorGUI()
		{
			Sprite oldSprite = m_SpriteProperty.objectReferenceValue as Sprite;

			EditorGUI.BeginChangeCheck();

			serializedObject.Update();

			EditorGUILayout.PropertyField(m_SpriteProperty);

			serializedObject.ApplyModifiedProperties();

			if(EditorGUI.EndChangeCheck())
			{
				Sprite sprite = m_SpriteProperty.objectReferenceValue as Sprite;

				SpriteMeshUtils.UpdateAssets(target as SpriteMesh);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				AssetDatabase.StartAssetEditing();

				if(oldSprite)
				{
					AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(oldSprite));
				}

				AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(sprite));
				AssetDatabase.StopAssetEditing();
			}

			serializedObject.Update();

			EditorGUI.BeginDisabledGroup(true);

			EditorGUILayout.PropertyField(m_SharedMeshProperty);

			EditorGUI.EndDisabledGroup();

			EditorGUILayout.BeginHorizontal();
			
			GUILayout.FlexibleSpace();
			
			if (GUILayout.Button("Edit Sprite Mesh",GUILayout.Width(150f)))
			{
				SpriteMeshEditorWindow window = SpriteMeshEditorWindow.GetWindow();
				window.UpdateFromSelection();
			}
			
			GUILayout.FlexibleSpace();
			
			EditorGUILayout.EndHorizontal();

			serializedObject.ApplyModifiedProperties();
		}
		
		public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
		{
			if(target == null)
			{
				return null;
			}
			if(!(target as SpriteMesh).sprite)
			{
				return null;
			}
			return BuildPreviewTexture(width, height, (target as SpriteMesh).sprite, null);
		}
		
		Texture2D BuildPreviewTexture(int width, int height, Sprite sprite, Material spriteRendererMaterial)
		{
			Texture2D result = null;
			
			MethodInfo methodInfo = typeof(Editor).Assembly.GetType("UnityEditor.SpriteInspector").GetMethod("BuildPreviewTexture", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			
			if(methodInfo != null)
			{
#if UNITY_5_0_0 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
				object[] parameters = new object[] { width, height, sprite, spriteRendererMaterial };
#else
				object[] parameters = new object[] { width, height, sprite, spriteRendererMaterial, false };
#endif
				result = (Texture2D)methodInfo.Invoke(null,parameters);
			}
			
			return result;
		}
		
		public override bool HasPreviewGUI()
		{
			return target != null && (target as SpriteMesh).sprite;
		}
		public override void OnPreviewGUI(Rect r, GUIStyle background)
		{
			if(target == null)
			{
				return;
			}
			if(!(target as SpriteMesh).sprite)
			{
				return;
			}
			if (Event.current.type != EventType.Repaint)
			{
				return;
			}
			DrawPreview(r, (target as SpriteMesh).sprite, null);
		}
		public static void DrawPreview(Rect r, Sprite frame, Material spriteRendererMaterial)
		{
			if (frame == null)
			{
				return;
			}
			MethodInfo methodInfo = typeof(Editor).Assembly.GetType("UnityEditor.SpriteInspector").GetMethod("DrawPreview", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			
			if(methodInfo != null)
			{
#if UNITY_5_0_0 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
				object[] parameters = new object[] { r, frame, spriteRendererMaterial };
#else
				object[] parameters = new object[] { r, frame, spriteRendererMaterial, false };
#endif
				methodInfo.Invoke(null,parameters);
			}
			
		}
	}
}
