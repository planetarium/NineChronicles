using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using SimpleSpritePacker;

namespace SimpleSpritePackerEditor
{
	public class SPTools
	{
		public static string Settings_UseSpriteThumbsKey = "SPSettings_UseSpriteThumbs";
		public static string Settings_ThumbsHeightKey = "SPSettings_SpriteThumbsHeight";
		public static string Settings_UseScrollViewKey = "SPSettings_SpriteScrollView";
		public static string Settings_ScrollViewHeightKey = "SPSettings_SpriteScrollViewHeight";
		public static string Settings_DisableReadWriteEnabled = "SPSettings_DisableReadWriteEnabled";
		public static string Settings_AllowMuliSpritesOneSource = "SPSettings_AllowMuliSpritesOneSource";
		public static string Settings_ShowSpritesKey = "SP_ShowSprites";
		public static string Settings_SavedInstanceIDKey = "SP_SavedInstanceID";
		
		/// <summary>
		/// Prepares the default editor preference values.
		/// </summary>
		public static void PrepareDefaultEditorPrefs()
		{
			if (!EditorPrefs.HasKey(SPTools.Settings_UseScrollViewKey))
			{
				EditorPrefs.SetBool(SPTools.Settings_UseScrollViewKey, true);
			}
			if (!EditorPrefs.HasKey(SPTools.Settings_ScrollViewHeightKey))
			{
				EditorPrefs.SetFloat(SPTools.Settings_ScrollViewHeightKey, 216f);
			}
			if (!EditorPrefs.HasKey(SPTools.Settings_ShowSpritesKey))
			{
				EditorPrefs.SetBool(SPTools.Settings_ShowSpritesKey, true);
			}
			if (!EditorPrefs.HasKey(SPTools.Settings_UseSpriteThumbsKey))
			{
				EditorPrefs.SetBool(SPTools.Settings_UseSpriteThumbsKey, true);
			}
			if (!EditorPrefs.HasKey(SPTools.Settings_ThumbsHeightKey))
			{
				EditorPrefs.SetFloat(SPTools.Settings_ThumbsHeightKey, 50f);
			}
			if (!EditorPrefs.HasKey(SPTools.Settings_AllowMuliSpritesOneSource))
			{
				EditorPrefs.SetBool(SPTools.Settings_AllowMuliSpritesOneSource, true);
			}
		}
		
		/// <summary>
		/// Gets the editor preference bool value with the specified key.
		/// </summary>
		/// <returns><c>true</c>, if editor preference bool was gotten, <c>false</c> otherwise.</returns>
		/// <param name="key">Key.</param>
		public static bool GetEditorPrefBool(string key)
		{
			return EditorPrefs.GetBool(key);
		}
		
		/// <summary>
		/// Gets the asset path of a texture.
		/// </summary>
		/// <returns>The asset path.</returns>
		/// <param name="texture">Texture.</param>
		public static string GetAssetPath(Texture2D texture)
		{
			if (texture == null)
				return string.Empty;
			
			return AssetDatabase.GetAssetPath(texture.GetInstanceID());
		}
		
		/// <summary>
		/// Gets the asset path of a object.
		/// </summary>
		/// <returns>The asset path.</returns>
		/// <param name="obj">Object.</param>
		public static string GetAssetPath(Object obj)
		{
			if (obj == null)
				return string.Empty;
			
			return AssetDatabase.GetAssetPath(obj);
		}
		
		/// <summary>
		/// Does asset reimport.
		/// </summary>
		/// <param name="path">Path.</param>
		/// <param name="options">Options.</param>
		public static void DoAssetReimport(string path, ImportAssetOptions options)
		{
			try
			{
				AssetDatabase.StartAssetEditing();
				AssetDatabase.ImportAsset(path, options);
			}
			finally
			{
				AssetDatabase.StopAssetEditing();
			}
		}
		
