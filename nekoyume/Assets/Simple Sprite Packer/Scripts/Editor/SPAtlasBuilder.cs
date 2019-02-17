using UnityEngine;
using UnityEditor;
using SimpleSpritePacker;
using SimpleSpritePackerEditor;
using System.Collections;
using System.Collections.Generic;

namespace SimpleSpritePackerEditor
{
	public class SPAtlasBuilder {
	
		private SPInstance m_Instance;
		
		// Constructor
		public SPAtlasBuilder(SPInstance instance)
		{
			this.m_Instance = instance;
		}
		
		public static int CompareBySize(SPSpriteInfo a, SPSpriteInfo b)
		{
			// A is null b is not b is greater so put it at the front of the list
			if (a == null && b != null) return 1;
			
			// A is not null b is null a is greater so put it at the front of the list
			if (a != null && b == null) return -1;
			
			// Get the total pixels used for each sprite
			float aPixels = a.sizeForComparison.x * a.sizeForComparison.y;
			float bPixels = b.sizeForComparison.x * b.sizeForComparison.y;
			
			if (aPixels > bPixels) return -1;
			else if (aPixels < bPixels) return 1;
			return 0;
		}
		
		/// <summary>
		/// Rebuilds the atlas texture.
		/// </summary>
		public void RebuildAtlas()
		{
			if (this.m_Instance == null)
			{
				Debug.LogError("SPAtlasBuilder failed to rebuild the atlas, reason: Sprite Packer Instance reference is null.");
				return;
			}
			
			if (this.m_Instance.texture == null)
			{
				Debug.LogWarning("Sprite Packer failed to rebuild the atlas, please make sure the atlas texture reference is set.");
				return;
			}
			
			// Make the atlas texture readable
			if (SPTools.TextureSetReadWriteEnabled(this.m_Instance.texture, true, false))
			{
				// Get a list with the current sprites and applied actions
				List<SPSpriteInfo> spriteInfoList = this.m_Instance.GetSpriteListWithAppliedActions();
				
				// Get the source textures asset paths
				string[] sourceTexturePaths = this.CollectSourceTextureAssetPaths(spriteInfoList);
				
				// Make the source textures readable
				if (!this.SetAssetsReadWriteEnabled(sourceTexturePaths, true))
				{
					Debug.LogError("Sprite Packer failed to make one or more of the source texture readable, please do it manually.");
					return;
				}
				
				// Make sure all the textures have the correct texture format
				this.CorrectTexturesFormat(spriteInfoList);
				
				// If we are using max rects packing, sort the sprite info list by size
				if (this.m_Instance.packingMethod == SPInstance.PackingMethod.MaxRects)
				{
					spriteInfoList.Sort(CompareBySize);
				}
				
				// Temporary textures array
				Texture2D[] textures = new Texture2D[spriteInfoList.Count];
				
				// Create an array to contain the sprite import data
				SPSpriteImportData[] spritesImportData = new SPSpriteImportData[spriteInfoList.Count];
				
				// Populate the textures and names arrays
				int ia = 0;
				foreach (SPSpriteInfo si in spriteInfoList)
				{
					// Temporary texture
					Texture2D texture = null;
					
					// Prepare the sprite import data
					SPSpriteImportData importData = new SPSpriteImportData();
					
					// Prepare the sprite name
					importData.name = "Sprite_" + ia.ToString();
					
					if (si.targetSprite != null)
					{
						importData.name = si.targetSprite.name;
					}
					else if (si.source != null && (si.source is Texture2D || si.source is Sprite))
					{
						if (si.source is Texture2D) importData.name = (si.source as Texture2D).name;
						else importData.name = (si.source as Sprite).name;
					}
					
					// Prepare texture
					// In case the source texture is missing, rebuild from the already existing sprite
					if (si.source == null && si.targetSprite != null)
					{
						// Copy the sprite into the temporary texture
						texture = new Texture2D((int)si.targetSprite.rect.width, (int)si.targetSprite.rect.height, TextureFormat.ARGB32, false);
						Color[] pixels = si.targetSprite.texture.GetPixels((int)si.targetSprite.rect.x, 
						                                                   (int)si.targetSprite.rect.y, 
						                                                   (int)si.targetSprite.rect.width, 
						                                                   (int)si.targetSprite.rect.height);
						texture.SetPixels(pixels);
						texture.Apply();
					}
					// Handle texture source
					else if (si.source is Texture2D)
					{
						// Get as texture
						Texture2D sourceTex = si.source as Texture2D;
						
						// Check if we have as source texture
						if (sourceTex != null)
						{
							// Copy the source texture into the temp one
							texture = new Texture2D(sourceTex.width, sourceTex.height, TextureFormat.ARGB32, false);
							Color[] pixels = sourceTex.GetPixels(0, 0, sourceTex.width, sourceTex.height);
							texture.SetPixels(pixels);
							texture.Apply();
							
							// Transfer the sprite data
							importData.border = Vector4.zero;
							importData.alignment = this.m_Instance.defaultPivot;
							importData.pivot = this.m_Instance.defaultCustomPivot;
						}
					}
					// Handle sprite source
					else if (si.source is Sprite)
					{
						// Get as sprite
						Sprite sourceSprite = si.source as Sprite;
						
						// Make sure we have the sprite
						if (sourceSprite != null)
						{
							// Copy the sprite into the temporary texture
							texture = new Texture2D((int)sourceSprite.rect.width, (int)sourceSprite.rect.height, TextureFormat.ARGB32, false);
							Color[] pixels = sourceSprite.texture.GetPixels((int)sourceSprite.rect.x, 
							                                                (int)sourceSprite.rect.y, 
							                                                (int)sourceSprite.rect.width, 
							                                                (int)sourceSprite.rect.height);
							texture.SetPixels(pixels);
							texture.Apply();
							
							// Transfer the sprite data
							importData.border = sourceSprite.border;
							importData.alignment = SpriteAlignment.Custom;
							importData.pivot = new Vector2(	(0f - sourceSprite.bounds.center.x / sourceSprite.bounds.extents.x / 2 + 0.5f), 
							                               (0f - sourceSprite.bounds.center.y / sourceSprite.bounds.extents.y / 2 + 0.5f));
						}
					}
					
					// Save the new texture into our array
					textures[ia] = (texture != null) ? texture : new Texture2D(1, 1);
					
					// Set the sprite import data
					spritesImportData[ia] = importData;
					
					// Increase the indexer
					ia++;
				}
				
				// Make the source textures assets non readable
				if (SPTools.GetEditorPrefBool(SPTools.Settings_DisableReadWriteEnabled))
					this.SetAssetsReadWriteEnabled(sourceTexturePaths, false);
				
				// Clear the source textures asset paths
				System.Array.Clear(sourceTexturePaths, 0, sourceTexturePaths.Length);
				
				// Create a temporary texture for the packing
				Texture2D tempTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
				
				// UV coords array
				Rect[] uvs;
				
				// Pack the textures into the temporary
				if (this.m_Instance.packingMethod == SPInstance.PackingMethod.Unity)
				{
					uvs = tempTexture.PackTextures(textures, this.m_Instance.padding, this.m_Instance.maxSize);
				}
				else
				{
					uvs = UITexturePacker.PackTextures(tempTexture, textures, this.m_Instance.padding, this.m_Instance.maxSize);
					
					// Check if packing failed
					if (uvs == null)
					{
						Debug.LogError("Sprite Packer texture packing failed, the textures might be exceeding the specified maximum size.");
						return;
					}
				}
				
				// Import and Configure the texture atlas (also disables Read/Write)
				SPTools.ImportAndConfigureAtlasTexture(this.m_Instance.texture, tempTexture, uvs, spritesImportData);
				
				// Clear the current sprite info list
				this.m_Instance.ClearSprites();
				
				// Clear the actions list
				this.m_Instance.ClearActions();
				
				// Destroy the textures from the temporary textures array
				for (int ib = 0; ib < textures.Length; ib++)
					UnityEngine.Object.DestroyImmediate(textures[ib]);
				
				// Destroy the temporary texture
				UnityEngine.Object.DestroyImmediate(tempTexture);
				
				// Convert the temporary sprite info into array
				SPSpriteInfo[] spriteInfoArray = spriteInfoList.ToArray();
				
				// Clear the temporary sprite info list
				spriteInfoList.Clear();
				
				// Apply the new sprite reff to the sprite info and add the sprite info to the sprites list
				for (int i = 0; i < spriteInfoArray.Length; i++)
				{
					SPSpriteInfo info = spriteInfoArray[i];
					
					if (info.targetSprite == null)
						info.targetSprite = SPTools.LoadSprite(this.m_Instance.texture, spritesImportData[i].name);
					
					// Add to the instance sprite info list
					this.m_Instance.AddSprite(info);
				}
				
				// Clear the sprites import data array
				System.Array.Clear(spritesImportData, 0, spritesImportData.Length);
				
				// Set dirty
				EditorUtility.SetDirty(this.m_Instance);
			}
			else
			{
				Debug.LogError("Sprite Packer failed to make the atlas texture readable, please do it manually.");
			}
		}
		
