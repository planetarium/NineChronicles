using UnityEngine;
using UnityEditor;
using UnityEditor.Sprites;
using UnityEditorInternal;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Anima2D 
{
	public class SliceEditor : WindowEditorTool
	{
		public SpriteMeshCache spriteMeshCache;

		protected override string GetHeader() { return "Slice tool"; }

		public float alpha { get; set; }
		public float detail { get; set; }
		public float tessellation { get; set; }
		public bool holes { get; set; }

		public SliceEditor()
		{
			windowRect = new Rect(0f, 0f, 225f, 35);

			alpha = 0.05f;
			detail = 0.25f;
			holes = true;
		}

		public override void OnWindowGUI(Rect viewRect)
		{
			windowRect.position = new Vector2(5f, 30f);

			base.OnWindowGUI(viewRect);
		}

		protected override void DoWindow(int windowId)
		{
			EditorGUIUtility.labelWidth = 85f;
			EditorGUIUtility.fieldWidth = 32f;

			detail = EditorGUILayout.Slider("Outline detail",detail,0f,1f);
			alpha = EditorGUILayout.Slider("Alpha cutout",alpha,0f,1f);
			tessellation = EditorGUILayout.Slider("Tessellation",tessellation,0f,1f);
			holes = EditorGUILayout.Toggle("Detect holes",holes);

			EditorGUILayout.BeginHorizontal();

			GUILayout.FlexibleSpace();

			if(GUILayout.Button("Apply", GUILayout.Width(70f)))
			{
				if(spriteMeshCache)
				{
					spriteMeshCache.InitFromOutline(detail,alpha,holes,tessellation,"set outline");
				}
			}

			GUILayout.FlexibleSpace();

			EditorGUILayout.EndHorizontal();
		}

		protected override void DoGUI()
		{
			if(canShow())
			{
				Vector3 pos = Vector3.zero;
				Quaternion rot = Quaternion.identity;
				Rect rect = spriteMeshCache.rect;

				EditorGUI.BeginChangeCheck();

				RectHandles.Do(ref rect, ref pos, ref rot,false);

				if(EditorGUI.EndChangeCheck())
				{
					spriteMeshCache.RegisterUndo("set rect");

					spriteMeshCache.rect = rect;
				}
			}
		}
	}
}
