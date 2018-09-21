using UnityEngine;
using UnityEditor;
using UnityEditor.Sprites;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	public class SpriteMeshCache : SerializedCache, IVertexManipulable, IRectManipulable
	{
		public SpriteMesh spriteMesh;
		public SpriteMeshData spriteMeshData;
		public SpriteMeshInstance spriteMeshInstance;

		[SerializeField]
		SpriteMeshEditorWindow.Mode m_Mode;

		public SpriteMeshEditorWindow.Mode mode {
			get {
				return m_Mode;
			}
			set {
				if(m_Mode != value)
				{
					m_Mode = value;
					m_DirtyVertices = true;
				}
			}
		}

		public List<Node> nodes = new List<Node>();

		[SerializeField]
		List<Vector2> m_TexVertices = new List<Vector2>();
		
		public List<Vector2> uvs {
			get {
				int width = 1;
				int height = 1;
				SpriteMeshUtils.GetSpriteTextureSize(spriteMesh.sprite,ref width, ref height);
				Vector2 t = new Vector2(1f/width,1f/height);
				return m_TexVertices.ConvertAll( v => Vector2.Scale(v,t) );
			}
		}
		
		public List<BoneWeight> boneWeights = new List<BoneWeight>();
		
		public List<Edge> edges = new List<Edge>();
		
		List<IndexedEdge> indexedEdges {
			get {
				return edges.ConvertAll( e => new IndexedEdge(nodes.IndexOf(e.node1), nodes.IndexOf(e.node2)) );
			}
		}
		
		public List<Hole> holes = new List<Hole>();
		
		public List<int> indices = new List<int>();
		
		public List<BindInfo> bindPoses = new List<BindInfo>();
		
		public List<BlendShape> blendshapes = new List<BlendShape>();
		
		public Vector2 pivotPoint = Vector2.zero;
		
		public Rect rect;
		
		[SerializeField]
		VertexSelection m_Selection = new VertexSelection();

		public VertexSelection selection {
			get {
				return m_Selection;
			}
		}
			
		public Node selectedNode {
			get {
				if(selection.Count == 1)
				{
					return nodes[selection.First()];
				}

				return null;
			}
		}

		public List<Node> selectedNodes {
			get {
				return nodes.Where( (n) => { return selection.IsSelected(n.index); } ).ToList();
			}
		}

		public bool multiselection { get { return selection.Count > 1; } }
		
		public Edge selectedEdge = null;
		
		public bool isBound { get { return bindPoses.Count > 0f; } }
		public bool isDirty { get; set; }
		
		[SerializeField]
		int mSelectedHoleIndex = -1;
		public Hole selectedHole {
			get {
				Hole hole = null;
				
				if(mSelectedHoleIndex >= 0 && mSelectedHoleIndex < holes.Count)
				{
					hole = holes[mSelectedHoleIndex];
				}
				
				return hole;
			}
			set {
				mSelectedHoleIndex = holes.IndexOf(value);
			}
		}
		
		public Bone2D selectedBone;
		
		[SerializeField]
		int m_SelectedBindPose = -1;
		public BindInfo selectedBindPose {
			get {
				BindInfo bindPose = null;
				
				if(m_SelectedBindPose >= 0 && m_SelectedBindPose < bindPoses.Count)
				{
					bindPose = bindPoses[m_SelectedBindPose];
				}
				
				return bindPose;
			}
			set {
				m_SelectedBindPose = bindPoses.IndexOf(value);
			}
		}

		[SerializeField]
		BlendShape m_SelectedBlendShape;

		public BlendShape selectedBlendshape {
			get {
				return m_SelectedBlendShape;
			}
			set {
				if(m_SelectedBlendShape != value)
				{
					m_DirtyVertices = true;
					m_SelectedBlendShape = value;
				}
			}
		}
			
		public BlendShapeFrame selectedBlendshapeFrame {
			get {
				return GetBlendShapeFrame();
			}
			set {
				if(selectedBlendshape && selectedBlendshape.frames.Contains(value))
				{
					blendShapeWeight = value.weight;
				}
			}
		}

		//[SerializeField]
		float m_BlendShapeWeight = 0f;

		public float blendShapeWeight {
			get {
				return m_BlendShapeWeight;
			}
			set {
				if(m_BlendShapeWeight != value)
				{
					m_DirtyVertices = true;
					m_BlendShapeWeight = Mathf.Clamp(value, 0f, value);
				}
			}
		}

		[SerializeField]
		RectManipulatorParams m_RectManipulatorParams;

		public RectManipulatorParams rectManipulatorParams {
			get {
				return m_RectManipulatorParams;
			}
			set {
				m_RectManipulatorParams = value;
			}
		}

		[SerializeField]
		bool m_DirtyVertices = true;

		[SerializeField]
		List<Vector2> m_CurrentTexVertices = new List<Vector2>();

		void OnEnable()
		{
			m_DirtyVertices = true;
		}

		override public void RegisterUndo(string undoName)
		{
			base.RegisterUndo(undoName);

			RegisterObjectUndo(selectedBlendshapeFrame,undoName);
		}

		public string[] GetBoneNames(string noBoneText)
		{
			List<string> names = new List<string>(bindPoses.Count);
			List<int> repetitions = new List<int>(bindPoses.Count);
			
			names.Add(noBoneText);
			repetitions.Add(0);
			
			foreach(BindInfo bindInfo in bindPoses)
			{
				List<string> repetedNames = names.Where( s => s == bindInfo.name ).ToList();
				
				names.Add(bindInfo.name);
				repetitions.Add(repetedNames.Count);
			}
			
			for (int i = 1; i < names.Count; i++)
			{
				string name = names[i];
				int count = repetitions[i] + 1;
				if(count > 1)
				{
					name += " (" + count.ToString() + ")";
					names[i] = name;
				}
			}
			
			return names.ToArray();
		}
		
		protected override void DoOnAfterDeserialize()
		{
			for(int i = 0; i < nodes.Count; ++i)
			{
				nodes[i].index = i;
			}
		}
		
		public void SetSpriteMesh(SpriteMesh _spriteMesh, SpriteMeshInstance _spriteMeshInstance)
		{
			spriteMesh = _spriteMesh;
			spriteMeshInstance = _spriteMeshInstance;
			spriteMeshData = SpriteMeshUtils.LoadSpriteMeshData(_spriteMesh);
			RevertChanges();
		}
		
		public void ApplyChanges()
		{
			if(spriteMeshData)
			{
				spriteMeshData.vertices = m_TexVertices.ToArray();
				spriteMeshData.boneWeights = boneWeights.ToArray();
				spriteMeshData.edges = indexedEdges.ToArray();
				spriteMeshData.holes = holes.ConvertAll( h => h.vertex ).ToArray();
				spriteMeshData.indices = indices.ToArray();
				spriteMeshData.bindPoses = bindPoses.ToArray();
				spriteMeshData.pivotPoint = pivotPoint;

				SpriteMeshUtils.DestroyBlendShapes(spriteMeshData,false,"");

				SetBlendShapesFromCache();

				EditorUtility.SetDirty(spriteMeshData);

				string spriteAssetPath = AssetDatabase.GetAssetPath(spriteMesh.sprite);
				SpriteMeshUtils.UpdateAssets(spriteMesh,spriteMeshData);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				TextureImporter textureImporter = AssetImporter.GetAtPath(spriteAssetPath) as TextureImporter;
				textureImporter.userData = textureImporter.assetTimeStamp.ToString();
				AssetDatabase.StartAssetEditing();
				AssetDatabase.ImportAsset(spriteAssetPath);
				AssetDatabase.StopAssetEditing();
				isDirty = false;
			}
			
			if(spriteMeshInstance)
			{
				SpriteMeshUtils.UpdateRenderer(spriteMeshInstance,false);
			}
		}
			
		public void RevertChanges()
		{
			Clear("");
			
			if(spriteMesh && spriteMeshData)
			{
				pivotPoint = spriteMeshData.pivotPoint;
				rect = SpriteMeshUtils.GetRect(spriteMesh.sprite);
				
				m_TexVertices = spriteMeshData.vertices.ToList();
				nodes = m_TexVertices.ConvertAll( v => Node.Create(m_TexVertices.IndexOf(v)) );
				boneWeights = spriteMeshData.boneWeights.ToList();
				edges = spriteMeshData.edges.ToList().ConvertAll( e => Edge.Create(nodes[e.index1],nodes[e.index2]) );
				holes = spriteMeshData.holes.ToList().ConvertAll( h => new Hole(h) );
				indices = spriteMeshData.indices.ToList();
				bindPoses = spriteMeshData.bindPoses.ToList().ConvertAll( b => b.Clone() as BindInfo );

				CreateBlendShapeCache(spriteMeshData);

				if(boneWeights.Count != nodes.Count)
				{
					boneWeights = nodes.ConvertAll( n => BoneWeight.Create() );
				}

				m_DirtyVertices = true;
			}
		}
		
		public void Clear(string undoName)
		{
			RegisterUndo(undoName);

			foreach(Edge edge in edges)
			{
				DestroyObjectImmediate(edge);
			}

			foreach(Node node in nodes)
			{
				DestroyObjectImmediate(node);
			}

			DestroyBlendShapeCache(undoName);

			selectedBindPose = null;
			selectedBone = null;
			selectedEdge = null;
			selectedBlendshape = null;
			
			selection.Clear();
			nodes.Clear();
			edges.Clear();
			indices.Clear();
			boneWeights.Clear();

			blendShapeWeight = 0f;

			m_CurrentTexVertices.Clear();

			isDirty = false;
			m_DirtyVertices = false;
		}

		public void Select(Node node, bool append)
		{
			if(!append)
			{
				selection.Clear();
			}

			selection.Select(node.index,true);
		}

		public void BeginSelection()
		{
			selection.BeginSelection();
		}

		public void EndSelection(bool select)
		{
			selection.EndSelection(select);
		}

		public bool IsSelected(Node node)
		{
			return selection.IsSelected(node.index);
		}

		public void Unselect(Node node)
		{
			selection.Select(node.index,false);
		}

		public void ClearSelection()
		{
			selection.Clear();
		}

		public void SetPivotPoint(Vector2 _pivotPoint)
		{
			pivotPoint = _pivotPoint;
			isDirty = true;
		}
		
		public Node AddNode(Vector2 position)
		{
			return AddNode(position,null);
		}
		
		public Node AddNode(Vector2 position, Edge edge)
		{
			Node node = Node.Create(nodes.Count);
			
			nodes.Add(node);
			
			if(edge)
			{
				edges.Add(Edge.Create(edge.node1,node));
				edges.Add(Edge.Create(edge.node2,node));
				edges.Remove(edge);
			}
			
			m_TexVertices.Add(position);
			boneWeights.Add(BoneWeight.Create());

			if(blendshapes.Count > 0)
			{
				List<Vector3> frameVertices = new List<Vector3>(nodes.Count);
				Vector3 vertex = ToVertex(position);

				foreach(BlendShape blendShape in blendshapes)
				{
					foreach(BlendShapeFrame frame in blendShape.frames)
					{
						RegisterObjectUndo(frame, Undo.GetCurrentGroupName());

						frameVertices.Clear();
						frameVertices.AddRange(frame.vertices);
						frameVertices.Add(vertex);
						frame.vertices = frameVertices.ToArray();
					}
				}
			}

			
			Triangulate();

			m_DirtyVertices = true;

			return node;
		}
		
		public void DeleteNode(Node node, bool triangulate = true)
		{
			List<Edge> l_edges = new List<Edge>();
			
			for(int i = 0; i < edges.Count; i++)
			{
				Edge edge = edges[i];
				if(edge.ContainsNode(node))
				{
					l_edges.Add(edge);
				}
			}
			
			if(l_edges.Count == 2)
			{
				Node node1 = l_edges[0].node1 != node ? l_edges[0].node1 : l_edges[0].node2;
				Node node2 = l_edges[1].node1 != node ? l_edges[1].node1 : l_edges[1].node2;
				
				edges.Remove(l_edges[0]);
				edges.Remove(l_edges[1]);
				
				AddEdge(node1,node2);
			}else{
				foreach(Edge edge in l_edges)
				{
					edges.Remove(edge);
				}
			}
			
			m_TexVertices.RemoveAt(node.index);
			boneWeights.RemoveAt(node.index);

			if(blendshapes.Count > 0)
			{
				List<Vector3> frameVertices = new List<Vector3>(nodes.Count);

				foreach(BlendShape blendShape in blendshapes)
				{
					foreach(BlendShapeFrame frame in blendShape.frames)
					{
						frameVertices.Clear();
						frameVertices.AddRange(frame.vertices);
						frameVertices.RemoveAt(node.index);
						frame.vertices = frameVertices.ToArray();
					}
				}
			}

			nodes.Remove(node);
			
			for(int i = 0; i < nodes.Count; ++i)
			{
				nodes[i].index = i;
			}
			
			if(triangulate)
			{
				Triangulate();
			}

			m_DirtyVertices = true;
		}
		
		public void AddEdge(Node node1, Node node2)
		{
			Edge newEdge = Edge.Create(node1,node2);
			
			if(!edges.Contains(newEdge))
			{
				edges.Add(newEdge);
				Triangulate();
			}
		}
		
		public void DeleteEdge(Edge edge)
		{
			if(edges.Contains(edge))
			{
				edges.Remove(edge);
				Triangulate();
			}
		}
		
		public void AddHole(Vector2 position)
		{
			holes.Add(new Hole(position));
			Triangulate();
		}
		
		public void DeleteHole(Hole hole, bool triangulate = true)
		{
			holes.Remove(hole);
			
			if(triangulate)
			{
				Triangulate();
			}
		}

		public BlendShape CreateBlendshape(string name, string undoName = "")
		{
			BlendShape blendShape = BlendShape.Create(name);
			blendShape.hideFlags = HideFlags.DontSave;

			RegisterUndo(undoName);

			RegisterCreatedObjectUndo(blendShape, undoName);

			blendshapes.Add(blendShape);

			m_DirtyVertices = true;

			return blendShape;
		}

		public void DeleteBlendShape(BlendShape blendshape, string undoName = "")
		{
			if(blendshape)
			{
				RegisterUndo(undoName);

				selectedBlendshape = null;
				blendshapes.Remove(blendshape);

				RegisterObjectUndo(blendshape, undoName);

				foreach(BlendShapeFrame frame in blendshape.frames)
				{
					DestroyObjectImmediate(frame);
				}

				DestroyObjectImmediate(blendshape);

				m_DirtyVertices = true;
				isDirty = true;
			}
		}

		public void ResetVertices(List<Node> _nodes, string undoName = "")
		{
			if(selectedBlendshapeFrame)
			{
				RegisterUndo(undoName);

				foreach(Node node in _nodes)
				{
					SetVertex( node, m_TexVertices[node.index] );
				}

				m_DirtyVertices = true;
				isDirty = true;
			}
		}

		void CreateBlendShapeCache(SpriteMeshData spriteMeshData)
		{
			DestroyBlendShapeCache("");

			List<BlendShapeFrame> frameClones = new List<BlendShapeFrame>();

			foreach(BlendShape blendShape in spriteMeshData.blendshapes)
			{
				frameClones.Clear();

				foreach(BlendShapeFrame frame in blendShape.frames)
				{
					BlendShapeFrame frameClone = ScriptableObject.CreateInstance<BlendShapeFrame>();
					frameClone.hideFlags = HideFlags.DontSave;

					EditorUtility.CopySerialized(frame,frameClone);

					frameClones.Add(frameClone);
				}

				BlendShape blendShapeClone = CreateBlendshape(blendShape.name);

				blendShapeClone.frames = frameClones.ToArray();
			}
		}

		void DestroyBlendShapeCache(string undoName)
		{
			foreach(BlendShape blendShape in blendshapes)
			{
				if(blendShape)
				{
					RegisterObjectUndo(blendShape, undoName);

					foreach(BlendShapeFrame frame in blendShape.frames)
					{
						DestroyObjectImmediate(frame);
					}

					DestroyObjectImmediate(blendShape);
				}
			}

			blendshapes.Clear();
		}

		void SetBlendShapesFromCache()
		{
			if(spriteMesh)
			{
				foreach(BlendShape blendshape in blendshapes)
				{
					BlendShape newBlendshape = SpriteMeshUtils.CreateBlendShape(spriteMesh,blendshape.name);

					foreach(BlendShapeFrame frame in blendshape.frames)
					{
						SpriteMeshUtils.CreateBlendShapeFrame(newBlendshape,frame.weight,frame.vertices.ToArray());
					}
				}
			}
		}

		void SortBlendshapeFrames(BlendShape blendShape)
		{
			if(blendShape)
			{
				List<BlendShapeFrame> frames = blendShape.frames.ToList();

				frames.Sort( (a,b) => { return a.weight.CompareTo(b.weight); } );

				blendShape.frames = frames.ToArray();
			}
		}

		public BlendShapeFrame CreateBlendShapeFrame(BlendShape blendshape, float weight, string undoName)
		{
			BlendShapeFrame frame = null;

			if(blendshape && weight >= 1f)
			{
				frame = BlendShapeFrame.Create(weight,ToVertices(GetTexVertices()).ToArray());

				RegisterCreatedObjectUndo(frame, undoName);
				RegisterObjectUndo(blendshape, undoName);

				List<BlendShapeFrame> frames = new List<BlendShapeFrame>(blendshape.frames);

				frames.Add(frame);

				blendshape.frames = frames.ToArray();

				SortBlendshapeFrames(blendshape);

				m_DirtyVertices = true;
				isDirty = true;
			}

			return frame;
		}

		public void DeleteBlendShapeFrame(BlendShape blendShape, BlendShapeFrame blendShapeFrame, string undoName = "")
		{
			if(blendShape && blendShape.frames.Contains(blendShapeFrame))
			{
				RegisterObjectUndo(blendShape,undoName);

				List<BlendShapeFrame> frames = blendShape.frames.ToList();

				frames.Remove(blendShapeFrame);

				blendShape.frames = frames.ToArray();

				DestroyObjectImmediate(blendShapeFrame);

				m_DirtyVertices = true;
				isDirty = true;
			}
		}

		public void SetBlendShapeFrameWeight(BlendShapeFrame blendShapeFrame, float weight, string undoName)
		{
			if(selectedBlendshape &&
				blendShapeFrame &&
				weight >= 1f &&
				selectedBlendshape.frames.Contains(blendShapeFrame))
			{
				RegisterObjectUndo(selectedBlendshape, undoName);
				RegisterObjectUndo(blendShapeFrame, undoName);

				BlendShapeFrame other = null;

				foreach(BlendShapeFrame f in selectedBlendshape.frames)
				{
					if(f != blendShapeFrame && f.weight == weight)
					{
						other = f;
						break;
					}
				}

				if(other)
				{
					DeleteBlendShapeFrame(selectedBlendshape, other, undoName);
				}

				blendShapeFrame.weight = weight;

				SortBlendshapeFrames(selectedBlendshape);

				m_DirtyVertices = true;
				isDirty = true;
			}
		}

		public void Triangulate()
		{
			SpriteMeshUtils.Triangulate(m_TexVertices,indexedEdges,holes,ref indices);
			
			isDirty = true;
		}
		
		public void InitFromOutline(float detail, float alphaTolerance, bool holeDetection, float tessellation, string undoName)
		{
			Clear(undoName);
			
			float pixelsPerUnit = SpriteMeshUtils.GetSpritePixelsPerUnit(spriteMesh.sprite);
			float factor =  pixelsPerUnit / spriteMesh.sprite.pixelsPerUnit;
			Vector2 position = rect.position / factor;
			Vector2 size = rect.size / factor;
			
			Rect l_rect = new Rect(position.x,position.y,size.x,size.y);
			
			Texture2D texture = SpriteUtility.GetSpriteTexture(spriteMesh.sprite,false);
			Rect clampedRect = MathUtils.ClampRect(MathUtils.OrderMinMax(l_rect),new Rect(0f,0f,texture.width,texture.height));
			
			List<Vector2> l_texcoords;
			List<IndexedEdge> l_indexedEdges;
			List<int> l_indices;
			
			SpriteMeshUtils.InitFromOutline(texture,clampedRect,detail,alphaTolerance,holeDetection, out l_texcoords, out l_indexedEdges, out l_indices);
			SpriteMeshUtils.Tessellate(l_texcoords,l_indexedEdges,holes,l_indices,tessellation * 10f);
			
			nodes = l_texcoords.ConvertAll( v => Node.Create(l_texcoords.IndexOf(v)) );
			edges = l_indexedEdges.ConvertAll( e => Edge.Create(nodes[e.index1], nodes[e.index2]) );
			m_TexVertices = l_texcoords.ConvertAll( v => v * factor );
			boneWeights = l_texcoords.ConvertAll( v => BoneWeight.Create() );
			indices = l_indices;
			
			isDirty = true;

			m_DirtyVertices = true;
		}
		
		bool ContainsVector(Vector2 vectorToFind, List<Vector2> list, float epsilon, out int index)
		{
			for (int i = 0; i < list.Count; i++)
			{
				Vector2 v = list [i];
				if ((v - vectorToFind).sqrMagnitude < epsilon)
				{
					index = i;
					return true;
				}
			}
			
			index = -1;
			return false;
		}
		
		public void DeleteBone(Bone2D bone)
		{
			if(spriteMeshInstance && bone)
			{
				List<Bone2D> bones = spriteMeshInstance.bones;
				
				if(bones.Contains(bone))
				{
					bones.Remove(bone);
					spriteMeshInstance.bones = bones;
					EditorUtility.SetDirty(spriteMeshInstance);
				}
			}
		}
		
		public void DeleteBindPose(BindInfo bindPose)
		{
			if(bindPose)
			{
				if(selectedBindPose == bindPose)
				{
					selectedBindPose = null;
				}
				
				int index = bindPoses.IndexOf(bindPose);
				
				Unassign(bindPose);
				bindPoses.Remove(bindPose);
				
				for(int i = 0; i < boneWeights.Count; i++)
				{
					BoneWeight boneWeight = boneWeights[i];
					boneWeight.DeleteBoneIndex(index);
					SetBoneWeight(nodes[i],boneWeight);
				}
				
				isDirty = true;
			}
		}
		
		public void Unassign(BindInfo bindPose)
		{
			Unassign(nodes,bindPose);
		}
		
		public void Unassign(List<Node> targetNodes, BindInfo bindPose)
		{
			if(bindPose)
			{
				foreach(Node node in targetNodes)
				{
					BoneWeight boneWeight = GetBoneWeight(node);
					boneWeight.Unassign(bindPoses.IndexOf(bindPose));
					SetBoneWeight(node,boneWeight);
				}
			}
		}
		
		public void BindBone(Bone2D bone)
		{
			if(spriteMeshInstance && bone)
			{
				BindInfo bindInfo = new BindInfo();
				bindInfo.bindPose = bone.transform.worldToLocalMatrix * spriteMeshInstance.transform.localToWorldMatrix;
				bindInfo.boneLength = bone.localLength;
				bindInfo.path = BoneUtils.GetBonePath (bone);
				bindInfo.name = bone.name;
				bindInfo.color = ColorRing.GetColor(bindPoses.Count);
				
				if(!bindPoses.Contains(bindInfo))
				{
					bindPoses.Add (bindInfo);
					
					isDirty = true;
				}
			}
		}
		
		public void BindBones()
		{
			selectedBone = null;
			
			if(spriteMeshInstance)
			{
				bindPoses.Clear();
				
				foreach(Bone2D bone in spriteMeshInstance.bones)
				{
					BindBone(bone);
				}
			}
		}
		
		public void CalculateAutomaticWeights()
		{
			CalculateAutomaticWeights(nodes);
		}
		
		public void CalculateAutomaticWeights(List<Node> targetNodes)
		{
			float pixelsPerUnit = SpriteMeshUtils.GetSpritePixelsPerUnit(spriteMesh.sprite);
			
			if(nodes.Count <= 0)
			{
				Debug.Log("Cannot calculate automatic weights from a SpriteMesh with no vertices.");
				return;
			}
			
			if(bindPoses.Count <= 0)
			{
				Debug.Log("Cannot calculate automatic weights. Specify bones to the SpriteMeshInstance.");
				return;
			}
			
			if(!spriteMesh)
				return;

			List<Vector2> controlPoints = new List<Vector2>();
			List<IndexedEdge> controlPointEdges = new List<IndexedEdge>();
			List<int> pins = new List<int>();
			
			foreach(BindInfo bindInfo in bindPoses)
			{
				Vector2 tip = SpriteMeshUtils.VertexToTexCoord(spriteMesh,pivotPoint,bindInfo.position,pixelsPerUnit);
				Vector2 tail = SpriteMeshUtils.VertexToTexCoord(spriteMesh,pivotPoint,bindInfo.endPoint,pixelsPerUnit);
				
				if(bindInfo.boneLength <= 0f)
				{
					int index = controlPoints.Count;
					controlPoints.Add(tip);
					pins.Add(index);

					continue;
				}

				int index1 = -1;
				
				if(!ContainsVector(tip,controlPoints,0.01f, out index1))
				{
					index1 = controlPoints.Count;
					controlPoints.Add(tip);
				}
				
				int index2 = -1;
				
				if(!ContainsVector(tail,controlPoints,0.01f, out index2))
				{
					index2 = controlPoints.Count;
					controlPoints.Add(tail);
				}
				
				IndexedEdge edge = new IndexedEdge(index1, index2);
				controlPointEdges.Add(edge);
				
			}
			
			UnityEngine.BoneWeight[] boneWeights = BbwPlugin.CalculateBbw(m_TexVertices.ToArray(), indexedEdges.ToArray(), controlPoints.ToArray(), controlPointEdges.ToArray(), pins.ToArray());
			
			foreach(Node node in targetNodes)
			{
				UnityEngine.BoneWeight unityBoneWeight = boneWeights[node.index];
				
				SetBoneWeight(node,CreateBoneWeightFromUnityBoneWeight(unityBoneWeight));
			}
			
			isDirty = true;
		}

		BoneWeight CreateBoneWeightFromUnityBoneWeight(UnityEngine.BoneWeight unityBoneWeight)
		{
			BoneWeight boneWeight = new BoneWeight();
			
			boneWeight.boneIndex0 = unityBoneWeight.boneIndex0;
			boneWeight.boneIndex1 = unityBoneWeight.boneIndex1;
			boneWeight.boneIndex2 = unityBoneWeight.boneIndex2;
			boneWeight.boneIndex3 = unityBoneWeight.boneIndex3;
			boneWeight.weight0 = unityBoneWeight.weight0;
			boneWeight.weight1 = unityBoneWeight.weight1;
			boneWeight.weight2 = unityBoneWeight.weight2;
			boneWeight.weight3 = unityBoneWeight.weight3;
			
			return boneWeight;
		}
		
		void FillBoneWeights(List<Node> targetNodes, float[,] weights)
		{
			List<float> l_weights = new List<float>();
			
			foreach(Node node in targetNodes)
			{
				l_weights.Clear();
				
				for(int i = 0; i < bindPoses.Count; ++i)
				{
					l_weights.Add(weights[i,node.index]);
				}
				
				SetBoneWeight(node,CreateBoneWeightFromWeights(l_weights));
			}
		}
		
		BoneWeight CreateBoneWeightFromWeights(List<float> weights)
		{
			BoneWeight boneWeight = new BoneWeight();
			
			float weight = 0f;
			int index = -1;
			
			weight = weights.Max();
			if(weight < 0.01f) weight = 0f;
			index = weight > 0f? weights.IndexOf(weight) : -1;
			
			boneWeight.weight0 = weight;
			boneWeight.boneIndex0 = index;
			
			if(index >= 0) weights[index] = 0f;
			
			weight = weights.Max();
			if(weight < 0.01f) weight = 0f;
			index = weight > 0f? weights.IndexOf(weight) : -1;
			
			boneWeight.weight1 = weight;
			boneWeight.boneIndex1 = index;
			
			if(index >= 0) weights[index] = 0f;
			
			weight = weights.Max();
			if(weight < 0.01f) weight = 0f;
			index = weight > 0f? weights.IndexOf(weight) : -1;
			
			boneWeight.weight2 = weight;
			boneWeight.boneIndex2 = index;
			
			if(index >= 0) weights[index] = 0f;
			
			weight = weights.Max();
			if(weight < 0.01f) weight = 0f;
			index = weight > 0f? weights.IndexOf(weight) : -1;
			
			boneWeight.weight3 = weight;
			boneWeight.boneIndex3 = index;
			
			float sum = boneWeight.weight0 + 
				boneWeight.weight1 +
					boneWeight.weight2 +
					boneWeight.weight3;
			
			if(sum > 0f)
			{
				boneWeight.weight0 /= sum;
				boneWeight.weight1 /= sum;
				boneWeight.weight2 /= sum;
				boneWeight.weight3 /= sum;
			}
			
			return boneWeight;
		}
		
		public void SmoothWeights(List<Node> targetNodes)
		{
			float[,] weights = new float[nodes.Count,bindPoses.Count];
			Array.Clear(weights,0,weights.Length);
			
			List<int> usedIndices = new List<int>();
			
			for (int i = 0; i < nodes.Count; i++)
			{
				usedIndices.Clear();
				
				BoneWeight weight = boneWeights[i];
				
				if(weight.boneIndex0 >= 0)
				{
					weights[i,weight.boneIndex0] = weight.weight0;
					usedIndices.Add(weight.boneIndex0);
				}
				if(weight.boneIndex1 >= 0 && !usedIndices.Contains(weight.boneIndex1))
				{
					weights[i,weight.boneIndex1] = weight.weight1;
					usedIndices.Add(weight.boneIndex1);
				}
				if(weight.boneIndex2 >= 0 && !usedIndices.Contains(weight.boneIndex2))
				{
					weights[i,weight.boneIndex2] = weight.weight2;
					usedIndices.Add(weight.boneIndex2);
				}
				if(weight.boneIndex3 >= 0 && !usedIndices.Contains(weight.boneIndex3))
				{
					weights[i,weight.boneIndex3] = weight.weight3;
					usedIndices.Add(weight.boneIndex3);
				}
			}
			
			float[] denominator = new float[nodes.Count];
			float[,] smoothedWeights = new float[nodes.Count,bindPoses.Count]; 
			Array.Clear(smoothedWeights,0,smoothedWeights.Length);
			
			for (int i = 0; i < indices.Count / 3; ++i)
			{
				for (int j = 0; j < 3; ++j)
				{
					int j1 = (j + 1) % 3;
					int j2 = (j + 2) % 3;
					
					for(int k = 0; k < bindPoses.Count; ++k)
					{
						smoothedWeights[indices[i*3 + j],k] += weights[indices[i*3 + j1],k] + weights[indices[i*3 + j2],k]; 
					}
					
					denominator[indices[i*3 + j]] += 2;
				}
			}
			
			for (int i = 0; i < nodes.Count; ++i)
			{
				for (int j = 0; j < bindPoses.Count; ++j)
				{
					smoothedWeights[i,j] /= denominator[i];
				}
			}
			
			float[,] smoothedWeightsTransposed = new float[bindPoses.Count,nodes.Count]; 
			
			for (int i = 0; i < nodes.Count; ++i)
			{
				for (int j = 0; j < bindPoses.Count; ++j)
				{
					smoothedWeightsTransposed[j,i] = smoothedWeights[i,j];
				}
			}
			
			FillBoneWeights(targetNodes, smoothedWeightsTransposed);
			
			isDirty = true;
		}
		
		public void ClearWeights()
		{
			bindPoses.Clear();
			
			isDirty = true;
		}

		public List<Vector2> GetTexVertices()
		{
			UpdateVertices();
			
			return m_CurrentTexVertices;
		}

		public List<Vector3> GetTexVerticesV3()
		{
			return ToVector3List(GetTexVertices());
		}

		List<Vector3> ToVector3List(List<Vector2> list)
		{
			return list.ConvertAll( v => (Vector3)v );
		}

		List<Vector3> ToVertices(List<Vector2> list)
		{
			float pixelsPerUnit = SpriteMeshUtils.GetSpritePixelsPerUnit(spriteMesh.sprite);

			return list.ConvertAll( v => ToVertex(v,pixelsPerUnit));
		}

		Vector3 ToVertex(Vector2 v)
		{
			float pixelsPerUnit = SpriteMeshUtils.GetSpritePixelsPerUnit(spriteMesh.sprite);

			return ToVertex(v,pixelsPerUnit);
		}

		Vector3 ToVertex(Vector2 v, float pixelsPerUnit)
		{
			return SpriteMeshUtils.TexCoordToVertex(pivotPoint,v,pixelsPerUnit);
		}

		void UpdateVertices()
		{
			if(m_DirtyVertices)
			{
				if(mode == SpriteMeshEditorWindow.Mode.Blendshapes && selectedBlendshape)
				{
					m_CurrentTexVertices = GetBlendshapePositions(selectedBlendshape, blendShapeWeight);
				}else{
					m_CurrentTexVertices = m_TexVertices.ToList();
				}
				m_DirtyVertices = false;
			}

		}
			
		List<Vector2> GetBlendshapePositions(BlendShape blendshape, float weight)
		{
			weight = Mathf.Clamp(weight, 0f, weight);

			List<Vector2> result = new List<Vector2>(m_TexVertices.Count);

			if(blendshape)
			{
				BlendShapeFrame prevFrame = null;
				BlendShapeFrame nextFrame = null;

				foreach(BlendShapeFrame frame in blendshape.frames)
				{
					if(frame && frame.weight < weight)
					{
						prevFrame = frame;
					}else if(frame && nextFrame == null)
					{
						nextFrame = frame;

						break;
					}
				}

				Vector3[] prevFrameVertices = null;
				Vector3[] nextFrameVertices = null;

				float prevWeight = 0f;
				float nextWeight = 0f;

				if(prevFrame)
				{
					prevFrameVertices = prevFrame.vertices;
					prevWeight = prevFrame.weight;

				}else{
					prevFrameVertices = ToVertices(m_TexVertices).ToArray();
				}

				if(nextFrame)
				{
					nextFrameVertices = nextFrame.vertices;
					nextWeight = nextFrame.weight;
				}else if(prevFrameVertices != null)
				{
					nextFrameVertices = prevFrameVertices;
					nextWeight = prevWeight;
				}

				if(prevFrameVertices != null &&
					nextFrameVertices != null &&
					prevFrameVertices.Length == nextFrameVertices.Length)
				{
					int count = prevFrameVertices.Length;
					float pixelsPerUnit = SpriteMeshUtils.GetSpritePixelsPerUnit(spriteMesh.sprite);

					float t = 0f;

					float weightDelta = (nextWeight - prevWeight);

					if(weightDelta > 0f)
					{
						t = (weight - prevWeight) / weightDelta;
					}
						
					for(int i = 0; i < count; ++i)
					{
						Vector3 v = Vector3.Lerp(prevFrameVertices[i],nextFrameVertices[i],t);

						result.Add(SpriteMeshUtils.VertexToTexCoord(spriteMesh,pivotPoint,v,pixelsPerUnit));
					}
				}
			}

			return result;
		}

		BlendShapeFrame GetBlendShapeFrame()
		{
			BlendShapeFrame frame = null;

			if(selectedBlendshape)
			{
				foreach(BlendShapeFrame f in selectedBlendshape.frames)
				{
					if(f && Mathf.Approximately(f.weight, blendShapeWeight))
					{
						frame = f;
						break;
					}
				}	
			}

			return frame;
		}

		public Vector2 GetVertex(Node node)
		{
			UpdateVertices();

			if(node)
			{
				return m_CurrentTexVertices[node.index];
			}

			return Vector2.zero;
		}

		public void SetVertex(Node node, Vector2 position)
		{
			SetVertex(node.index,position);
		}

		public void SetVertex(int index, Vector2 position)
		{
			if(mode == SpriteMeshEditorWindow.Mode.Mesh)
			{
				m_TexVertices[index] = position;

				m_DirtyVertices = true;

			}else if(mode == SpriteMeshEditorWindow.Mode.Blendshapes)
			{
				if (!selectedBlendshapeFrame && selectedBlendshape)
				{
					CreateBlendShapeFrame (selectedBlendshape, blendShapeWeight, Undo.GetCurrentGroupName ());
				}

				if (selectedBlendshapeFrame)
				{
					selectedBlendshapeFrame.vertices [index] = ToVertex (position);
					m_DirtyVertices = true;
				}
			}

			isDirty = true;
		}
		
		public BoneWeight GetBoneWeight(Node node)
		{
			return boneWeights[node.index];
		}
		
		public void SetBoneWeight(Node node, BoneWeight boneWeight)
		{
			boneWeights[node.index] = boneWeight;
			isDirty = true;
		}

		public void PrepareManipulableVertices()
		{
			m_ManipulableNodes = selectedNodes;
		}

#region IVertexManipulable
		List<Node> m_ManipulableNodes = new List<Node>();

		public int GetManipulableVertexCount()
		{
			return m_ManipulableNodes.Count;
		}

		public Vector3 GetManipulableVertex(int index)
		{
			return GetVertex(m_ManipulableNodes[index]);
		}

		public void SetManipulatedVertex(int index, Vector3 vertex)
		{
			SetVertex(m_ManipulableNodes[index], vertex);
		}
#endregion

#region IRectManipulatorData
		RectManipulatorData m_RectManipulatorData = new RectManipulatorData();

		public IRectManipulatorData rectManipulatorData {
			get { return m_RectManipulatorData; }
		}
#endregion
	}
}
