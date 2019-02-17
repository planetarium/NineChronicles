using UnityEngine;
using System.Collections;

namespace SimpleSpritePackerEditor
{
	[System.Serializable]
	public struct SPSpriteImportData
	{
		public string name;
		public Vector4 border;
		public SpriteAlignment alignment;
		public Vector2 pivot;
	}
}