		/// <summary>
		/// Collects the source textures asset paths.
		/// </summary>
		/// <returns>The source texture asset paths.</returns>
		/// <param name="spriteInfoList">Sprite info list.</param>
		protected string[] CollectSourceTextureAssetPaths(List<SPSpriteInfo> spriteInfoList)
		{
			List<string> texturePaths = new List<string>();
			
			// Add the textures from the sprite info list into our textures list
			foreach (SPSpriteInfo spriteInfo in spriteInfoList)
			{
				string path = string.Empty;
				
				// No source but present target sprite
				if (spriteInfo.source == null && spriteInfo.targetSprite != null)
				{
					path = SPTools.GetAssetPath(spriteInfo.targetSprite.texture);
				}
				// Texture source
				else if (spriteInfo.source is Texture2D)
				{
					path = SPTools.GetAssetPath(spriteInfo.source as Texture2D);
				}
				// Sprite source
				else if (spriteInfo.source is Sprite)
				{
					path = SPTools.GetAssetPath((spriteInfo.source as Sprite).texture);
				}
				
				if (!string.IsNullOrEmpty(path))
				{
					if (!texturePaths.Contains(path))
						texturePaths.Add(path);
				}
			}
			
			return texturePaths.ToArray();
		}
		
		/// <summary>
		/// Sets the assets read write enabled.
		/// </summary>
		/// <returns><c>true</c>, if assets read write enabled was set, <c>false</c> otherwise.</returns>
		/// <param name="assetPaths">Asset paths.</param>
		/// <param name="enabled">If set to <c>true</c> enabled.</param>
		protected bool SetAssetsReadWriteEnabled(string[] assetPaths, bool enabled)
		{
			bool success = true;
			
			// Make the assets readable
			foreach (string assetPath in assetPaths)
			{
				// Make the texture readable
				if (!SPTools.AssetSetReadWriteEnabled(assetPath, enabled, false))
				{
					Debug.LogWarning("Sprite Packer failed to set Read/Write state (" + enabled.ToString() + ") on asset: " + assetPath);
					success = false;
				}
			}
			
			// Return the result
			return success;
		}
		
