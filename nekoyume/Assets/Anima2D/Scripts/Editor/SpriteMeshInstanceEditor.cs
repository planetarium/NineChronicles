using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	[DisallowMultipleComponent]
	[CanEditMultipleObjects]
	[CustomEditor(typeof(SpriteMeshInstance))]
	public class SpriteMeshInstanceEditor : Editor
	{
		SpriteMeshInstance m_SpriteMeshInstance;
		SpriteMeshData m_SpriteMeshData;

		ReorderableList mBoneList = null;

		SerializedProperty m_SortingOrder;
		SerializedProperty m_SortingLayerID;
		SerializedProperty m_SpriteMeshProperty;
		SerializedProperty m_ColorProperty;
		SerializedProperty m_MaterialsProperty;
		SerializedProperty m_BoneTransformsProperty;

		int m_UndoGroup = -1;

		void OnEnable()
		{
			m_SpriteMeshInstance = target as SpriteMeshInstance;
			m_SortingOrder = serializedObject.FindProperty("m_SortingOrder");
			m_SortingLayerID = serializedObject.FindProperty("m_SortingLayerID");
			m_SpriteMeshProperty = serializedObject.FindProperty("m_SpriteMesh");
			m_ColorProperty = serializedObject.FindProperty("m_Color");
			m_MaterialsProperty = serializedObject.FindProperty("m_Materials.Array");
			m_BoneTransformsProperty = serializedObject.FindProperty("m_BoneTransforms.Array");

			UpgradeToMaterials();

			UpdateSpriteMeshData();
			SetupBoneList();

#if UNITY_5_5_OR_NEWER
			
#else
			EditorUtility.SetSelectedWireframeHidden(m_SpriteMeshInstance.cachedRenderer, !m_SpriteMeshInstance.cachedSkinnedRenderer);
#endif
		}

		void UpgradeToMaterials()
		{
			if(Selection.transforms.Length == 1 && m_MaterialsProperty.arraySize == 0)
			{
				serializedObject.Update();
				m_MaterialsProperty.InsertArrayElementAtIndex(0);
				m_MaterialsProperty.GetArrayElementAtIndex(0).objectReferenceValue = SpriteMeshUtils.defaultMaterial;
				serializedObject.ApplyModifiedProperties();
			}
		}

		public void OnDisable()
		{
			if(target)
			{
#if UNITY_5_5_OR_NEWER

#else
				EditorUtility.SetSelectedWireframeHidden(m_SpriteMeshInstance.cachedRenderer, false);
#endif
			}
		}

		bool HasBindPoses()
		{
			bool hasBindPoses = false;
			
			if(m_SpriteMeshData && m_SpriteMeshData.bindPoses != null && m_SpriteMeshData.bindPoses.Length > 0)
			{
				hasBindPoses = true;
			}

			return hasBindPoses;
		}

		void SetupBoneList()
		{
			if(HasBindPoses() && m_BoneTransformsProperty.arraySize != m_SpriteMeshData.bindPoses.Length)
			{
				int oldSize = m_BoneTransformsProperty.arraySize;

				serializedObject.Update();

				m_BoneTransformsProperty.arraySize = m_SpriteMeshData.bindPoses.Length;

				for(int i = oldSize; i < m_BoneTransformsProperty.arraySize; ++i)
				{
					SerializedProperty element = m_BoneTransformsProperty.GetArrayElementAtIndex(i);
					element.objectReferenceValue = null;
				}
				
				serializedObject.ApplyModifiedProperties();
			}

			mBoneList = new ReorderableList(serializedObject,m_BoneTransformsProperty,!HasBindPoses(),true,!HasBindPoses(),!HasBindPoses());

			mBoneList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {

				SerializedProperty boneProperty = mBoneList.serializedProperty.GetArrayElementAtIndex(index);

				rect.y += 1.5f;

				float labelWidth = 0f;

				if(HasBindPoses() && index < m_SpriteMeshData.bindPoses.Length)
				{
					labelWidth = EditorGUIUtility.labelWidth;
					EditorGUI.LabelField( new Rect(rect.x, rect.y, labelWidth, EditorGUIUtility.singleLineHeight), new GUIContent(m_SpriteMeshData.bindPoses[index].name));
				}

				EditorGUI.BeginChangeCheck();

				EditorGUI.PropertyField( new Rect(rect.x + labelWidth, rect.y, rect.width - labelWidth, EditorGUIUtility.singleLineHeight), boneProperty, GUIContent.none);

				if(EditorGUI.EndChangeCheck())
				{
					Transform l_NewTransform = boneProperty.objectReferenceValue as Transform;
					if(l_NewTransform && !l_NewTransform.GetComponent<Bone2D>())
					{
						boneProperty.objectReferenceValue = null;
					}
				}
			};

			mBoneList.drawHeaderCallback = (Rect rect) => {  
				EditorGUI.LabelField(rect, "Bones");
			};

			mBoneList.onSelectCallback = (ReorderableList list) => {};
		}


		public override void OnInspectorGUI()
		{
#if UNITY_5_5_OR_NEWER

#else
			EditorUtility.SetSelectedWireframeHidden(m_SpriteMeshInstance.cachedRenderer, !m_SpriteMeshInstance.cachedSkinnedRenderer);
#endif

			EditorGUI.BeginChangeCheck();

			serializedObject.Update();

			EditorGUILayout.PropertyField(m_SpriteMeshProperty);

			serializedObject.ApplyModifiedProperties();

			if(EditorGUI.EndChangeCheck())
			{
				UpdateSpriteMeshData();
				UpdateRenderers();
				SetupBoneList();
			}

			serializedObject.Update();

			EditorGUILayout.PropertyField(m_ColorProperty);

			if(m_MaterialsProperty.arraySize == 0)
			{
				m_MaterialsProperty.InsertArrayElementAtIndex(0);
			}
			EditorGUILayout.PropertyField(m_MaterialsProperty.GetArrayElementAtIndex(0), new GUIContent("Material"), true, new GUILayoutOption[0]);
			
			EditorGUILayout.Space();
			
			EditorGUIExtra.SortingLayerField(new GUIContent("Sorting Layer"), m_SortingLayerID, EditorStyles.popup, EditorStyles.label);
			EditorGUILayout.PropertyField(m_SortingOrder, new GUIContent("Order in Layer"));

			EditorGUILayout.Space();

			if(!HasBindPoses())
			{
				List<Bone2D> bones = new List<Bone2D>();

				EditorGUI.BeginChangeCheck();

				Transform root = EditorGUILayout.ObjectField("Set bones",null,typeof(Transform),true) as Transform;

				if(EditorGUI.EndChangeCheck())
				{
					if(root)
					{
						root.GetComponentsInChildren<Bone2D>(bones);
					}

					Undo.RegisterCompleteObjectUndo(m_SpriteMeshInstance,"set bones");

					m_BoneTransformsProperty.arraySize = bones.Count;

					for(int i = 0; i < bones.Count; ++i)
					{
						m_BoneTransformsProperty.GetArrayElementAtIndex(i).objectReferenceValue = bones[i].transform;
					}

					UpdateRenderers();
				}
			}

			EditorGUI.BeginChangeCheck();

			if(mBoneList != null)
			{
				mBoneList.DoLayoutList();
			}

			serializedObject.ApplyModifiedProperties();

			if(EditorGUI.EndChangeCheck())
			{
				UpdateRenderers();
			}

			if(m_SpriteMeshInstance.spriteMesh)
			{
				if(SpriteMeshUtils.HasNullBones(m_SpriteMeshInstance))
				{
					EditorGUILayout.HelpBox("Warning:\nBone list contains null references.", MessageType.Warning);
				}
				
				if(m_SpriteMeshInstance.spriteMesh.sharedMesh.bindposes.Length != m_SpriteMeshInstance.bones.Count)
				{
					EditorGUILayout.HelpBox("Warning:\nNumber of SpriteMesh Bind Poses and number of Bones does not match.", MessageType.Warning);
				}
			}

		}

		void UpdateSpriteMeshData()
		{
			m_SpriteMeshData = null;
			
			if(m_SpriteMeshProperty != null && m_SpriteMeshProperty.objectReferenceValue)
			{
				m_SpriteMeshData = SpriteMeshUtils.LoadSpriteMeshData(m_SpriteMeshProperty.objectReferenceValue as SpriteMesh);
			}
		}

		void UpdateRenderers()
		{
			m_UndoGroup = Undo.GetCurrentGroup();

			EditorApplication.delayCall += DoUpdateRenderer;
		}

		void DoUpdateRenderer()
		{
			SpriteMeshUtils.UpdateRenderer(m_SpriteMeshInstance);

#if UNITY_5_5_OR_NEWER

#else
			EditorUtility.SetSelectedWireframeHidden(m_SpriteMeshInstance.cachedRenderer, !m_SpriteMeshInstance.cachedSkinnedRenderer);
#endif

			Undo.CollapseUndoOperations(m_UndoGroup);
			SceneView.RepaintAll();
		}
	}
}
