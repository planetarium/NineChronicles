using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

namespace Anima2D 
{
	[InitializeOnLoad]
	public class ColorRing
	{
		static List<Color> mColors = new List<Color>();
		
		public static Color GetColor(int index)
		{
			index = Mathf.Clamp(index,0,index);
			index %= mColors.Count;

			return mColors[index];
		}

		static ColorRing()
		{
			float hueAngleStep = Mathf.Clamp(45f,1f,360f);
			float hueLoopOffset = Mathf.Clamp(20f,1f,360f);

			int numColors = (int)(360f / hueAngleStep) * (int)(360f / hueLoopOffset);

			mColors.Capacity = numColors;

			for(int i = 0; i < numColors; ++i)
			{
				float hueAngle = i * hueAngleStep;
				float loops = (int)(hueAngle / 360f);
				float hue = ((hueAngle % 360f + (loops * hueLoopOffset % 360f)) / 360f);

#if UNITY_5_0_0 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
				mColors.Add(EditorGUIUtility.HSVToRGB(hue, 1f, 1f));
#else
				mColors.Add(Color.HSVToRGB(hue, 1f, 1f));

#endif
			}
		}
	}
}