		/// <summary>
		/// Corrects the textures format.
		/// </summary>
		/// <param name="spriteInfoList">Sprite info list.</param>
		protected void CorrectTexturesFormat(List<SPSpriteInfo> spriteInfoList)
		{
			if (spriteInfoList == null || spriteInfoList.Count == 0)
				return;
			
			foreach (SPSpriteInfo spriteInfo in spriteInfoList)
			{
				Texture2D texture = null;
				
				// No source but present target sprite
				if (spriteInfo.source == null && spriteInfo.targetSprite != null)
				{
					texture = spriteInfo.targetSprite.texture;
				}
				// Texture source
				else if (spriteInfo.source is Texture2D)
				{
					texture = (spriteInfo.source as Texture2D);
				}
				// Sprite source
				else if (spriteInfo.source is Sprite)
				{
					texture = (spriteInfo.source as Sprite).texture;
				}
				
				if (texture != null)
				{
					// Make sure it's the correct format
					if (texture.format != TextureFormat.ARGB32 && 
					    texture.format != TextureFormat.RGBA32 && 
					    texture.format != TextureFormat.BGRA32 && 
					    texture.format != TextureFormat.RGB24 && 
					    texture.format != TextureFormat.Alpha8 && 
					    texture.format != TextureFormat.DXT1 &&
					    texture.format != TextureFormat.DXT5)
					{
						// Get the texture asset path
						string assetPath = SPTools.GetAssetPath(texture);
						
						// Set new texture format
						if (!SPTools.AssetSetFormat(assetPath, TextureImporterFormat.ARGB32))
						{
							Debug.LogWarning("Sprite Packer failed to set texture format ARGB32 on asset: " + assetPath);
						}
					}
				}
			}
		}
	}
}