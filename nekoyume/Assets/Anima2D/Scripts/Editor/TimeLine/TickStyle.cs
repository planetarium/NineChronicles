using System;
using UnityEngine;

namespace Anima2D
{
	[Serializable]
	internal class TickStyle
	{
		public Color color = new Color(0f, 0f, 0f, 0.2f);
		public Color labelColor = new Color(0f, 0f, 0f, 1f);
		public int distMin = 10;
		public int distFull = 80;
		public int distLabel = 50;
		public string unit = string.Empty;

		public TickStyle()
		{
			if (EditorExtra.IsProSkin())
			{
				this.color = new Color(0.45f, 0.45f, 0.45f, 0.2f);
				this.labelColor = new Color(0.8f, 0.8f, 0.8f, 0.32f);
			}
			else
			{
				this.color = new Color(0f, 0f, 0f, 0.2f);
				this.labelColor = new Color(0f, 0f, 0f, 0.32f);
			}
		}
	}
}