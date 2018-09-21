using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Anima2D 
{
	public class InspectorEditor : WindowEditorTool
	{
		public SpriteMeshCache spriteMeshCache;

		protected override string GetHeader() { return "Inspector"; }

		public InspectorEditor()
		{
			windowRect = new Rect(0f, 0f, 250f, 75);
		}

		Vector2 GetWindowSize()
		{
			Vector2 size = Vector2.one;

			if(spriteMeshCache.mode == SpriteMeshEditorWindow.Mode.Mesh)
			{
				if(spriteMeshCache.isBound && spriteMeshCache.selection.Count > 0)
				{
					size = new Vector2(250f,95f);
				}else if(spriteMeshCache.selectedBindPose)
				{
					size = new Vector2(175f,75f);
				}else{
					size = new Vector2(175f,75f);
				}
			}else if(spriteMeshCache.mode == SpriteMeshEditorWindow.Mode.Blendshapes)
			{
				if(spriteMeshCache.selectedBlendshape)
				{
					size = new Vector2(200f,45f);
				}
			}

			return size;
		}

		public override void OnWindowGUI(Rect viewRect)
		{
			windowRect.size = GetWindowSize();
			windowRect.position = new Vector2(5f, viewRect.height - windowRect.height - 5f);

			base.OnWindowGUI(viewRect);
		}

		protected override void DoWindow(int windowId)
		{
			EditorGUILayout.BeginVertical();

			if(spriteMeshCache.mode == SpriteMeshEditorWindow.Mode.Mesh)
			{
				if(spriteMeshCache.isBound && spriteMeshCache.selection.Count > 0)
				{
					DoVerticesInspector();
				}else if(spriteMeshCache.selectedBindPose)
				{
					DoBindPoseInspector();
				}else{
					DoSpriteMeshInspector();
				}
			}else if(spriteMeshCache.mode == SpriteMeshEditorWindow.Mode.Blendshapes)
			{
				if(spriteMeshCache.selectedBlendshape)
				{
					DoBlendshapeInspector();
				}
			}
			
			EditorGUILayout.EndVertical();
			
			EditorGUIUtility.labelWidth = -1;
		}

		void DoSpriteMeshInspector()
		{
			if(spriteMeshCache.spriteMesh)
			{
				EditorGUI.BeginDisabledGroup(true);
				
				EditorGUIUtility.labelWidth = 55f;
				
				EditorGUILayout.ObjectField("Sprite",spriteMeshCache.spriteMesh.sprite,typeof(UnityEngine.Object),false);
				
				EditorGUI.EndDisabledGroup();

				EditorGUIUtility.labelWidth = 15f;
				
				EditorGUI.BeginChangeCheck();
				
				Vector2 pivotPoint = EditorGUILayout.Vector2Field("Pivot",spriteMeshCache.pivotPoint);
				
				if(EditorGUI.EndChangeCheck())
				{
					spriteMeshCache.RegisterUndo("set pivot");
					
					spriteMeshCache.SetPivotPoint(pivotPoint);
				}
			}
		}

		void DoBindPoseInspector()
		{
			EditorGUIUtility.labelWidth = 55f;
			EditorGUIUtility.fieldWidth = 55f;

			EditorGUI.BeginChangeCheck();
			
			string name = EditorGUILayout.TextField("Name",spriteMeshCache.selectedBindPose.name);
			
			if(EditorGUI.EndChangeCheck())
			{
				if(string.IsNullOrEmpty(name))
				{
					name = "New bone";
				}

				spriteMeshCache.selectedBindPose.name = name;

				spriteMeshCache.RegisterUndo("set name");

				if(!string.IsNullOrEmpty(spriteMeshCache.selectedBindPose.path))
				{
					int index = spriteMeshCache.selectedBindPose.path.LastIndexOf("/");

					if(index < 0)
					{
						index = 0;
					}else{
						index++;
					}

					foreach(BindInfo bindInfo in spriteMeshCache.bindPoses)
					{
						if(!string.IsNullOrEmpty(bindInfo.path) && index < bindInfo.path.Length)
						{
							string pathPrefix = bindInfo.path;
							string pathSuffix = "";

							if(bindInfo.path.Contains('/'))
							{
								pathPrefix = bindInfo.path.Substring(0,index);

								string tail = bindInfo.path.Substring(index);

								int index2 = tail.IndexOf("/");

								if(index2 > 0)
								{
									pathSuffix = tail.Substring(index2);
								}
								bindInfo.path = pathPrefix + name + pathSuffix;
							}else{
								bindInfo.path = bindInfo.name;
							}
						}
					}
				}



				spriteMeshCache.isDirty = true;
			}

			EditorGUI.BeginChangeCheck();
			
			int zOrder = EditorGUILayout.IntField("Z-Order",spriteMeshCache.selectedBindPose.zOrder);
			
			if(EditorGUI.EndChangeCheck())
			{
				spriteMeshCache.RegisterUndo("set z-order");
				spriteMeshCache.selectedBindPose.zOrder = zOrder;
				spriteMeshCache.isDirty = true;
			}

			EditorGUI.BeginChangeCheck();
			
			Color color = EditorGUILayout.ColorField("Color",spriteMeshCache.selectedBindPose.color);
			
			if(EditorGUI.EndChangeCheck())
			{
				spriteMeshCache.RegisterUndo("set color");
				spriteMeshCache.selectedBindPose.color = color;
				spriteMeshCache.isDirty = true;
			}
		}

		bool IsMixedBoneIndex(int weightIndex, out int boneIndex)
		{
			boneIndex = -1;
			float weight = 0f;

			spriteMeshCache.GetBoneWeight(spriteMeshCache.nodes[spriteMeshCache.selection.First()]).GetWeight(weightIndex, out boneIndex, out weight);

			List<Node> selectedNodes = spriteMeshCache.selectedNodes;

			foreach(Node node in selectedNodes)
			{
				int l_boneIndex = -1;
				spriteMeshCache.GetBoneWeight(node).GetWeight(weightIndex, out l_boneIndex, out weight);

				if(l_boneIndex != boneIndex)
				{
					return true;
				}
			}

			return false;
		}

		void DoVerticesInspector()
		{
			if(spriteMeshCache.selection.Count > 0)
			{
				string[] names = spriteMeshCache.GetBoneNames("Unassigned");

				BoneWeight boneWeight = BoneWeight.Create();

				EditorGUI.BeginChangeCheck();

				bool mixedBoneIndex0 = false;
				bool mixedBoneIndex1 = false;
				bool mixedBoneIndex2 = false;
				bool mixedBoneIndex3 = false;
				bool changedIndex0 = false;
				bool changedIndex1 = false;
				bool changedIndex2 = false;
				bool changedIndex3 = false;
				bool mixedWeight = false;

				if(spriteMeshCache.multiselection)
				{
					mixedWeight = true;

					int boneIndex = -1;
					mixedBoneIndex0 = IsMixedBoneIndex(0,out boneIndex);
					if(!mixedBoneIndex0) boneWeight.boneIndex0 = boneIndex;
					mixedBoneIndex1 = IsMixedBoneIndex(1,out boneIndex);
					if(!mixedBoneIndex1) boneWeight.boneIndex1 = boneIndex;
					mixedBoneIndex2 = IsMixedBoneIndex(2,out boneIndex);
					if(!mixedBoneIndex2) boneWeight.boneIndex2 = boneIndex;
					mixedBoneIndex3 = IsMixedBoneIndex(3,out boneIndex);
					if(!mixedBoneIndex3) boneWeight.boneIndex3 = boneIndex;

				}else{
					boneWeight = spriteMeshCache.GetBoneWeight(spriteMeshCache.selectedNode);
				}

				EditorGUI.BeginChangeCheck();

				EditorGUI.BeginChangeCheck();
				boneWeight = EditorGUIExtra.Weight(boneWeight,0,names,mixedBoneIndex0,mixedWeight);
				changedIndex0 = EditorGUI.EndChangeCheck();

				EditorGUI.BeginChangeCheck();
				boneWeight = EditorGUIExtra.Weight(boneWeight,1,names,mixedBoneIndex1,mixedWeight);
				changedIndex1 = EditorGUI.EndChangeCheck();

				EditorGUI.BeginChangeCheck();
				boneWeight = EditorGUIExtra.Weight(boneWeight,2,names,mixedBoneIndex2,mixedWeight);
				changedIndex2 = EditorGUI.EndChangeCheck();

				EditorGUI.BeginChangeCheck();
				boneWeight = EditorGUIExtra.Weight(boneWeight,3,names,mixedBoneIndex3,mixedWeight);
				changedIndex3 = EditorGUI.EndChangeCheck();

				if(EditorGUI.EndChangeCheck())
				{
					spriteMeshCache.RegisterUndo("modify weights");

					if(spriteMeshCache.multiselection)
					{
						List<Node> selectedNodes = spriteMeshCache.selectedNodes;

						foreach(Node node in selectedNodes)
						{
							BoneWeight l_boneWeight = spriteMeshCache.GetBoneWeight(node);
							
							if(!mixedBoneIndex0 || changedIndex0) l_boneWeight.SetWeight(0,boneWeight.boneIndex0,l_boneWeight.weight0);
							if(!mixedBoneIndex1 || changedIndex1) l_boneWeight.SetWeight(1,boneWeight.boneIndex1,l_boneWeight.weight1);
							if(!mixedBoneIndex2 || changedIndex2) l_boneWeight.SetWeight(2,boneWeight.boneIndex2,l_boneWeight.weight2);
							if(!mixedBoneIndex3 || changedIndex3) l_boneWeight.SetWeight(3,boneWeight.boneIndex3,l_boneWeight.weight3);

							spriteMeshCache.SetBoneWeight(node,l_boneWeight);
						}
					}else{
						spriteMeshCache.SetBoneWeight(spriteMeshCache.selectedNode,boneWeight);
					}
				}

				EditorGUI.showMixedValue = false;
			}
		}

		void DoBlendshapeInspector()
		{
			EditorGUIUtility.labelWidth = 65f;
			EditorGUIUtility.fieldWidth = 55f;

			string name = spriteMeshCache.selectedBlendshape.name;

			EditorGUI.BeginChangeCheck();

			name = EditorGUILayout.TextField("Name",name);

			if(EditorGUI.EndChangeCheck())
			{
				spriteMeshCache.RegisterUndo("change name");
				spriteMeshCache.selectedBlendshape.name = name;
			}
		}
	}
}
