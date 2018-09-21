using UnityEngine;
using UnityEditor;
using UnityEditor.Sprites;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TriangleNet.Geometry;

namespace Anima2D 
{
	public class SpriteMeshUtils
	{
		static Material m_DefaultMaterial = null;
		public static Material defaultMaterial {
			get {
				if(!m_DefaultMaterial)
				{
					GameObject go = new GameObject();
					SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
					m_DefaultMaterial = sr.sharedMaterial;
					GameObject.DestroyImmediate(go);
				}
				
				return m_DefaultMaterial;
			}
		}
		public class WeightedTriangle
		{
			int m_P1;
			int m_P2;
			int m_P3;
			float m_W1;
			float m_W2;
			float m_W3;
			float m_Weight;
			
			public int p1 { get { return m_P1; } }
			public int p2 { get { return m_P2; } }
			public int p3 { get { return m_P3; } }
			public float w1 { get { return m_W1; } }
			public float w2 { get { return m_W2; } }
			public float w3 { get { return m_W3; } }
			public float weight { get { return m_Weight; } }
			
			public WeightedTriangle(int _p1, int _p2, int _p3,
			                        float _w1, float _w2, float _w3)
			{
				m_P1 = _p1;
				m_P2 = _p2;
				m_P3 = _p3;
				m_W1 = _w1;
				m_W2 = _w2;
				m_W3 = _w3;
				m_Weight = (w1 + w2 + w3) / 3f;
			}
		}
		
		public static SpriteMesh CreateSpriteMesh(Sprite sprite)
		{
			SpriteMesh spriteMesh = SpriteMeshPostprocessor.GetSpriteMeshFromSprite(sprite);
			SpriteMeshData spriteMeshData = null;
			
			if(!spriteMesh && sprite)
			{
				string spritePath = AssetDatabase.GetAssetPath(sprite);
				string directory = Path.GetDirectoryName(spritePath);
				string assetPath = AssetDatabase.GenerateUniqueAssetPath(directory + Path.DirectorySeparatorChar + sprite.name + ".asset");
				
				spriteMesh = ScriptableObject.CreateInstance<SpriteMesh>();
				InitFromSprite(spriteMesh,sprite);
				AssetDatabase.CreateAsset(spriteMesh,assetPath);
				
				spriteMeshData = ScriptableObject.CreateInstance<SpriteMeshData>();
				spriteMeshData.name = spriteMesh.name + "_Data";
				spriteMeshData.hideFlags = HideFlags.HideInHierarchy;
				InitFromSprite(spriteMeshData,sprite);
				AssetDatabase.AddObjectToAsset(spriteMeshData,assetPath);
				
				UpdateAssets(spriteMesh,spriteMeshData);
				
				AssetDatabase.SaveAssets();
				AssetDatabase.ImportAsset(assetPath);
				
				Selection.activeObject = spriteMesh;
			}
			
			return spriteMesh;
		}
		
		public static void CreateSpriteMesh(Texture2D texture)
		{
			if(texture)
			{
				Object[] objects = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(texture));
				
				for (int i = 0; i < objects.Length; i++)
				{
					Object o = objects [i];
					Sprite sprite = o as Sprite;
					if (sprite) {
						EditorUtility.DisplayProgressBar ("Processing " + texture.name, sprite.name, (i+1) / (float)objects.Length);
						CreateSpriteMesh(sprite);
					}
				}
				
				EditorUtility.ClearProgressBar();
			}
		}
		
		public static SpriteMeshData LoadSpriteMeshData(SpriteMesh spriteMesh)
		{
			if(spriteMesh)
			{
				UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(spriteMesh));
				
				foreach(UnityEngine.Object asset in assets)
				{
					SpriteMeshData data = asset as SpriteMeshData;
					
					if(data)
					{
						return data;
					}
				}
			}
			
