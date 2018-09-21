using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Anima2D 
{
	public class WeightEditor : WindowEditorTool
	{
		public SpriteMeshCache spriteMeshCache;

		public bool showPie { get; set; }
		public bool overlayColors { get; set; }

		float m_Weight = 0f;

		List<BoneWeight> m_TempWeights = new List<BoneWeight>();

		protected override string GetHeader() { return "Weight tool"; }
		
		public WeightEditor()
		{
			windowRect = new Rect(0f, 0f, 250f, 82f);
		}

		public override void OnWindowGUI(Rect viewRect)
		{
			windowRect.position = new Vector2(viewRect.width - windowRect.width - 5f,
			                                  viewRect.height - windowRect.height - 5f);

			base.OnWindowGUI(viewRect);
		}

		protected override void DoWindow(int windowId)
		{
			float labelWidth = EditorGUIUtility.labelWidth;
			bool wideMode = EditorGUIUtility.wideMode;

			EditorGUIUtility.wideMode = true;

			EditorGUILayout.BeginHorizontal();

			string[] names = spriteMeshCache.GetBoneNames("None");
			int index = spriteMeshCache.bindPoses.IndexOf(spriteMeshCache.selectedBindPose);
			
			index = EditorGUILayout.Popup(index + 1,names,GUILayout.Width(75f)) - 1;

			if(index >= 0 && index < spriteMeshCache.bindPoses.Count)
			{
				spriteMeshCache.selectedBindPose = spriteMeshCache.bindPoses[index];
			}else{
				spriteMeshCache.selectedBindPose = null;
			}

			EditorGUI.BeginChangeCheck();

			EditorGUI.BeginDisabledGroup(spriteMeshCache.selectedBindPose == null);

			if(Event.current.type == EventType.MouseUp ||
			   Event.current.type == EventType.MouseDown)
			{
				m_Weight = 0f;

				m_TempWeights.Clear();

				if(spriteMeshCache.selection.Count == 0)
				{
					m_TempWeights = spriteMeshCache.boneWeights.ToList();
				}else{
					m_TempWeights = spriteMeshCache.selectedNodes.ConvertAll( n => spriteMeshCache.GetBoneWeight(n) );
				}
			}

			EditorGUIUtility.fieldWidth = 35f;

			m_Weight = EditorGUILayout.Slider(m_Weight,-1f,1f);

			EditorGUIUtility.fieldWidth = 0f;

			if(EditorGUI.EndChangeCheck())
			{
				spriteMeshCache.RegisterUndo("modify weights");

				List<Node> nodes = null;

				if(spriteMeshCache.selection.Count == 0)
				{
					nodes = spriteMeshCache.nodes;
				}else{
					nodes = spriteMeshCache.selectedNodes;
				}

				for (int i = 0; i < nodes.Count; i++)
				{
					Node node = nodes[i];
					BoneWeight tempWeight = m_TempWeights[i];
					tempWeight.SetBoneIndexWeight(index, tempWeight.GetBoneWeight(index) + m_Weight, !EditorGUI.actionKey, true);
					spriteMeshCache.SetBoneWeight(node, tempWeight);
				}
			}

			EditorGUI.EndDisabledGroup();

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if(GUILayout.Button(new GUIContent("Smooth", "Smooth weights")))
			{
				spriteMeshCache.RegisterUndo("smooth weights");

				List<Node> targetNodes = spriteMeshCache.nodes;

				if(spriteMeshCache.selection.Count > 0)
				{
					targetNodes = spriteMeshCache.selectedNodes;
				}

				spriteMeshCache.SmoothWeights(targetNodes);
			}

			if(GUILayout.Button(new GUIContent("Auto", "Calculate automatic weights")))
			{
				spriteMeshCache.RegisterUndo("calculate weights");

				List<Node> targetNodes = spriteMeshCache.nodes;
				
				if(spriteMeshCache.selection.Count > 0)
				{
					targetNodes = spriteMeshCache.selectedNodes;
				}

				spriteMeshCache.CalculateAutomaticWeights(targetNodes);
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			
			EditorGUIUtility.labelWidth = 50f;
			
			overlayColors = EditorGUILayout.Toggle("Overlay", overlayColors);

			EditorGUIUtility.labelWidth = 30f;

			showPie = EditorGUILayout.Toggle("Pies", showPie);
			
			GUILayout.FlexibleSpace();
			EditorGUILayout.EndHorizontal();
			
			EditorGUIUtility.labelWidth = labelWidth;
			EditorGUIUtility.wideMode = wideMode;
		}
	}
}