		/// <summary>
		/// Removes the read only flag from the asset.
		/// </summary>
		/// <returns><c>true</c>, if read only flag was removed, <c>false</c> otherwise.</returns>
		/// <param name="path">Path.</param>
		public static bool RemoveReadOnlyFlag(string path)
		{
			if (string.IsNullOrEmpty(path))
				return false;
			
			// Clear the read-only flag in texture file attributes
			if (System.IO.File.Exists(path))
			{
				#if !UNITY_4_1 && !UNITY_4_0 && !UNITY_3_5
				if (!AssetDatabase.IsOpenForEdit(path))
				{
					Debug.LogError(path + " is not editable. Did you forget to do a check out?");
					return false;
				}
				#endif
				System.IO.FileAttributes texPathAttrs = System.IO.File.GetAttributes(path);
				texPathAttrs &= ~System.IO.FileAttributes.ReadOnly;
				System.IO.File.SetAttributes(path, texPathAttrs);
				
				return true;
			}
			
			// Default
			return false;
		}
		
		/// <summary>
		/// Creates a blank atlas texture.
		/// </summary>
		/// <returns><c>true</c>, if blank texture was created, <c>false</c> otherwise.</returns>
		/// <param name="path">Asset Path.</param>
		/// <param name="alphaTransparency">If set to <c>true</c> alpha transparency.</param>
		public static bool CreateBlankTexture(string path, bool alphaTransparency)
		{
			if (string.IsNullOrEmpty(path))
				return false;
			
			// Prepare blank texture
			Texture2D texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
			
			// Create the texture asset
			AssetDatabase.CreateAsset(texture, AssetDatabase.GenerateUniqueAssetPath(path));
	
			// Clear the read-only flag in texture file attributes
			if (!SPTools.RemoveReadOnlyFlag(path))
				return false;
			
			// Write the texture data
			byte[] bytes = texture.EncodeToPNG();
			System.IO.File.WriteAllBytes(path, bytes);
			bytes = null;
			
			// Get the asset texture importer
			TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
			
			if (textureImporter == null)
				return false;
			
			TextureImporterSettings settings = new TextureImporterSettings();
			textureImporter.ReadTextureSettings(settings);
			
			settings.spriteMode = 2;
			settings.readable = false;
			settings.maxTextureSize = 4096;
			settings.wrapMode = TextureWrapMode.Clamp;
			settings.npotScale = TextureImporterNPOTScale.ToNearest;
			settings.textureFormat = TextureImporterFormat.ARGB32;
			settings.filterMode = FilterMode.Point;
			settings.aniso = 4;
			settings.alphaIsTransparency = alphaTransparency;
			
			textureImporter.SetTextureSettings(settings);
			textureImporter.textureType = TextureImporterType.Sprite;
			
			AssetDatabase.SaveAssets();
			SPTools.DoAssetReimport(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			
			return true;
		}
		
		/// <summary>
		/// Imports a texture as asset.
		/// </summary>
		/// <returns><c>true</c>, if texture was imported, <c>false</c> otherwise.</returns>
		/// <param name="path">Path.</param>
		/// <param name="texture">Texture.</param>
		public static bool ImportTexture(string path, Texture2D texture)
		{
			if (string.IsNullOrEmpty(path))
				return false;
				
			// Clear the read-only flag in texture file attributes
			if (!SPTools.RemoveReadOnlyFlag(path))
				return false;
			
			byte[] bytes = texture.EncodeToPNG();
			System.IO.File.WriteAllBytes(path, bytes);
			bytes = null;
			
			AssetDatabase.SaveAssets();
			SPTools.DoAssetReimport(path, ImportAssetOptions.ForceSynchronousImport);
			
			return true;
		}
		
		/// <summary>
		/// Sets the texture asset Read/Write enabled state.
		/// </summary>
		/// <returns><c>true</c>, if set read write enabled was textured, <c>false</c> otherwise.</returns>
		/// <param name="texture">Texture.</param>
		/// <param name="enabled">If set to <c>true</c> enabled.</param>
		/// <param name="force">If set to <c>true</c> force.</param>
		public static bool TextureSetReadWriteEnabled(Texture2D texture, bool enabled, bool force)
		{
			return SPTools.AssetSetReadWriteEnabled(SPTools.GetAssetPath(texture), enabled, force);
		}
		
		/// <summary>
		/// Sets the asset Read/Write enabled state.
		/// </summary>
		/// <returns><c>true</c>, if set read write enabled was asseted, <c>false</c> otherwise.</returns>
		/// <param name="path">Path.</param>
		/// <param name="enabled">If set to <c>true</c> enabled.</param>
		/// <param name="force">If set to <c>true</c> force.</param>
		public static bool AssetSetReadWriteEnabled(string path, bool enabled, bool force)
		{
			if (string.IsNullOrEmpty(path))
				return false;
			
			TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
			
			if (ti == null)
				return false;
			
			TextureImporterSettings settings = new TextureImporterSettings();
			ti.ReadTextureSettings(settings);
			
			if (force || settings.readable != enabled)
			{
				settings.readable = enabled;
				ti.SetTextureSettings(settings);
				SPTools.DoAssetReimport(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			}
			
			return true;
		}
		
		/// <summary>
		/// Sets the asset texture format.
		/// </summary>
		/// <returns><c>true</c>, if set format was set, <c>false</c> otherwise.</returns>
		/// <param name="path">Path.</param>
		/// <param name="format">Format.</param>
		public static bool AssetSetFormat(string path, TextureImporterFormat format)
		{
			if (string.IsNullOrEmpty(path))
				return false;
			
			TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
			
			if (ti == null)
				return false;
			
			TextureImporterSettings settings = new TextureImporterSettings();
			ti.ReadTextureSettings(settings);
			
			settings.textureFormat = format;
			ti.SetTextureSettings(settings);
			SPTools.DoAssetReimport(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);

			return true;
		}

		/// <summary>
		/// Imports and configures atlas texture.
		/// </summary>
		/// <returns><c>true</c>, if import and configure atlas texture was successful, <c>false</c> otherwise.</returns>
		/// <param name="targetTexture">Target texture.</param>
		/// <param name="sourceTexture">Source texture.</param>
		/// <param name="uvs">Uvs.</param>
		/// <param name="names">Names.</param>
		/// <param name="defaultPivot">Default pivot.</param>
		/// <param name="defaultCustomPivot">Default custom pivot.</param>
		public static bool ImportAndConfigureAtlasTexture(Texture2D targetTexture, Texture2D sourceTexture, Rect[] uvs, SPSpriteImportData[] spritesImportData)
		{
			// Get the asset path
			string assetPath = SPTools.GetAssetPath(targetTexture);
			
			if (string.IsNullOrEmpty(assetPath))
			{
				Debug.LogError("Sprite Packer failed to Import and Configure the atlas texture, reason: Could not resolve asset path.");
				return false;
			}
			
			// Clear the read-only flag in texture file attributes
			if (!SPTools.RemoveReadOnlyFlag(assetPath))
			{
				Debug.LogError("Sprite Packer failed to Import and Configure the atlas texture, reason: Could not remove the readonly flag from the asset.");
				return false;
			}
			
			// Write the source texture data to the asset
			byte[] bytes = sourceTexture.EncodeToPNG();
			System.IO.File.WriteAllBytes(assetPath, bytes);
			bytes = null;
			
			// Get the asset texture importer
			TextureImporter texImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
			
			if (texImporter == null)
			{
				Debug.LogError("Sprite Packer failed to Import and Configure the atlas texture, reason: Could not get the texture importer for the asset.");
				return false;
			}
			
			// Get the asset texture importer settings
			TextureImporterSettings texImporterSettings = new TextureImporterSettings();
			
			// Apply sprite type
			texImporter.textureType = TextureImporterType.Sprite;
			texImporter.spriteImportMode = SpriteImportMode.Multiple;
			
			// Configure the spritesheet meta data
			SpriteMetaData[] spritesheetMeta = new SpriteMetaData[uvs.Length];
			for (int i = 0; i < uvs.Length; i++)
			{
				if (SPTools.HasSpritesheetMeta(texImporter.spritesheet, spritesImportData[i].name))
				{
					SpriteMetaData currentMeta = SPTools.GetSpritesheetMeta(texImporter.spritesheet, spritesImportData[i].name);
					Rect currentRect = uvs[i];
					currentRect.x *= sourceTexture.width;
					currentRect.width *= sourceTexture.width; 
					currentRect.y *= sourceTexture.height;
					currentRect.height *= sourceTexture.height;
					currentMeta.rect = currentRect;
					spritesheetMeta[i] = currentMeta;
				}
				else
				{
					SpriteMetaData currentMeta = new SpriteMetaData();
					Rect currentRect = uvs[i];
					currentRect.x *= sourceTexture.width;
					currentRect.width *= sourceTexture.width; 
					currentRect.y *= sourceTexture.height;
					currentRect.height *= sourceTexture.height;
					currentMeta.rect = currentRect;
					currentMeta.name = spritesImportData[i].name;
					currentMeta.alignment = (int)spritesImportData[i].alignment;
					currentMeta.pivot = spritesImportData[i].pivot;
					currentMeta.border = spritesImportData[i].border;
					spritesheetMeta[i] = currentMeta;
				}
			}
			texImporter.spritesheet = spritesheetMeta;
			
			// Read the texture importer settings
			texImporter.ReadTextureSettings(texImporterSettings);
			
			// Disable Read/Write
			texImporterSettings.readable = false;
			
			// Re-set the texture importer settings
			texImporter.SetTextureSettings(texImporterSettings);
			
			// Save and Reimport the asset
			AssetDatabase.SaveAssets();
			SPTools.DoAssetReimport(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			
			// Return success
			return true;
		}
		
		/// <summary>
		/// Determines if the specified name has spritesheet meta data.
		/// </summary>
		/// <returns><c>true</c> if has spritesheet meta the specified collection name; otherwise, <c>false</c>.</returns>
		/// <param name="collection">Collection.</param>
		/// <param name="name">Name.</param>
		private static bool HasSpritesheetMeta(SpriteMetaData[] collection, string name)
		{
			for (int i = 0; i < collection.Length; i++)
			{
				if (collection[i].name == name)
					return true;
			}
			
			return false;
		}
		
		/// <summary>
		/// Gets the spritesheet meta data for the specified name.
		/// </summary>
		/// <returns>The spritesheet meta.</returns>
		/// <param name="collection">Collection.</param>
		/// <param name="name">Name.</param>
		private static SpriteMetaData GetSpritesheetMeta(SpriteMetaData[] collection, string name)
		{
			for (int i = 0; i < collection.Length; i++)
			{
				if (collection[i].name == name)
					return collection[i];
			}
			
			return new SpriteMetaData();
		}
		
		/// <summary>
		/// Loads a sprite from a texture.
		/// </summary>
		/// <returns>The sprite.</returns>
		/// <param name="mainTexture">Main texture.</param>
		/// <param name="name">Name.</param>
		public static Sprite LoadSprite(Texture2D mainTexture, string name)
		{
			string texturePath = SPTools.GetAssetPath(mainTexture);
			Object[] atlasAssets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
			
			foreach (Object asset in atlasAssets)
			{
				if (AssetDatabase.IsSubAsset(asset) && asset.name == name)
				{
					return asset as Sprite;
				}
			}
			
			return null;
		}
		
		/// <summary>
		/// Determines if the specified object is main asset.
		/// </summary>
		/// <returns><c>true</c> if is main asset the specified obj; otherwise, <c>false</c>.</returns>
		/// <param name="obj">Object.</param>
		public static bool IsMainAsset(Object obj)
		{
			return AssetDatabase.IsMainAsset(obj);
		}
		
		/// <summary>
		/// Determines if the specified object has sub assets.
		/// </summary>
		/// <returns><c>true</c> if has sub assets the specified obj; otherwise, <c>false</c>.</returns>
		/// <param name="obj">Object.</param>
		public static bool HasSubAssets(Object obj)
		{
			return (AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(obj)).Length > 1);
		}
		
		/// <summary>
		/// Gets the sub assets of an object.
		/// </summary>
		/// <returns>The sub assets.</returns>
		/// <param name="obj">Object.</param>
		public static Object[] GetSubAssets(Object obj)
		{
			Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(obj));
			List<Object> subAssets = new List<Object>();
			
			foreach (Object asset in assets)
			{
				if (AssetDatabase.IsSubAsset(asset))
					subAssets.Add(asset);
			}
			
			return subAssets.ToArray();
		}
		
		/// <summary>
		/// Determines if the specified path is directory.
		/// </summary>
		/// <returns><c>true</c> if is directory the specified path; otherwise, <c>false</c>.</returns>
		/// <param name="path">Path.</param>
		public static bool IsDirectory(string path)
		{
			if (string.IsNullOrEmpty(path))
				return false;
			
			return System.IO.Directory.Exists(path);
		}
		
		/// <summary>
		/// Gets the assets in the specified directory.
		/// </summary>
		/// <returns>The directory assets.</returns>
		/// <param name="path">Path.</param>
		public static Object[] GetDirectoryAssets(string path)
		{
			List<Object> assets = new List<Object>();
			
			// Get the file paths of all the files in the specified directory
			string[] assetPaths = System.IO.Directory.GetFiles(path);
			
			// Enumerate through the list of files loading the assets they represent
			foreach (string assetPath in assetPaths)
			{
				// Check if it's a meta file
				if (assetPath.Contains(".meta"))
					continue;
				
				Object objAsset = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Object));
				
				if (objAsset != null)
					assets.Add(objAsset);
			}
			
