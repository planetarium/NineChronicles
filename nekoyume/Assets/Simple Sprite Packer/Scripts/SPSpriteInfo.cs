using UnityEngine;
using System;

namespace SimpleSpritePacker
{
	[System.Serializable]
	public class SPSpriteInfo : IComparable<SPSpriteInfo>
	{
		/// <summary>
		/// The source texture or sprite.
		/// </summary>
		public UnityEngine.Object source;
		
		/// <summary>
		/// The target sprite (the one in the atlas).
		/// </summary>
		public Sprite targetSprite;
		
		/// <summary>
		/// Gets the name of the sprite.
		/// </summary>
		/// <value>The name.</value>
		public string name
		{
			get
			{
				if (this.targetSprite != null)
				{
					return this.targetSprite.name;
				}
				else if (this.source != null)
				{
					return this.source.name;
				}
				
				// Default
				return string.Empty;
			}
		}
		
		/// <summary>
		/// Gets the sprite size used for comparison.
		/// </summary>
		/// <value>The size for comparison.</value>
		public Vector2 sizeForComparison
		{
			get
			{
				if (this.source != null)
				{
					if (this.source is Texture2D)
					{
						return new Vector2((this.source as Texture2D).width, (this.source as Texture2D).height);
					}
					else if (this.source is Sprite)
					{
						return (this.source as Sprite).rect.size;
					}
				}
				else if (this.targetSprite != null)
				{
					return this.targetSprite.rect.size;
				}
				
				// Default
				return Vector2.zero;
			}
		}
		
		public int CompareTo(SPSpriteInfo other)
		{
			return this.name.CompareTo(other.name);
		}
	}
}