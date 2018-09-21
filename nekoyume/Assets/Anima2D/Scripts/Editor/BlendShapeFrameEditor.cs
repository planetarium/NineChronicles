using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	public class BlendShapeFrameEditor : WindowEditorTool
	{
		public SpriteMeshCache spriteMeshCache;

		Timeline m_TimeLine;

		protected override string GetHeader() { return "Frames"; }

		public BlendShapeFrameEditor()
		{
			windowRect.size = new Vector2(200f, EditorGUIUtility.singleLineHeight * 2f);

			Undo.undoRedoPerformed += UndoRedoPerformed;
			BlendShapeFrameDopeElement.onFrameChanged += OnFrameChanged;
		}

		public override void OnWindowGUI(Rect viewRect)
		{
			float xPos = Mathf.Max(200f + 5f + 5f, viewRect.width - 400f);

			windowRect.position = new Vector2(xPos, viewRect.height - windowRect.height - 5f);
			windowRect.size = new Vector2(viewRect.width - xPos - 5f, windowRect.size.y);

			base.OnWindowGUI(viewRect);
		}

		void UndoRedoPerformed()
		{
			if(spriteMeshCache && m_TimeLine != null)
			{
				m_TimeLine.Time = spriteMeshCache.blendShapeWeight;
			}
		}

		protected override void DoWindow(int windowId)
		{
			if(m_TimeLine == null)
			{
				m_TimeLine = new Timeline();
			}

			EditorGUILayout.BeginVertical();

			Rect rect = GUILayoutUtility.GetRect(10f, 32f);

			EditorGUILayout.EndVertical();

			EditorGUI.BeginChangeCheck();

			if(windowRect.width > 32f)
			{
				List<IDopeElement> l_DopeElements = spriteMeshCache.selectedBlendshape.frames.ToList ().ConvertAll( f => (IDopeElement)BlendShapeFrameDopeElement.Create(f)  );

				m_TimeLine.dopeElements = l_DopeElements;
				m_TimeLine.FrameRate = 1f;
				m_TimeLine.Time = spriteMeshCache.blendShapeWeight;
				m_TimeLine.DoTimeline(rect);
			}

			if(EditorGUI.EndChangeCheck())
			{
				spriteMeshCache.blendShapeWeight = Mathf.Clamp(m_TimeLine.Time,0f,100f);
			}
		}

		void OnFrameChanged(BlendShapeFrame blendShapeFrame, float weight)
		{
			spriteMeshCache.SetBlendShapeFrameWeight(blendShapeFrame, weight, "Set weight");
		}
	}
}