			// Return the array of objects
			return assets.ToArray();
		}
		
		/// <summary>
		/// Filters the resources for atlas import.
		/// </summary>
		/// <returns>The resources for atlas import.</returns>
		/// <param name="resources">Resources.</param>
		public static Object[] FilterResourcesForAtlasImport(Object[] resources)
		{
			List<Object> tempList = new List<Object>();
			
			foreach (Object resource in resources)
			{
				string resourcePath = SPTools.GetAssetPath(resource);
				
				// Check if this is a main asset and queue all it's sub assets
				if (SPTools.IsMainAsset(resource) && SPTools.HasSubAssets(resource))
				{
					Object[] subAssets = SPTools.FilterResourcesForAtlasImport(SPTools.GetSubAssets(resource));
					
					foreach (Object a in subAssets) tempList.Add(a);
				}
				else if (resource is Texture2D || resource is Sprite)
				{
					tempList.Add(resource);
				}
				else if (SPTools.IsDirectory(resourcePath))
				{
					Object[] subAssets = SPTools.FilterResourcesForAtlasImport(SPTools.GetDirectoryAssets(resourcePath));
					
					foreach (Object a in subAssets) tempList.Add(a);
				}
			}
			
			return tempList.ToArray();
		}
		
		/// <summary>
		/// Replaces all the references in the scene (does not work with internal properties).
		/// </summary>
		/// <param name="spriteInfoList">Sprite info list.</param>
		/// <param name="spriteRenderersOnly">If set to <c>true</c> sprite renderers only.</param>
		/// <returns>The replaced references count.</returns>
		public static int ReplaceReferencesInScene(List<SPSpriteInfo> spriteInfoList, SPReferenceReplacerWindow.ReplaceMode replaceMode, bool spriteRenderersOnly)
		{
			Component[] comps = Resources.FindObjectsOfTypeAll<Component>();
			
			bool replaceAtlas = (replaceMode == SPReferenceReplacerWindow.ReplaceMode.AtlasWithSource);
			
			int count = 0;
			foreach (SPSpriteInfo spriteInfo in spriteInfoList)
			{
				if (spriteInfo.source == null || !(spriteInfo.source is Sprite) || spriteInfo.targetSprite == null)
					continue;
				
				count += SPTools.ReplaceReferences(comps, (replaceAtlas ? spriteInfo.targetSprite : (spriteInfo.source as Sprite)), (replaceAtlas ? (spriteInfo.source as Sprite) : spriteInfo.targetSprite), spriteRenderersOnly);
			}
			
			return count;
		}
		
