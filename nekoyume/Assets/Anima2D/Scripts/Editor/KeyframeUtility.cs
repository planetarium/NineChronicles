using UnityEngine;
using System.Collections;

namespace Anima2D
{
	public class KeyframeUtility
	{
		public enum TangentMode
		{
			Editable = 0,
			Smooth = 1,
			Linear = 2,
			Stepped = Linear | Smooth,
		}

		public static TangentMode GetKeyTangentMode(int tangentMode, int leftRight)
		{
			if (leftRight == 0)
			{
				return (TangentMode) ((tangentMode & 6) >> 1);
			}else{
				return (TangentMode) ((tangentMode & 24) >> 3);
			}
		}

		public static void SetKeyTangentMode(ref Keyframe keyframe, int leftRight, TangentMode mode)
		{
			int tangentMode = keyframe.tangentMode;

			if (leftRight == 0)
			{
				tangentMode &= -7;
				tangentMode |= (int) mode << 1;
			}
			else
			{
				tangentMode &= -25;
				tangentMode |= (int) mode << 3;
			}

			keyframe.tangentMode = tangentMode;
		}
	}
}
