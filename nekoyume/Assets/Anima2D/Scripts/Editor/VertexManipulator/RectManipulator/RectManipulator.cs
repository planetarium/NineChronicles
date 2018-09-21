using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D
{
	[Serializable]
	public class RectManipulator : VertexManipulator
	{
		public RectManipulatorParams rectManipulatorParams;

		public override void DoManipulate()
		{
			Rect rect = GetRect(rectManipulatorParams.position, rectManipulatorParams.rotation);

			int vertexCount = 0;

			foreach(IVertexManipulable vm in manipulables)
			{
				vertexCount += vm.GetManipulableVertexCount();
			}

			if(Event.current.type == EventType.MouseDown &&
				Event.current.button == 0)
			{
				foreach(IVertexManipulable vm in manipulables)
				{
					Normalize(vm,rect,rectManipulatorParams.position,rectManipulatorParams.rotation);
				}
			}

			if(!EditorGUI.actionKey && vertexCount > 2)
			{
				EditorGUI.BeginChangeCheck();

				RectHandles.Do(ref rect,ref rectManipulatorParams.position, ref rectManipulatorParams.rotation,true,true);

				if(EditorGUI.EndChangeCheck())
				{
					foreach(IVertexManipulable vm in manipulables)
					{
						Denormalize(vm,rect,rectManipulatorParams.position,rectManipulatorParams.rotation);
					}
				}
			}
				
		}

		Rect GetRect(Vector3 position, Quaternion rotation)
		{
			Vector2 min = new Vector2(float.MaxValue,float.MaxValue);
			Vector2 max = new Vector2(float.MinValue,float.MinValue);

			Rect rect = new Rect();

			foreach(IVertexManipulable vm in manipulables)
			{
				for (int i = 0; i < vm.GetManipulableVertexCount(); i++)
				{
					Vector3 vertex = vm.GetManipulableVertex(i);
					Vector3 v = (Quaternion.Inverse (rotation) * (vertex - position));
					if (v.x < min.x)
						min.x = v.x;
					if (v.y < min.y)
						min.y = v.y;
					if (v.x > max.x)
						max.x = v.x;
					if (v.y > max.y)
						max.y = v.y;
				}
			}

			Vector2 offset = Vector2.one * 0.05f * HandleUtility.GetHandleSize(position);
			rect.min = min - offset;
			rect.max = max + offset;

			return rect;
		}

		void Normalize(IVertexManipulable vm, Rect rect, Vector3 position, Quaternion rotation)
		{
			IRectManipulable rm = vm as IRectManipulable;

			if(rm == null)
			{
				return;
			}

			rm.rectManipulatorData.normalizedVertices.Clear();

			for (int i = 0; i < vm.GetManipulableVertexCount(); i++)
			{
				Vector3 v = vm.GetManipulableVertex(i);

				v = (Quaternion.Inverse(rotation) * (v - position)) - (Vector3)rect.min;
				v.x /= rect.width;
				v.y /= rect.height;

				rm.rectManipulatorData.normalizedVertices.Add(v);
			}
		}

		void Denormalize(IVertexManipulable vm, Rect rect, Vector3 position, Quaternion rotation)
		{
			IRectManipulable rm = vm as IRectManipulable;

			if(rm == null)
			{
				return;
			}

			for (int i = 0; i < vm.GetManipulableVertexCount(); i++)
			{
				Vector3 v = rm.rectManipulatorData.normalizedVertices [i];

				v = (rotation * (Vector3.Scale (v, (Vector3)rect.size) + (Vector3)rect.min)) + position;

				vm.SetManipulatedVertex(i, v);
			}
		}
	}
}