		/// <summary>
		/// Replaces all the references in the project (does not work with internal properties).
		/// </summary>
		/// <param name="spriteInfoList">Sprite info list.</param>
		/// <param name="spriteRenderersOnly">If set to <c>true</c> sprite renderers only.</param>
		/// <returns>The replaced references count.</returns>
		public static int ReplaceReferencesInProject(List<SPSpriteInfo> spriteInfoList, SPReferenceReplacerWindow.ReplaceMode replaceMode, bool spriteRenderersOnly)
		{
			Component[] comps = SPTools.GetProjectPrefabComponents();
			
			bool replaceAtlas = (replaceMode == SPReferenceReplacerWindow.ReplaceMode.AtlasWithSource);
			
			int count = 0;
			foreach (SPSpriteInfo spriteInfo in spriteInfoList)
			{
				if (spriteInfo.source == null || !(spriteInfo.source is Sprite) || spriteInfo.targetSprite == null)
					continue;
				
				count += SPTools.ReplaceReferences(comps, (replaceAtlas ? spriteInfo.targetSprite : (spriteInfo.source as Sprite)), (replaceAtlas ? (spriteInfo.source as Sprite) : spriteInfo.targetSprite), spriteRenderersOnly);
			}
			
			return count;
		}
		
		/// <summary>
		/// Replaces all the references in all scenes.
		/// </summary>
		/// <param name="spriteInfoList">Sprite info list.</param>
		/// <param name="spriteRenderersOnly">If set to <c>true</c> sprite renderers only.</param>
		/// <param name="skipCurrent">If set to <c>true</c> skip current scene.</param>
		/// <returns>The replaced references count.</returns>
		public static int ReplaceReferencesInAllScenes(List<SPSpriteInfo> spriteInfoList, SPReferenceReplacerWindow.ReplaceMode replaceMode, bool spriteRenderersOnly, bool skipCurrent)
		{
			int count = 0;
			bool replaceAtlas = (replaceMode == SPReferenceReplacerWindow.ReplaceMode.AtlasWithSource);
			
			// Grab the current scene name
			string startingScene = EditorApplication.currentScene;
			
			// Get all scene names
			string[] sceneNames = SPTools.GetAllScenesNames();
			
			if (sceneNames.Length == 0)
				return count;
			
			foreach (string sceneName in sceneNames)
			{
				// Check if we should skip the scene
				if (skipCurrent && sceneName.Equals(startingScene))
					continue;
				
				// Try opening the scene
				if (EditorApplication.OpenScene(sceneName))
				{
					Component[] comps = Object.FindObjectsOfType<Component>();
					
					foreach (SPSpriteInfo spriteInfo in spriteInfoList)
					{
						if (spriteInfo.source == null || !(spriteInfo.source is Sprite) || spriteInfo.targetSprite == null)
							continue;
						
						count += SPTools.ReplaceReferences(comps, (replaceAtlas ? spriteInfo.targetSprite : (spriteInfo.source as Sprite)), (replaceAtlas ? (spriteInfo.source as Sprite) : spriteInfo.targetSprite), spriteRenderersOnly);
					}
					
					EditorApplication.SaveScene();
				}
			}
			
			// Load back the original scene
			EditorApplication.OpenScene(startingScene);
			
			// Return the replaced references count
			return count;
		}
		