			return null;
		}
		
		public static void UpdateAssets(SpriteMesh spriteMesh)
		{
			UpdateAssets(spriteMesh, SpriteMeshUtils.LoadSpriteMeshData(spriteMesh));
		}
		
		public static void UpdateAssets(SpriteMesh spriteMesh, SpriteMeshData spriteMeshData)
		{
			if(spriteMesh && spriteMeshData)
			{
				string spriteMeshPath = AssetDatabase.GetAssetPath(spriteMesh);
				
				SerializedObject spriteMeshSO = new SerializedObject(spriteMesh);
				SerializedProperty sharedMeshProp = spriteMeshSO.FindProperty("m_SharedMesh");
				
				if(!spriteMesh.sharedMesh)
				{
					Mesh mesh = new Mesh();
					mesh.hideFlags = HideFlags.HideInHierarchy;
					AssetDatabase.AddObjectToAsset(mesh,spriteMeshPath);
					
					spriteMeshSO.Update();
					sharedMeshProp.objectReferenceValue = mesh;
					spriteMeshSO.ApplyModifiedProperties();
					EditorUtility.SetDirty(mesh);
				}
				
				spriteMesh.sharedMesh.name = spriteMesh.name;

				spriteMeshData.hideFlags = HideFlags.HideInHierarchy;
				EditorUtility.SetDirty(spriteMeshData);
				
				int width = 0;
				int height = 0;
				
				GetSpriteTextureSize(spriteMesh.sprite,ref width,ref height);

				Vector3[] vertices = GetMeshVertices(spriteMesh.sprite, spriteMeshData);

				Vector2 textureWidthHeightInv = new Vector2(1f/width,1f/height);
				
				Vector2[] uvs = (new List<Vector2>(spriteMeshData.vertices)).ConvertAll( v => Vector2.Scale(v,textureWidthHeightInv)).ToArray();
				
				Vector3[] normals = (new List<Vector3>(vertices)).ConvertAll( v => Vector3.back ).ToArray();
				
				BoneWeight[] boneWeightsData = spriteMeshData.boneWeights;
				
				if(boneWeightsData.Length != spriteMeshData.vertices.Length)
				{
					boneWeightsData = new BoneWeight[spriteMeshData.vertices.Length];
				}
				
				List<UnityEngine.BoneWeight> boneWeights = new List<UnityEngine.BoneWeight>(boneWeightsData.Length);
				
				List<float> verticesOrder = new List<float>(spriteMeshData.vertices.Length);
				
				for (int i = 0; i < boneWeightsData.Length; i++)
				{
					BoneWeight boneWeight = boneWeightsData[i];
					
					List< KeyValuePair<int,float> > pairs = new List<KeyValuePair<int, float>>();
					pairs.Add(new KeyValuePair<int, float>(boneWeight.boneIndex0,boneWeight.weight0));
					pairs.Add(new KeyValuePair<int, float>(boneWeight.boneIndex1,boneWeight.weight1));
					pairs.Add(new KeyValuePair<int, float>(boneWeight.boneIndex2,boneWeight.weight2));
					pairs.Add(new KeyValuePair<int, float>(boneWeight.boneIndex3,boneWeight.weight3));
					
					pairs = pairs.OrderByDescending(s=>s.Value).ToList();
					
					UnityEngine.BoneWeight boneWeight2 = new UnityEngine.BoneWeight();
					boneWeight2.boneIndex0 = Mathf.Max(0,pairs[0].Key);
					boneWeight2.boneIndex1 = Mathf.Max(0,pairs[1].Key);
					boneWeight2.boneIndex2 = Mathf.Max(0,pairs[2].Key);
					boneWeight2.boneIndex3 = Mathf.Max(0,pairs[3].Key);
					boneWeight2.weight0 = pairs[0].Value;
					boneWeight2.weight1 = pairs[1].Value;
					boneWeight2.weight2 = pairs[2].Value;
					boneWeight2.weight3 = pairs[3].Value;
					
					boneWeights.Add(boneWeight2);
					
					float vertexOrder = i;
					
					if(spriteMeshData.bindPoses.Length > 0)
					{
						vertexOrder = spriteMeshData.bindPoses[boneWeight2.boneIndex0].zOrder * boneWeight2.weight0 +
							spriteMeshData.bindPoses[boneWeight2.boneIndex1].zOrder * boneWeight2.weight1 +
								spriteMeshData.bindPoses[boneWeight2.boneIndex2].zOrder * boneWeight2.weight2 +
								spriteMeshData.bindPoses[boneWeight2.boneIndex3].zOrder * boneWeight2.weight3;
					}
					
					verticesOrder.Add(vertexOrder);
				}
				
				List<WeightedTriangle> weightedTriangles = new List<WeightedTriangle>(spriteMeshData.indices.Length / 3);
				
				for(int i = 0; i < spriteMeshData.indices.Length; i+=3)
				{
					int p1 = spriteMeshData.indices[i];
					int p2 = spriteMeshData.indices[i+1];
					int p3 = spriteMeshData.indices[i+2];
					
					weightedTriangles.Add(new WeightedTriangle(p1,p2,p3,
					                                           verticesOrder[p1],
					                                           verticesOrder[p2],
					                                           verticesOrder[p3]));
				}
				
				weightedTriangles = weightedTriangles.OrderBy( t => t.weight ).ToList();
				
				List<int> indices = new List<int>(spriteMeshData.indices.Length);
				
				for(int i = 0; i < weightedTriangles.Count; ++i)
				{
					WeightedTriangle t = weightedTriangles[i];
					indices.Add(t.p1);
					indices.Add(t.p2);
					indices.Add(t.p3);
				}

				List<Matrix4x4> bindposes = (new List<BindInfo>(spriteMeshData.bindPoses)).ConvertAll( p => p.bindPose );

				for (int i = 0; i < bindposes.Count; i++)
				{
					Matrix4x4 bindpose = bindposes [i];

					bindpose.m23 = 0f;

					bindposes[i] = bindpose;
				}	

				spriteMesh.sharedMesh.Clear();
				spriteMesh.sharedMesh.vertices = vertices;
				spriteMesh.sharedMesh.uv = uvs;
				spriteMesh.sharedMesh.triangles = indices.ToArray();
				spriteMesh.sharedMesh.normals = normals;
				spriteMesh.sharedMesh.boneWeights = boneWeights.ToArray();
				spriteMesh.sharedMesh.bindposes = bindposes.ToArray();
				spriteMesh.sharedMesh.RecalculateBounds();
#if UNITY_5_6_OR_NEWER
				spriteMesh.sharedMesh.RecalculateTangents();
#endif
				RebuildBlendShapes(spriteMesh);
			}
		}

		public static Vector3[] GetMeshVertices(SpriteMesh spriteMesh)
		{
			return GetMeshVertices(spriteMesh.sprite, LoadSpriteMeshData(spriteMesh));
		}

		public static Vector3[] GetMeshVertices(Sprite sprite, SpriteMeshData spriteMeshData)
		{
			float pixelsPerUnit = GetSpritePixelsPerUnit(sprite);

			return (new List<Vector2>(spriteMeshData.vertices)).ConvertAll( v => TexCoordToVertex(spriteMeshData.pivotPoint,v,pixelsPerUnit)).ToArray();
		}

		public static void GetSpriteTextureSize(Sprite sprite, ref int width, ref int height)
		{
			if(sprite)
			{
				Texture2D texture = SpriteUtility.GetSpriteTexture(sprite,false);
				
				TextureImporter textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
				
				GetWidthAndHeight(textureImporter,ref width, ref height);
			}
		}
		
		public static void GetWidthAndHeight(TextureImporter textureImporter, ref int width, ref int height)
		{
			MethodInfo methodInfo = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.Instance | BindingFlags.NonPublic);
			
			if(methodInfo != null)
			{
				object[] parameters = new object[] { null, null };
				methodInfo.Invoke(textureImporter,parameters);
				width = (int)parameters[0];
				height = (int)parameters[1];
			}
		}
		
		public static float GetSpritePixelsPerUnit(Sprite sprite)
		{
			TextureImporter textureImporter = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(sprite)) as TextureImporter;
			
			return textureImporter.spritePixelsPerUnit;
		}
		
		static void InitFromSprite(SpriteMesh spriteMesh, Sprite sprite)
		{
			SerializedObject spriteMeshSO = new SerializedObject(spriteMesh);
			SerializedProperty spriteProp = spriteMeshSO.FindProperty("m_Sprite");
			SerializedProperty apiProp = spriteMeshSO.FindProperty("m_ApiVersion");
			
			spriteMeshSO.Update();
			apiProp.intValue = SpriteMesh.api_version;
			spriteProp.objectReferenceValue = sprite;
			spriteMeshSO.ApplyModifiedProperties();
		}
		
		static void InitFromSprite(SpriteMeshData spriteMeshData, Sprite sprite)
		{
			Vector2[] vertices;
			IndexedEdge[] edges;
			int[] indices;
			Vector2 pivotPoint;
			
			if(sprite)
			{
				GetSpriteData(sprite, out vertices, out edges, out indices, out pivotPoint);
				
				spriteMeshData.vertices = vertices;
				spriteMeshData.edges = edges;
				spriteMeshData.indices = indices;
				spriteMeshData.pivotPoint = pivotPoint;
			}
		}
		
		public static void GetSpriteData(Sprite sprite, out Vector2[] vertices, out IndexedEdge[] edges, out int[] indices, out Vector2 pivotPoint)
		{
			int width = 0;
			int height = 0;
			
			GetSpriteTextureSize(sprite,ref width,ref height);
			
			pivotPoint = Vector2.zero;
			
			Vector2[] uvs = SpriteUtility.GetSpriteUVs(sprite,false);
			
			vertices = new Vector2[uvs.Length];
			
			for(int i = 0; i < uvs.Length; ++i)
			{
				vertices[i] = new Vector2(uvs[i].x * width, uvs[i].y * height);
			}
			
			ushort[] l_indices = sprite.triangles;
			
			indices = new int[l_indices.Length];
			
			for(int i = 0; i < l_indices.Length; ++i)
			{	
				indices[i] = (int)l_indices[i];
			}
			
			HashSet<IndexedEdge> edgesSet = new HashSet<IndexedEdge>();
			
			for(int i = 0; i < indices.Length; i += 3)
			{
				int index1 = indices[i];
				int index2 = indices[i+1];
				int index3 = indices[i+2];
				
				IndexedEdge edge1 = new IndexedEdge(index1,index2);
				IndexedEdge edge2 = new IndexedEdge(index2,index3);
				IndexedEdge edge3 = new IndexedEdge(index1,index3);
				
				if(edgesSet.Contains(edge1))
				{
					edgesSet.Remove(edge1);
				}else{
					edgesSet.Add(edge1);
				}
				
				if(edgesSet.Contains(edge2))
				{
					edgesSet.Remove(edge2);
				}else{
					edgesSet.Add(edge2);
				}
				
				if(edgesSet.Contains(edge3))
				{
					edgesSet.Remove(edge3);
				}else{
					edgesSet.Add(edge3);
				}
			}
			
			edges = new IndexedEdge[edgesSet.Count];
			int edgeIndex = 0;
			foreach(IndexedEdge edge in edgesSet)
			{
				edges[edgeIndex] = edge;
				++edgeIndex;
			}
			
			pivotPoint = GetPivotPoint(sprite);
		}
		
		public static void InitFromOutline(Texture2D texture, Rect rect, float detail, float alphaTolerance, bool holeDetection,
		                                   out List<Vector2> vertices, out List<IndexedEdge> indexedEdges, out List<int> indices)
		{
			vertices = new List<Vector2>();
			indexedEdges = new List<IndexedEdge>();
			indices = new List<int>();
			
			if(texture)
			{
				Vector2[][] paths = GenerateOutline(texture,rect,detail,(byte)(alphaTolerance * 255f),holeDetection);
				
				int startIndex = 0;
				for (int i = 0; i < paths.Length; i++)
				{
					Vector2[] path = paths [i];
					for (int j = 0; j < path.Length; j++)
					{
						vertices.Add(path[j] + rect.center);
						indexedEdges.Add(new IndexedEdge(startIndex + j,startIndex + ((j+1) % path.Length)));
					}
					startIndex += path.Length;
				}
				List<Hole> holes = new List<Hole>();
				Triangulate(vertices,indexedEdges,holes,ref indices);
			}
		}
		
		static Vector2[][] GenerateOutline(Texture2D texture, Rect rect, float detail, byte alphaTolerance, bool holeDetection)
		{
			Vector2[][] paths = null;
			
			MethodInfo methodInfo = typeof(SpriteUtility).GetMethod("GenerateOutline", BindingFlags.Static | BindingFlags.NonPublic);
			
			if(methodInfo != null)
			{
				object[] parameters = new object[] { texture,rect,detail,alphaTolerance,holeDetection,null };
				methodInfo.Invoke(null,parameters);
				
				paths = (Vector2[][]) parameters[5];
			}
			
			return paths;
		}
		
		public static void Triangulate(List<Vector2> vertices, List<IndexedEdge> edges, List<Hole> holes,ref List<int> indices)
		{
			indices.Clear();
			
			if(vertices.Count >= 3)
			{
				InputGeometry inputGeometry = new InputGeometry(vertices.Count);
				
				for(int i = 0; i < vertices.Count; ++i)
				{
					Vector2 position = vertices[i];
					inputGeometry.AddPoint(position.x,position.y);
				}
				
				for(int i = 0; i < edges.Count; ++i)
				{
					IndexedEdge edge = edges[i];
					inputGeometry.AddSegment(edge.index1,edge.index2);
				}
				
				for(int i = 0; i < holes.Count; ++i)
				{
					Vector2 hole = holes[i].vertex;
					inputGeometry.AddHole(hole.x,hole.y);
				}
				
				TriangleNet.Mesh triangleMesh = new TriangleNet.Mesh();

				triangleMesh.Triangulate(inputGeometry);
				
				foreach (TriangleNet.Data.Triangle triangle in triangleMesh.Triangles)
				{
					if(triangle.P0 >= 0 && triangle.P0 < vertices.Count &&
					   triangle.P0 >= 0 && triangle.P1 < vertices.Count &&
					   triangle.P0 >= 0 && triangle.P2 < vertices.Count)
					{
						indices.Add(triangle.P0);
						indices.Add(triangle.P2);
						indices.Add(triangle.P1);
					}
				}
			}
		}
		
		public static void Tessellate(List<Vector2> vertices, List<IndexedEdge> indexedEdges, List<Hole> holes, List<int> indices, float tessellationAmount)
		{
			if(tessellationAmount <= 0f)
			{
				return;
			}
			
			indices.Clear();
			
			if(vertices.Count >= 3)
			{
				InputGeometry inputGeometry = new InputGeometry(vertices.Count);
				
				for(int i = 0; i < vertices.Count; ++i)
				{
					Vector2 vertex = vertices[i];
					inputGeometry.AddPoint(vertex.x,vertex.y);
				}
				
				for(int i = 0; i < indexedEdges.Count; ++i)
				{
					IndexedEdge edge = indexedEdges[i];
					inputGeometry.AddSegment(edge.index1,edge.index2);
				}
				
				for(int i = 0; i < holes.Count; ++i)
				{
					Vector2 hole = holes[i].vertex;
					inputGeometry.AddHole(hole.x,hole.y);
				}
				
				TriangleNet.Mesh triangleMesh = new TriangleNet.Mesh();
				TriangleNet.Tools.Statistic statistic = new TriangleNet.Tools.Statistic();
				
				triangleMesh.Triangulate(inputGeometry);
				
				triangleMesh.Behavior.MinAngle = 20.0;
				triangleMesh.Behavior.SteinerPoints = -1;
				triangleMesh.Refine(true);
				
				statistic.Update(triangleMesh,1);
				
				triangleMesh.Refine(statistic.LargestArea / tessellationAmount);
				triangleMesh.Renumber();
				
				vertices.Clear();
				indexedEdges.Clear();
				
				foreach(TriangleNet.Data.Vertex vertex in triangleMesh.Vertices)
				{
					vertices.Add(new Vector2((float)vertex.X,(float)vertex.Y));
				}
				
				foreach(TriangleNet.Data.Segment segment in triangleMesh.Segments)
				{
					indexedEdges.Add(new IndexedEdge(segment.P0,segment.P1));
				}
				
				foreach (TriangleNet.Data.Triangle triangle in triangleMesh.Triangles)
				{
					if(triangle.P0 >= 0 && triangle.P0 < vertices.Count &&
					   triangle.P0 >= 0 && triangle.P1 < vertices.Count &&
					   triangle.P0 >= 0 && triangle.P2 < vertices.Count)
					{
						indices.Add(triangle.P0);
						indices.Add(triangle.P2);
						indices.Add(triangle.P1);
					}
				}
			}
		}
		
		public static Vector3 TexCoordToVertex(Vector2 pivotPoint, Vector2 vertex, float pixelsPerUnit)
		{
			return (Vector3)(vertex - pivotPoint) / pixelsPerUnit;
		}
		
		public static Vector2 VertexToTexCoord(SpriteMesh spriteMesh, Vector2 pivotPoint, Vector3 vertex, float pixelsPerUnit)
		{
			Vector2 texCoord = Vector3.zero;
			
			if(spriteMesh != null)
			{
				texCoord = (Vector2)vertex * pixelsPerUnit + pivotPoint;
			}
			
			return texCoord;
		}
		
		public static Rect GetRect(Sprite sprite)
		{
			float pixelsPerUnit = GetSpritePixelsPerUnit(sprite);
			float factor = pixelsPerUnit / sprite.pixelsPerUnit;
			Vector2 position = sprite.rect.position * factor;
			Vector2 size = sprite.rect.size * factor;
			
			return new Rect(position.x,position.y,size.x,size.y);
		}
		
		public static Vector2 GetPivotPoint(Sprite sprite)
		{
			float pixelsPerUnit = GetSpritePixelsPerUnit(sprite);
			return (sprite.pivot + sprite.rect.position) * pixelsPerUnit / sprite.pixelsPerUnit;
		}
		
		public static Rect CalculateSpriteRect(SpriteMesh spriteMesh, int padding)
		{
			int width = 0;
			int height = 0;
			
			GetSpriteTextureSize(spriteMesh.sprite,ref width,ref height);
			
			SpriteMeshData spriteMeshData = LoadSpriteMeshData(spriteMesh);
			
			Rect rect = spriteMesh.sprite.rect;
			
			if(spriteMeshData)
			{
				Vector2 min = new Vector2(float.MaxValue,float.MaxValue);
				Vector2 max = new Vector2(float.MinValue,float.MinValue);
				
				for (int i = 0; i < spriteMeshData.vertices.Length; i++)
				{
					Vector2 v = spriteMeshData.vertices[i];
					
					if(v.x < min.x) min.x = v.x;
					if(v.y < min.y) min.y = v.y;
					if(v.x > max.x) max.x = v.x;
					if(v.y > max.y) max.y = v.y;
				}
				
				rect.position = min - Vector2.one * padding;
				rect.size = (max - min) + Vector2.one * padding * 2f;
				rect = MathUtils.ClampRect(rect,new Rect(0f,0f,width,height));
			}
			
			return rect;
		}
		
		public static SpriteMeshInstance CreateSpriteMeshInstance(SpriteMesh spriteMesh, bool undo = true)
		{
			if(spriteMesh)
			{
				GameObject gameObject = new GameObject(spriteMesh.name);
				
				if(undo)
				{
					Undo.RegisterCreatedObjectUndo(gameObject,Undo.GetCurrentGroupName());
				}
				
				return CreateSpriteMeshInstance(spriteMesh, gameObject, undo);
			}
			
			return null;
		}
		
		public static SpriteMeshInstance CreateSpriteMeshInstance(SpriteMesh spriteMesh, GameObject gameObject, bool undo = true)
		{
			SpriteMeshInstance spriteMeshInstance = null;
			
			if(spriteMesh && gameObject)
			{
				if(undo)
				{
					spriteMeshInstance = Undo.AddComponent<SpriteMeshInstance>(gameObject);
				}else{
					spriteMeshInstance = gameObject.AddComponent<SpriteMeshInstance>();
				}
				spriteMeshInstance.spriteMesh = spriteMesh;
				spriteMeshInstance.sharedMaterial = defaultMaterial;
				
				SpriteMeshData spriteMeshData = SpriteMeshUtils.LoadSpriteMeshData(spriteMesh);
				
				List<Bone2D> bones = new List<Bone2D>();
				List<string> paths = new List<string>();
				
				Vector4 zero = new Vector4 (0f, 0f, 0f, 1f);
				
				foreach(BindInfo bindInfo in spriteMeshData.bindPoses)
				{
					Matrix4x4 m =  spriteMeshInstance.transform.localToWorldMatrix * bindInfo.bindPose.inverse;
					
					GameObject bone = new GameObject(bindInfo.name);
					
					if(undo)
					{
						Undo.RegisterCreatedObjectUndo(bone,Undo.GetCurrentGroupName());
					}
					
					Bone2D boneComponent = bone.AddComponent<Bone2D>();
					
					boneComponent.localLength = bindInfo.boneLength;
					bone.transform.position = m * zero;
					bone.transform.rotation = m.GetRotation();
					bone.transform.parent = gameObject.transform;
					
					bones.Add(boneComponent);
					paths.Add(bindInfo.path);
				}
				
				BoneUtils.ReconstructHierarchy(bones,paths);
				
				spriteMeshInstance.bones = bones;
				
				SpriteMeshUtils.UpdateRenderer(spriteMeshInstance, undo);
				
				EditorUtility.SetDirty(spriteMeshInstance);
			}
			
			return spriteMeshInstance;
		}
		
		public static bool HasNullBones(SpriteMeshInstance spriteMeshInstance)
		{
			if(spriteMeshInstance)
			{
				return spriteMeshInstance.bones.Contains(null);
			}
			return false;
		}
		
		public static bool CanEnableSkinning(SpriteMeshInstance spriteMeshInstance)
		{
			return spriteMeshInstance.spriteMesh && !HasNullBones(spriteMeshInstance) && spriteMeshInstance.bones.Count > 0 && (spriteMeshInstance.spriteMesh.sharedMesh.bindposes.Length == spriteMeshInstance.bones.Count);
		}
		
		public static void UpdateRenderer(SpriteMeshInstance spriteMeshInstance, bool undo = true)
		{
			if(!spriteMeshInstance)
			{
				return;
			}
			
			SerializedObject spriteMeshInstaceSO = new SerializedObject(spriteMeshInstance);
			
			SpriteMesh spriteMesh = spriteMeshInstaceSO.FindProperty("m_SpriteMesh").objectReferenceValue as SpriteMesh;
			
			if(spriteMesh)
			{
				Mesh sharedMesh = spriteMesh.sharedMesh;
				
				if(sharedMesh.bindposes.Length > 0 && spriteMeshInstance.bones.Count > sharedMesh.bindposes.Length)
				{
					spriteMeshInstance.bones = spriteMeshInstance.bones.GetRange(0,sharedMesh.bindposes.Length);
				}
				
				if(CanEnableSkinning(spriteMeshInstance))
				{
					MeshFilter meshFilter = spriteMeshInstance.cachedMeshFilter;
					MeshRenderer meshRenderer = spriteMeshInstance.cachedRenderer as MeshRenderer;
					
					if(meshFilter)
					{
						if(undo)
						{
							Undo.DestroyObjectImmediate(meshFilter);
						}else{
							GameObject.DestroyImmediate(meshFilter);
						}
					}
					if(meshRenderer)
					{
						if(undo)
						{
							Undo.DestroyObjectImmediate(meshRenderer);
						}else{
							GameObject.DestroyImmediate(meshRenderer);
						}
					}
					
					SkinnedMeshRenderer skinnedMeshRenderer = spriteMeshInstance.cachedSkinnedRenderer;
					
					if(!skinnedMeshRenderer)
					{
						if(undo)
						{
							skinnedMeshRenderer = Undo.AddComponent<SkinnedMeshRenderer>(spriteMeshInstance.gameObject);
						}else{
							skinnedMeshRenderer = spriteMeshInstance.gameObject.AddComponent<SkinnedMeshRenderer>();
						}
					}
					
					skinnedMeshRenderer.bones = spriteMeshInstance.bones.ConvertAll( bone => bone.transform ).ToArray();
					
					if(spriteMeshInstance.bones.Count > 0)
					{
						skinnedMeshRenderer.rootBone = spriteMeshInstance.bones[0].transform;
					}

					EditorUtility.SetDirty(skinnedMeshRenderer);
				}else{
					SkinnedMeshRenderer skinnedMeshRenderer = spriteMeshInstance.cachedSkinnedRenderer;
					MeshFilter meshFilter = spriteMeshInstance.cachedMeshFilter;
					MeshRenderer meshRenderer = spriteMeshInstance.cachedRenderer as MeshRenderer;
					
					if(skinnedMeshRenderer)
					{
						if(undo)
						{
							Undo.DestroyObjectImmediate(skinnedMeshRenderer);
						}else{
							GameObject.DestroyImmediate(skinnedMeshRenderer);
						}
					}
					
					if(!meshFilter)
					{
						if(undo)
						{
							meshFilter = Undo.AddComponent<MeshFilter>(spriteMeshInstance.gameObject);
						}else{
							meshFilter = spriteMeshInstance.gameObject.AddComponent<MeshFilter>();
						}

						EditorUtility.SetDirty(meshFilter);
					}
					
					if(!meshRenderer)
					{
						if(undo)
						{
							meshRenderer = Undo.AddComponent<MeshRenderer>(spriteMeshInstance.gameObject);
						}else{
							meshRenderer = spriteMeshInstance.gameObject.AddComponent<MeshRenderer>();
						}
						
						EditorUtility.SetDirty(meshRenderer);
					}
				}
			}
		}
		
		public static bool NeedsOverride(SpriteMesh spriteMesh)
		{
			if(!spriteMesh || !spriteMesh.sprite) return false;
			
			SpriteMeshData spriteMeshData = LoadSpriteMeshData(spriteMesh);
			
			if(!spriteMeshData) return false;
			
			ushort[] triangles = spriteMesh.sprite.triangles;
			
			if(triangles.Length != spriteMeshData.indices.Length) return true;
			
			for(int i = 0; i < triangles.Length; i++)
			{
				if(spriteMeshData.indices[i] != triangles[i])
				{
					return true;
				}
			}
			
			return false;
		}
			
		public static BlendShape CreateBlendShape(SpriteMesh spriteMesh, string blendshapeName)
		{
			BlendShape l_blendshape = null;

			SpriteMeshData spriteMeshData = LoadSpriteMeshData(spriteMesh);

			if(spriteMeshData)
			{
				l_blendshape = BlendShape.Create(blendshapeName);

				l_blendshape.hideFlags = HideFlags.HideInHierarchy;

				AssetDatabase.AddObjectToAsset(l_blendshape,spriteMeshData);

				List<BlendShape> l_blendshapes = new List<BlendShape>(spriteMeshData.blendshapes);

				l_blendshapes.Add(l_blendshape);

				spriteMeshData.blendshapes = l_blendshapes.ToArray();

				EditorUtility.SetDirty(spriteMeshData);
				EditorUtility.SetDirty(l_blendshape);
			}

			return l_blendshape;
		}

		public static BlendShapeFrame CreateBlendShapeFrame(BlendShape blendshape, float weight, Vector3[] vertices)
		{
			BlendShapeFrame l_blendshapeFrame = null;

			if(blendshape && vertices != null)
			{
				l_blendshapeFrame = BlendShapeFrame.Create(weight,vertices);

				l_blendshapeFrame.hideFlags = HideFlags.HideInHierarchy;

				AssetDatabase.AddObjectToAsset(l_blendshapeFrame,blendshape);

				List<BlendShapeFrame> l_blendshapeFrames = new List<BlendShapeFrame>(blendshape.frames);

				l_blendshapeFrames.Add(l_blendshapeFrame);

				l_blendshapeFrames.Sort( (a,b) => { return a.weight.CompareTo(b.weight); } );

				blendshape.frames = l_blendshapeFrames.ToArray();

				EditorUtility.SetDirty(blendshape);
				EditorUtility.SetDirty(l_blendshapeFrame);
			}

			return l_blendshapeFrame;
		}

		public static void DestroyBlendShapes(SpriteMesh spriteMesh)
		{
			DestroyBlendShapes(spriteMesh, false, "");
		}

		public static void DestroyBlendShapes(SpriteMesh spriteMesh, bool undo, string undoName)
		{
			DestroyBlendShapes(LoadSpriteMeshData(spriteMesh), false, "");
		}

		public static void DestroyBlendShapes(SpriteMeshData spriteMeshData, bool undo, string undoName)
		{
			if(spriteMeshData)
			{
				if(undo && !string.IsNullOrEmpty(undoName))
				{
					Undo.RegisterCompleteObjectUndo(spriteMeshData,undoName);
				}

				foreach(BlendShape blendShape in spriteMeshData.blendshapes)
				{
					foreach(BlendShapeFrame frame in blendShape.frames)
					{
						if(undo)
						{
							Undo.DestroyObjectImmediate(frame);
						}else{
							GameObject.DestroyImmediate(frame,true);
						}
					}

					if(undo)
					{
						Undo.DestroyObjectImmediate(blendShape);
					}else{
						GameObject.DestroyImmediate(blendShape,true);
					}
				}

				spriteMeshData.blendshapes = new BlendShape[0];
			}
		}

		public static void RebuildBlendShapes(SpriteMesh spriteMesh)
		{
			RebuildBlendShapes(spriteMesh,spriteMesh.sharedMesh);
		}

		public static void RebuildBlendShapes(SpriteMesh spriteMesh, Mesh mesh)
		{
			if(!mesh)
				return;

			if(!spriteMesh)
				return;
			
			BlendShape[] blendShapes = null;

			SpriteMeshData spriteMeshData = LoadSpriteMeshData(spriteMesh);

			if(spriteMeshData)
			{
				blendShapes = spriteMeshData.blendshapes;
			}

			if(spriteMesh.sharedMesh.vertexCount != mesh.vertexCount)
			{
				return;
			}

			if(blendShapes != null)
			{
#if !(UNITY_5_0 || UNITY_5_1 || UNITY_5_2)
				List<string> blendShapeNames = new List<string>();

				mesh.ClearBlendShapes();

				Vector3[] from = mesh.vertices;

				for (int i = 0; i < blendShapes.Length; i++)
				{
					BlendShape blendshape = blendShapes[i];

					if(blendshape)
					{
						string blendShapeName = blendshape.name;

						if(blendShapeNames.Contains(blendShapeName))
						{
							Debug.LogWarning("Found repeated BlendShape name '" + blendShapeName + "' in SpriteMesh: " + spriteMesh.name);
						}else{
							blendShapeNames.Add(blendShapeName);

							for(int j = 0; j < blendshape.frames.Length; j++)
							{
								BlendShapeFrame l_blendshapeFrame = blendshape.frames[j];

								if(l_blendshapeFrame && from.Length == l_blendshapeFrame.vertices.Length)
								{
									Vector3[] deltaVertices = GetDeltaVertices(from, l_blendshapeFrame.vertices);

									mesh.AddBlendShapeFrame(blendShapeName, l_blendshapeFrame.weight, deltaVertices, null, null);
								}
							}
						}
					}
				}

				mesh.UploadMeshData(false);

				EditorUtility.SetDirty(mesh);
#endif
			}
		}

		static Vector3[] GetDeltaVertices(Vector3[] from, Vector3[] to)
		{
			Vector3[] result = new Vector3[from.Length];

			for (int i = 0; i < to.Length; i++)
			{
				result[i] = to[i] - from[i];
			}

			return result;
		}
	}
}
