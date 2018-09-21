using UnityEngine;
using UnityEditor;
using UnityEditor.Sprites;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Anima2D
{
	[InitializeOnLoad]
	public class SpriteMeshPostprocessor : AssetPostprocessor
	{
		static Dictionary<string,string> s_SpriteMeshToTextureCache = new Dictionary<string, string>();
		
		static bool s_Initialized = false;
		
		static SpriteMeshPostprocessor()
		{
			if(!Application.isPlaying)
			{
				EditorApplication.delayCall += Initialize;
			}
		}
		
		public static SpriteMesh GetSpriteMeshFromSprite(Sprite sprite)
		{
			string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(sprite));
			
			if(s_SpriteMeshToTextureCache.ContainsValue(guid))
			{
				foreach(KeyValuePair<string,string> pair in s_SpriteMeshToTextureCache)
				{
					if(pair.Value.Equals(guid))
					{
						SpriteMesh spriteMesh = LoadSpriteMesh(AssetDatabase.GUIDToAssetPath(pair.Key));
						
						if(spriteMesh && spriteMesh.sprite == sprite)
						{
							return spriteMesh;
						}
					}
				}
			}
			
			return null;
		}
		
		static void Initialize() 
		{
			s_SpriteMeshToTextureCache.Clear();
			
			string[] spriteMeshGUIDs = AssetDatabase.FindAssets("t:SpriteMesh");
			
			List<string> needsOverride = new List<string>();
			
			foreach(string guid in spriteMeshGUIDs)
			{
				SpriteMesh spriteMesh = LoadSpriteMesh(AssetDatabase.GUIDToAssetPath(guid));
				
				if(spriteMesh)
				{
					UpdateCachedSpriteMesh(spriteMesh);
					UpgradeSpriteMesh(spriteMesh);
					
					if(s_SpriteMeshToTextureCache.ContainsKey(guid) &&
					   SpriteMeshUtils.NeedsOverride(spriteMesh))
					{
						needsOverride.Add(s_SpriteMeshToTextureCache[guid]);
					}
				}
			}
			
			s_Initialized = true;
			
			needsOverride = needsOverride.Distinct().ToList();
			
			AssetDatabase.StartAssetEditing();
			
			foreach(string textureGuid in needsOverride)
			{
				AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(textureGuid));
			}
			
			AssetDatabase.StopAssetEditing();
			
		}
		
		static void UpgradeSpriteMesh(SpriteMesh spriteMesh)
		{
			if(spriteMesh)
			{
				SerializedObject spriteMeshSO = new SerializedObject(spriteMesh);
				SerializedProperty apiVersionProp = spriteMeshSO.FindProperty("m_ApiVersion");
				
				if(apiVersionProp.intValue < SpriteMesh.api_version)
				{
					if(apiVersionProp.intValue < 1)
					{
						Upgrade_001(spriteMeshSO);
					}
					
					if(apiVersionProp.intValue < 2)
					{
						Upgrade_002(spriteMeshSO);
					}
					
					if(apiVersionProp.intValue < 3)
					{
						Upgrade_003(spriteMeshSO);
					}

					if(apiVersionProp.intValue < 4)
					{
						Upgrade_004(spriteMeshSO);
					}
					
					spriteMeshSO.Update();
					apiVersionProp.intValue = SpriteMesh.api_version;
					spriteMeshSO.ApplyModifiedProperties();
					
					AssetDatabase.SaveAssets();
				}
			}
		}
		
		static void Upgrade_001(SerializedObject spriteMeshSO)
		{
			spriteMeshSO.Update();
			
			SerializedProperty bindPosesProp = spriteMeshSO.FindProperty("bindPoses");
			
			for(int i = 0; i < bindPosesProp.arraySize; ++i)
			{
				SerializedProperty bindPoseProp = bindPosesProp.GetArrayElementAtIndex(i);
				bindPoseProp.FindPropertyRelative("color").colorValue = ColorRing.GetColor(i);
			}
			
			SerializedProperty pivotPointProp = spriteMeshSO.FindProperty("pivotPoint");
			SerializedProperty spriteProp = spriteMeshSO.FindProperty("m_Sprite");
			Sprite sprite = spriteProp.objectReferenceValue as Sprite;
			
			if(sprite)
			{
				pivotPointProp.vector2Value = SpriteMeshUtils.GetPivotPoint(sprite);
			}
			
			spriteMeshSO.ApplyModifiedProperties();
		}
		
		static void Upgrade_002(SerializedObject spriteMeshSO)
		{
			spriteMeshSO.Update();
			
			SpriteMesh spriteMesh = spriteMeshSO.targetObject as SpriteMesh;
			
			SerializedProperty sharedMeshProp = spriteMeshSO.FindProperty("m_SharedMesh");
			SerializedProperty sharedMaterialsProp = spriteMeshSO.FindProperty("m_SharedMaterials");
			SerializedProperty pivotPointProp = spriteMeshSO.FindProperty("pivotPoint");
			SerializedProperty texVerticesProp = spriteMeshSO.FindProperty("texVertices");
			SerializedProperty edgesProp = spriteMeshSO.FindProperty("edges");
			SerializedProperty holesProp = spriteMeshSO.FindProperty("holes");
			SerializedProperty indicesProp = spriteMeshSO.FindProperty("indices");
			
			string path = AssetDatabase.GetAssetPath(spriteMeshSO.targetObject);
			GameObject go = AssetDatabase.LoadAssetAtPath(path,typeof(GameObject)) as GameObject;
			
			if(go)
			{
				Object.DestroyImmediate(go,true);
			}
			
			Material material = AssetDatabase.LoadAssetAtPath(path,typeof(Material)) as Material;
			
			if(material)
			{
				sharedMaterialsProp.arraySize = 1;
				sharedMaterialsProp.GetArrayElementAtIndex(0).objectReferenceValue = material;
				material.hideFlags = HideFlags.HideInHierarchy;
				EditorUtility.SetDirty(material);
			}
			
			Mesh mesh = AssetDatabase.LoadAssetAtPath(path,typeof(Mesh)) as Mesh;
			
			if(mesh)
			{
				mesh.hideFlags = HideFlags.HideInHierarchy;
				sharedMeshProp.objectReferenceValue = mesh;
				EditorUtility.SetDirty(mesh);
			}
			
			spriteMeshSO.ApplyModifiedProperties();
			
			SpriteMeshData spriteMeshData = SpriteMeshUtils.LoadSpriteMeshData(spriteMesh);
			
			if(!spriteMeshData)
			{
				spriteMeshData = ScriptableObject.CreateInstance<SpriteMeshData>();
				AssetDatabase.AddObjectToAsset(spriteMeshData,path);
			}
			
			if(spriteMeshData)
			{
				spriteMeshData.name = spriteMeshSO.targetObject.name + "_Data";
				spriteMeshData.hideFlags = HideFlags.HideInHierarchy;
				
				SerializedObject spriteMeshDataSO = new SerializedObject(spriteMeshData);
				SerializedProperty smdPivotPointProp = spriteMeshDataSO.FindProperty("m_PivotPoint");
				SerializedProperty smdVerticesProp = spriteMeshDataSO.FindProperty("m_Vertices");
				SerializedProperty smdBoneWeightsProp = spriteMeshDataSO.FindProperty("m_BoneWeights");
				SerializedProperty smdEdgesProp = spriteMeshDataSO.FindProperty("m_Edges");
				SerializedProperty smdHolesProp = spriteMeshDataSO.FindProperty("m_Holes");
				SerializedProperty smdIndicesProp = spriteMeshDataSO.FindProperty("m_Indices");
				
				spriteMeshDataSO.Update();
				
				smdPivotPointProp.vector2Value = pivotPointProp.vector2Value;
				
				smdVerticesProp.arraySize = texVerticesProp.arraySize;
				smdBoneWeightsProp.arraySize = texVerticesProp.arraySize;
				
				for(int i = 0; i < smdVerticesProp.arraySize; ++i)
				{
					SerializedProperty vertexProp = texVerticesProp.GetArrayElementAtIndex(i);
					SerializedProperty vertexPositionProp = vertexProp.FindPropertyRelative("vertex");
					SerializedProperty boneWeightProp = vertexProp.FindPropertyRelative("boneWeight");
					SerializedProperty boneIndex0Prop = boneWeightProp.FindPropertyRelative("boneIndex0");
					SerializedProperty boneIndex1Prop = boneWeightProp.FindPropertyRelative("boneIndex1");
					SerializedProperty boneIndex2Prop = boneWeightProp.FindPropertyRelative("boneIndex2");
					SerializedProperty boneIndex3Prop = boneWeightProp.FindPropertyRelative("boneIndex3");
					SerializedProperty boneWeight0Prop = boneWeightProp.FindPropertyRelative("weight0");
					SerializedProperty boneWeight1Prop = boneWeightProp.FindPropertyRelative("weight1");
					SerializedProperty boneWeight2Prop = boneWeightProp.FindPropertyRelative("weight2");
					SerializedProperty boneWeight3Prop = boneWeightProp.FindPropertyRelative("weight3");
					
					SerializedProperty smdVertexProp = smdVerticesProp.GetArrayElementAtIndex(i);
					SerializedProperty smdBoneWeightProp = smdBoneWeightsProp.GetArrayElementAtIndex(i);
					SerializedProperty smdBoneIndex0Prop = smdBoneWeightProp.FindPropertyRelative("boneIndex0");
					SerializedProperty smdBoneIndex1Prop = smdBoneWeightProp.FindPropertyRelative("boneIndex1");
					SerializedProperty smdBoneIndex2Prop = smdBoneWeightProp.FindPropertyRelative("boneIndex2");
					SerializedProperty smdBoneIndex3Prop = smdBoneWeightProp.FindPropertyRelative("boneIndex3");
					SerializedProperty smdBoneWeight0Prop = smdBoneWeightProp.FindPropertyRelative("weight0");
					SerializedProperty smdBoneWeight1Prop = smdBoneWeightProp.FindPropertyRelative("weight1");
					SerializedProperty smdBoneWeight2Prop = smdBoneWeightProp.FindPropertyRelative("weight2");
					SerializedProperty smdBoneWeight3Prop = smdBoneWeightProp.FindPropertyRelative("weight3");
					
					smdVertexProp.vector2Value = vertexPositionProp.vector2Value;
					smdBoneIndex0Prop.intValue = boneIndex0Prop.intValue;
					smdBoneIndex1Prop.intValue = boneIndex1Prop.intValue;
					smdBoneIndex2Prop.intValue = boneIndex2Prop.intValue;
					smdBoneIndex3Prop.intValue = boneIndex3Prop.intValue;
					smdBoneWeight0Prop.floatValue = boneWeight0Prop.floatValue;
					smdBoneWeight1Prop.floatValue = boneWeight1Prop.floatValue;
					smdBoneWeight2Prop.floatValue = boneWeight2Prop.floatValue;
					smdBoneWeight3Prop.floatValue = boneWeight3Prop.floatValue;
				}
				
				smdEdgesProp.arraySize = edgesProp.arraySize;
				
				for(int i = 0; i < smdEdgesProp.arraySize; ++i)
				{
					SerializedProperty edgeProp = edgesProp.GetArrayElementAtIndex(i);
					SerializedProperty smdEdgeProp = smdEdgesProp.GetArrayElementAtIndex(i);
					
					smdEdgeProp.FindPropertyRelative("index1").intValue = edgeProp.FindPropertyRelative("index1").intValue;
					smdEdgeProp.FindPropertyRelative("index2").intValue = edgeProp.FindPropertyRelative("index2").intValue;
				}
				
				smdHolesProp.arraySize = holesProp.arraySize;
				
				for(int i = 0; i < smdHolesProp.arraySize; ++i)
				{
					SerializedProperty holeProp = holesProp.GetArrayElementAtIndex(i);
					SerializedProperty smdHoleProp = smdHolesProp.GetArrayElementAtIndex(i);
					
					smdHoleProp.vector2Value = holeProp.FindPropertyRelative("vertex").vector2Value;
				}
				
				smdIndicesProp.arraySize = indicesProp.arraySize;
				
				for(int i = 0; i < smdIndicesProp.arraySize; ++i)
				{
					smdIndicesProp.GetArrayElementAtIndex(i).intValue = indicesProp.GetArrayElementAtIndex(i).intValue;
				}
				
				spriteMeshDataSO.ApplyModifiedProperties();
				
				//Can't find the way to make SerializedProperty work with Matrix4x4, so we use reflection
				SetBindPoses(spriteMeshData,GetBindPoses(spriteMesh));
			}
		}
		
		static void Upgrade_003(SerializedObject spriteMeshSO)
		{
			SpriteMesh spriteMesh = spriteMeshSO.targetObject as SpriteMesh;
			SpriteMeshData spriteMeshData = SpriteMeshUtils.LoadSpriteMeshData(spriteMesh);
			
			if(spriteMesh.sprite && spriteMeshData)
			{
				TextureImporter textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(spriteMesh.sprite)) as TextureImporter;
				
				float maxImporterSize = textureImporter.maxTextureSize;
				
				int width = 1;
				int height = 1;
				
				SpriteMeshUtils.GetWidthAndHeight(textureImporter,ref width, ref height);
				
				int maxSize = Mathf.Max(width,height);
				
				float factor = maxSize / maxImporterSize;
				
				if(factor > 1f)
				{
					SerializedObject spriteMeshDataSO = new SerializedObject(spriteMeshData);
					SerializedProperty smdPivotPointProp = spriteMeshDataSO.FindProperty("m_PivotPoint");
					SerializedProperty smdVerticesProp = spriteMeshDataSO.FindProperty("m_Vertices");
					SerializedProperty smdHolesProp = spriteMeshDataSO.FindProperty("m_Holes");
					
					spriteMeshDataSO.Update();
					
					smdPivotPointProp.vector2Value = spriteMeshData.pivotPoint * factor;
					
					for(int i = 0; i < spriteMeshData.vertices.Length; ++i)
					{
						smdVerticesProp.GetArrayElementAtIndex(i).vector2Value = spriteMeshData.vertices[i] * factor;
					}
					
					for(int i = 0; i < spriteMeshData.holes.Length; ++i)
					{
						smdHolesProp.GetArrayElementAtIndex(i).vector2Value = spriteMeshData.holes[i] * factor;
					}
					
					spriteMeshDataSO.ApplyModifiedProperties();
					
					EditorUtility.SetDirty(spriteMeshData);
				}
			}
		}

		static void Upgrade_004(SerializedObject spriteMeshSO)
		{
			SerializedProperty materialsProp = spriteMeshSO.FindProperty("m_SharedMaterials");

			for(int i = 0; i < materialsProp.arraySize; ++i)
			{
				SerializedProperty materialProp = materialsProp.GetArrayElementAtIndex(i);
				Material material = materialProp.objectReferenceValue as Material;

				if(material)
				{
					GameObject.DestroyImmediate(material, true);
				}
			}

			spriteMeshSO.Update();
			materialsProp.arraySize = 0;
			spriteMeshSO.ApplyModifiedProperties();
		}

		static SpriteMesh.BindInfo[] GetBindPoses(SpriteMesh spriteMesh)
		{
			SpriteMesh.BindInfo[] result = null;
			
			FieldInfo fieldInfo = typeof(SpriteMesh).GetField("bindPoses",BindingFlags.Instance | BindingFlags.NonPublic);
			
			if(fieldInfo != null)
			{
				result = (SpriteMesh.BindInfo[])fieldInfo.GetValue(spriteMesh);
			}
			return result;
		}
		
		static void SetBindPoses(SpriteMeshData spriteMeshData, SpriteMesh.BindInfo[] bindPoses)
		{
			if(bindPoses != null && bindPoses.Length > 0)
			{
				BindInfo[] array = new BindInfo[bindPoses.Length];
				
				for(int i = 0; i < array.Length; ++i)
				{
					BindInfo b = new BindInfo();
					b.bindPose = bindPoses[i].bindPose;
					b.boneLength = bindPoses[i].boneLength;
					b.color = bindPoses[i].color;
					b.name = bindPoses[i].name;
					b.path = bindPoses[i].path;
					b.zOrder = bindPoses[i].zOrder;
					
					array[i] = b;
				}
				
				FieldInfo fieldInfo = typeof(SpriteMeshData).GetField("m_BindPoses",BindingFlags.Instance | BindingFlags.NonPublic);
				
				if(fieldInfo != null)
				{
					fieldInfo.SetValue(spriteMeshData,array);
				}
				
				EditorUtility.SetDirty(spriteMeshData);
			}
		}
		
		static void UpdateCachedSpriteMesh(SpriteMesh spriteMesh)
		{
			if(spriteMesh)
			{
				string key = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(spriteMesh));
				
				if(spriteMesh.sprite)
				{
					SpriteMesh spriteMeshFromSprite = GetSpriteMeshFromSprite(spriteMesh.sprite);
					
					if(!spriteMeshFromSprite || spriteMesh == spriteMeshFromSprite)
					{
						string value = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(SpriteUtility.GetSpriteTexture(spriteMesh.sprite,false)));
						
						s_SpriteMeshToTextureCache[key] = value;
					}else{
						Debug.LogWarning("Anima2D: SpriteMesh " + spriteMesh.name + " uses the same Sprite as " + spriteMeshFromSprite.name + ". Use only one SpriteMesh per Sprite.");
					}
					
				}else if(s_SpriteMeshToTextureCache.ContainsKey(key))
				{
					s_SpriteMeshToTextureCache.Remove(key);
				}
			}
		}
		
		static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			if(!s_Initialized) return;
			
			foreach(string importetAssetPath in importedAssets)
			{
				SpriteMesh spriteMesh = LoadSpriteMesh(importetAssetPath);
				
				if(spriteMesh)
				{
					UpdateCachedSpriteMesh(spriteMesh);
					UpgradeSpriteMesh(spriteMesh); 
				}
			}
		}
		
		static bool IsSpriteMesh(string assetPath)
		{
			return s_SpriteMeshToTextureCache.ContainsKey(AssetDatabase.AssetPathToGUID(assetPath));
		}
		
		static SpriteMesh LoadSpriteMesh(string assetPath)
		{
			return AssetDatabase.LoadAssetAtPath(assetPath,typeof(SpriteMesh)) as SpriteMesh;
		}
		
		void OnPreprocessTexture()
		{
			if(!s_Initialized) return;
			
			string guid = AssetDatabase.AssetPathToGUID(assetPath);
			
			if(s_SpriteMeshToTextureCache.ContainsValue(guid))
			{
				TextureImporter textureImporter  = (TextureImporter) assetImporter;
				SerializedObject textureImporterSO = new SerializedObject(textureImporter);
				SerializedProperty textureImporterSprites = textureImporterSO.FindProperty("m_SpriteSheet.m_Sprites");
				
				foreach(KeyValuePair<string,string> pair in s_SpriteMeshToTextureCache)
				{
					if(pair.Value == guid)
					{
						SpriteMesh spriteMesh = LoadSpriteMesh(AssetDatabase.GUIDToAssetPath(pair.Key));
						SpriteMeshData spriteMeshData = SpriteMeshUtils.LoadSpriteMeshData(spriteMesh);
						
						if(spriteMesh && spriteMeshData && spriteMesh.sprite && spriteMeshData.vertices.Length > 0)
						{
							textureImporterSO.FindProperty("m_SpriteMeshType").intValue = 1;

							if(textureImporter.spriteImportMode == SpriteImportMode.Multiple)
							{
								SerializedProperty spriteProp = null;
								int i = 0;
								string name = "";
								
								while(i < textureImporterSprites.arraySize && name != spriteMesh.sprite.name)
								{
									spriteProp = textureImporterSprites.GetArrayElementAtIndex(i);
									name = spriteProp.FindPropertyRelative("m_Name").stringValue;
									
									++i;
								}
								
								if(name == spriteMesh.sprite.name)
								{
									Rect textureRect = SpriteMeshUtils.CalculateSpriteRect(spriteMesh,5);
									spriteProp.FindPropertyRelative("m_Rect").rectValue = textureRect;
									spriteProp.FindPropertyRelative("m_Alignment").intValue = 9;
									spriteProp.FindPropertyRelative("m_Pivot").vector2Value = Vector2.Scale(spriteMeshData.pivotPoint - textureRect.position, new Vector2(1f / textureRect.size.x, 1f / textureRect.size.y));
								}
							}else{
								int width = 0;
								int height = 0;
								SpriteMeshUtils.GetSpriteTextureSize(spriteMesh.sprite,ref width,ref height);
								textureImporterSO.FindProperty("m_Alignment").intValue = 9;
								textureImporterSO.FindProperty("m_SpritePivot").vector2Value = Vector2.Scale(spriteMeshData.pivotPoint, new Vector2(1f / width, 1f / height));
							}
						}
					}
				}
				
				textureImporterSO.ApplyModifiedProperties();
			}
		}
		
		void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
		{
			if(!s_Initialized) return;
			
			string guid = AssetDatabase.AssetPathToGUID(assetPath);
			
			if(s_SpriteMeshToTextureCache.ContainsValue(guid))
			{
				foreach(Sprite sprite in sprites)
				{
					foreach(KeyValuePair<string,string> pair in s_SpriteMeshToTextureCache)
					{
						if(pair.Value == guid)
						{
							SpriteMesh spriteMesh = LoadSpriteMesh(AssetDatabase.GUIDToAssetPath(pair.Key));
							
							if(spriteMesh && spriteMesh.sprite && sprite.name == spriteMesh.sprite.name)
							{
								DoSpriteOverride(spriteMesh,sprite);
								break;
							}
						}
					}
				}
			}
		}
		
		void DoSpriteOverride(SpriteMesh spriteMesh, Sprite sprite)
		{
			TextureImporter textureImporter = (TextureImporter) assetImporter;

#if UNITY_5_1 || UNITY_5_2 || UNITY_5_3_OR_NEWER
			Debug.Assert(textureImporter.spriteImportMode == SpriteImportMode.Single ||
						 textureImporter.spriteImportMode == SpriteImportMode.Multiple,
						"Incompatible Sprite Mode. Use Single or Multiple.");
#endif

			SpriteMeshData spriteMeshData = SpriteMeshUtils.LoadSpriteMeshData(spriteMesh);
			
			if(spriteMeshData) 
			{
				Vector2 factor = Vector2.one;
				Rect spriteRect = sprite.rect;
				Rect rectTextureSpace = new Rect();

				if(textureImporter.spriteImportMode == SpriteImportMode.Single)
				{
					int width = 0;
					int height = 0;

					SpriteMeshUtils.GetSpriteTextureSize(spriteMesh.sprite,ref width,ref height);
					rectTextureSpace = new Rect(0, 0, width, height);
				}
				else if(textureImporter.spriteImportMode == SpriteImportMode.Multiple)
				{
					rectTextureSpace = SpriteMeshUtils.CalculateSpriteRect(spriteMesh,5);
				}
				
				factor = new Vector2(spriteRect.width/rectTextureSpace.width,spriteRect.height/rectTextureSpace.height);
				
				Vector2[] newVertices = new List<Vector2>(spriteMeshData.vertices).ConvertAll( v => MathUtils.ClampPositionInRect(Vector2.Scale(v,factor),spriteRect) - spriteRect.position ).ToArray();
				ushort[] newIndices = new List<int>(spriteMeshData.indices).ConvertAll<ushort>( i => (ushort)i ).ToArray();
				
				sprite.OverrideGeometry(newVertices, newIndices);
			}
		}
	}
}