		/// <summary>
		/// Replaces all the references in the supplied array (does not work with internal properties).
		/// </summary>
		/// <param name="find">Find.</param>
		/// <param name="replaceWith">Replace with.</param>
		/// <param name="spriteRenderersOnly">If set to <c>true</c> sprite renderers only.</param>
		/// <returns>The replaced references count.</returns>
		public static int ReplaceReferences(Component[] components, Sprite find, Sprite replaceWith, bool spriteRenderersOnly)
		{
			if (components == null || components.Length == 0)
				return 0;
			
			int count = 0;
			foreach (Object comp in components)
			{
				// Handle sprite renderers differently
				if (comp is SpriteRenderer)
				{
					if ((comp as SpriteRenderer).sprite == find)
					{
						(comp as SpriteRenderer).sprite = replaceWith;
						count++;
					}
				}
				else if (comp is UnityEngine.UI.Image)
				{
					// If this component is not a sprite renderer
					if (spriteRenderersOnly)
						continue;
					
					if ((comp as UnityEngine.UI.Image).sprite == find)
					{
						(comp as UnityEngine.UI.Image).sprite = replaceWith;
						count++;
					}
				}
				else if (comp is UnityEngine.UI.Selectable)
				{
					// If this component is not a sprite renderer
					if (spriteRenderersOnly)
						continue;
					
					UnityEngine.UI.Selectable selectable = (comp as UnityEngine.UI.Selectable);
					UnityEngine.UI.SpriteState ss = selectable.spriteState;
					
					if (ss.highlightedSprite == find)
					{
						ss.highlightedSprite = replaceWith;
						count++;
					}
					if (ss.pressedSprite == find)
					{
						ss.pressedSprite = replaceWith;
						count++;
					}
					if (ss.disabledSprite == find)
					{
						ss.disabledSprite = replaceWith;
						count++;
					}
					
					selectable.spriteState = ss;
				}
				else
				{
					// If this component is not a sprite renderer
					if (spriteRenderersOnly)
						continue;
					
					// Get the fileds info
					FieldInfo[] fields = comp.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
					
					foreach (FieldInfo fieldInfo in fields)
					{
						if (fieldInfo == null)
							continue;
						
						object fieldValue = fieldInfo.GetValue(comp);
						
						// Handle arrays
						if (fieldInfo.FieldType.IsArray)
						{
							var fieldValueArray = fieldValue as System.Array;
							
							if (fieldValueArray == null || fieldValueArray.GetType() != typeof(Sprite[]))
								continue;
							
							bool changed = false;
							System.Array newArray = new System.Array[fieldValueArray.Length];
							fieldValueArray.CopyTo(newArray, 0);
							
							for (int i = 0; i < newArray.Length; i++)
							{
								object element = newArray.GetValue(i);
								
								if (element != null && element.GetType() == typeof(Sprite))
								{
									Sprite o = element as Sprite;
									
									// Check if the value is what we are looking for
									if (o == find)
									{
										newArray.SetValue((replaceWith as object), i);
										changed = true;
										count++;
									}
								}
							}
							
							// Repalce the array
							if (changed)
							{
								fieldInfo.SetValue(comp, newArray);
							}
						}
						// Handle structs
						else if (fieldInfo.FieldType.IsValueType && !fieldInfo.FieldType.IsEnum && !fieldInfo.IsLiteral)
						{
							FieldInfo[] structFields = fieldInfo.FieldType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
							
							foreach (FieldInfo structFieldInfo in structFields)
							{
								if (structFieldInfo == null)
									continue;
								
								if (structFieldInfo.FieldType == typeof(Sprite))
								{
									Sprite structFieldValue = structFieldInfo.GetValue(fieldValue) as Sprite;
									
									// Check if the value is what we are looking for
									if (structFieldValue == find)
									{
										// Replace
										structFieldInfo.SetValue(fieldValue, (replaceWith as object));
										count++;
									}
								}
							}
							
							fieldInfo.SetValue(comp, fieldValue);
						}
						// Handle direct sprites
						else if (fieldInfo.FieldType == typeof(Sprite))
						{
							// Check if the value is what we are looking for
							if ((fieldValue as Sprite) == find)
							{
								// Replace
								fieldInfo.SetValue(comp, (replaceWith as object));
								count++;
							}
						}
					}
				}
				
				if (PrefabUtility.GetPrefabType((comp as Component).gameObject) != PrefabType.None)
					EditorUtility.SetDirty((comp as Component));
			}
			
			return count;
		}
		
		/// <summary>
		/// Gets all scenes names.
		/// </summary>
		/// <returns>The all scenes names.</returns>
		public static string[] GetAllScenesNames()
		{
			List<string> list = new List<string>();
			
			foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
			{
				if (!scene.enabled) continue;
				list.Add(scene.path);
			}
			
			return list.ToArray();
		}
		
		public static Component[] GetProjectPrefabComponents()
		{
			List<Component> result = new List<Component>();
			
			string[] assets = AssetDatabase.GetAllAssetPaths();
			
			foreach (string assetPath in assets)
			{
				if (assetPath.IndexOf("Assets/") == -1)
					continue;
				
				UnityEngine.Object assetObj = AssetDatabase.LoadAssetAtPath(assetPath, typeof(UnityEngine.Object));
				
				if (assetObj == null)
					continue;
				
				if (PrefabUtility.GetPrefabType(assetObj) != PrefabType.None)
				{
					GameObject gameObject = assetObj as GameObject;
					if (gameObject != null)
					{
						Component[] comps = gameObject.GetComponentsInChildren<Component>(true);
						
						foreach (Component comp in comps)
						{
							result.Add(comp);
						}
					}
				}
			}
			
			return result.ToArray();
		}
	}
}