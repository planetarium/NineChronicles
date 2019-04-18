using UnityEngine;
using System.Collections;

namespace SimpleSpritePacker
{
	[System.Serializable]
	public class SPAction
	{
		public enum ActionType
		{
			Sprite_Add,
			Sprite_Remove,
		}

		public ActionType actionType;
		public UnityEngine.Object resource;
		public SPSpriteInfo spriteInfo;
	}
}