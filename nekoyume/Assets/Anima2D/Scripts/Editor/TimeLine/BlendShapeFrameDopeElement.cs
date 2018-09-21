using UnityEngine;
using UnityEditor;

namespace Anima2D
{
	public class BlendShapeFrameDopeElement : IDopeElement
	{
		public delegate void Callback(BlendShapeFrame frame, float weight);
		public static Callback onFrameChanged;

		public static BlendShapeFrameDopeElement Create(BlendShapeFrame frame)
		{
			BlendShapeFrameDopeElement element = null;

			if (frame)
			{
				element = new BlendShapeFrameDopeElement ();

				element.blendShapeFrame = frame;
			}

			return element;
		}

		public BlendShapeFrame blendShapeFrame { get; set; }

		public float time {
			get { 
				if (blendShapeFrame)
				{
					return blendShapeFrame.weight;
				}
				return 0f;
			}
			set { 
				if (blendShapeFrame)
				{
					SerializedCache.RegisterObjectUndo(blendShapeFrame, "Set weight");
					blendShapeFrame.weight = Mathf.Clamp(value,1f,100f);
				}
			}
		}

		public void Flush()
		{
			if(onFrameChanged != null)
			{
				onFrameChanged(blendShapeFrame, time);
			}
		}
	}
}
