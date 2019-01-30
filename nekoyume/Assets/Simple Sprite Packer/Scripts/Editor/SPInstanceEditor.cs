using UnityEngine;
using UnityEditor;
using SimpleSpritePacker;
using System.Collections;
using System.Collections.Generic;

namespace SimpleSpritePackerEditor
{
	[CustomEditor(typeof(SPInstance))]
	public class SPInstanceEditor : Editor {
		
		private SPInstance m_SPInstance;
		private SPAtlasBuilder m_AtlasBuilder;
		
		private static Color green = new Color(0.345f, 0.625f, 0.370f, 1f);
		private static Color red = new Color(0.779f, 0.430f, 0.430f, 1f);
		private static Color spriteBoxNormalColor = new Color(0.897f, 0.897f, 0.897f, 1f);
		private static Color spriteBoxHighlightColor = new Color(0.798f, 0.926f, 0.978f, 1f);
		
		private Vector2 scrollViewOffset = Vector2.zero;
		private int m_SelectedSpriteInstanceID = 0;
		
		private GUIStyle boxStyle;
		private GUIStyle paddingStyle;
		
		protected void OnEnable()
		{
			this.m_SPInstance = this.target as SPInstance;
			this.m_AtlasBuilder = new SPAtlasBuilder(this.m_SPInstance);
			
			SPTools.PrepareDefaultEditorPrefs();
			
			this.boxStyle = new GUIStyle(EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector).box);
			this.paddingStyle = new GUIStyle();
			this.paddingStyle.padding = new RectOffset(3, 3, 3, 3);
		}
		
		protected void OnDisable()
		{
			this.m_AtlasBuilder = null;
			this.m_SPInstance = null;
		}
		
		public override void OnInspectorGUI()
		{
			this.serializedObject.Update();
			EditorGUILayout.PropertyField(this.serializedObject.FindProperty("m_Texture"), new GUIContent("Atlas Texture"));
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(this.serializedObject.FindProperty("m_Padding"), new GUIContent("Packing Padding"));
			this.MaxSizePopup(this.serializedObject.FindProperty("m_MaxSize"), "Packing Max Size");
			EditorGUILayout.PropertyField(this.serializedObject.FindProperty("m_PackingMethod"));
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(this.serializedObject.FindProperty("m_DefaultPivot"));
			
			if ((SpriteAlignment)this.serializedObject.FindProperty("m_DefaultPivot").enumValueIndex != SpriteAlignment.Custom)
				GUI.enabled = false;
				
			EditorGUILayout.PropertyField(this.serializedObject.FindProperty("m_DefaultCustomPivot"), new GUIContent("Default Cus. Pivot"));
			
			GUI.enabled = true;
			
			this.serializedObject.ApplyModifiedProperties();
			
			EditorGUILayout.Space();
			this.DrawCurrentSprites();
			EditorGUILayout.Space();
			if (this.m_SPInstance.pendingActions.Count > 0)
			{
				this.DrawActionButtons();
				EditorGUILayout.Space();
			}
			this.DrawPendingActions();
			EditorGUILayout.Space();
			this.DrawActionButtons();
			EditorGUILayout.Space();
			this.DropAreaGUI();
		}
		
		private void DrawActionButtons()
		{
			// Get a rect for the buttons
			Rect controlRect = EditorGUILayout.GetControlRect();
			
			// Clear Actions Button
			controlRect.width = (controlRect.width / 2f) - 6f;
			
			if (this.m_SPInstance.pendingActions.Count == 0)
				GUI.enabled = false;
			
			if (GUI.Button(controlRect, "Clear Actions", EditorStyles.miniButton))
			{
				this.m_SPInstance.ClearActions();
			}
			GUI.enabled = true;
			
			// Rebuild Button
			controlRect.x = controlRect.width + 24f;
			
			if (GUI.Button(controlRect, "Rebuild Atlas", EditorStyles.miniButton))
			{
				this.m_AtlasBuilder.RebuildAtlas();
			}
		}
		
		private void MaxSizePopup(SerializedProperty property, string label)
		{
			string[] names = new string[8] { "32", "64", "128", "256", "512", "1024", "2048", "4096" };
			int[] sizes = new int[8] { 32, 64, 128, 256, 512, 1024, 2048, 4096 };
			
			GUI.changed = false;
			int size = EditorGUILayout.IntPopup(label, property.intValue, names, sizes);
			
			if (GUI.changed)
			{
				property.intValue = size;
			}
		}
		
		private void DrawCurrentSprites()
		{
			if (this.m_SPInstance == null)
				return;
			
			Rect controlRect = EditorGUILayout.GetControlRect();
			GUI.Label(controlRect, "Sprites (" + this.m_SPInstance.sprites.Count.ToString() + ")", EditorStyles.boldLabel);
			
			GUI.Label(new Rect(controlRect.width - 120f, controlRect.y, 70f, 20f), "Scrollview");
			
			GUI.changed = false;
			bool sv = GUI.Toggle(new Rect(controlRect.width - 55f, controlRect.y + 1f, 20f, 20f), EditorPrefs.GetBool(SPTools.Settings_UseScrollViewKey), " ");
			if (GUI.changed)
			{
				EditorPrefs.SetBool(SPTools.Settings_UseScrollViewKey, sv);
			}
			
			GUI.Label(new Rect(controlRect.width - 40f, controlRect.y, 40f, 20f), "Show");
			
			GUI.changed = false;
			bool ss = GUI.Toggle(new Rect(controlRect.width - 2f, controlRect.y + 1f, 20f, 20f), EditorPrefs.GetBool(SPTools.Settings_ShowSpritesKey), " ");
			if (GUI.changed)
			{
				EditorPrefs.SetBool(SPTools.Settings_ShowSpritesKey, ss);
			}
			
			if (EditorPrefs.GetBool(SPTools.Settings_ShowSpritesKey))
			{
				if (this.m_SPInstance.sprites.Count == 0)
				{
					this.DrawMessage("The atlas does not contain sprites.");
					return;
				}
				
				EditorGUILayout.BeginVertical(this.boxStyle);
				
				if (EditorPrefs.GetBool(SPTools.Settings_UseScrollViewKey))
				{
					this.scrollViewOffset = EditorGUILayout.BeginScrollView(this.scrollViewOffset, GUILayout.Height(EditorPrefs.GetFloat(SPTools.Settings_ScrollViewHeightKey)));
				}
				
				if (EditorPrefs.GetBool(SPTools.Settings_UseSpriteThumbsKey))
				{
					this.DrawSpritesWithThumbs();
				}
				else
				{
					this.DrawSpritesSimple();
				}
				
				if (EditorPrefs.GetBool(SPTools.Settings_UseScrollViewKey))
				{
					EditorGUILayout.EndScrollView();
				}
				
				EditorGUILayout.EndVertical();
			}
		}
		
		private Color c;
		
		private void DrawSpritesWithThumbs()
		{
			// Get a copy of the sprites list
			List<SPSpriteInfo> sprites = this.m_SPInstance.copyOfSprites;
			sprites.Sort();
			
			EditorGUILayout.BeginHorizontal();
			GUILayout.Space(6f);
			EditorGUILayout.BeginVertical();
			
			RectOffset padding = this.paddingStyle.padding;
			RectOffset thumbnailPadding = new RectOffset(6, 6, 6, 3);
			float thumbnailMaxHeight = EditorPrefs.GetFloat(SPTools.Settings_ThumbsHeightKey);
			float labelHeight = 20f;
			float selectedExtensionHeight = 27f;
			
			GUIStyle labelStyle = new GUIStyle(EditorStyles.label);
			labelStyle.fontStyle = FontStyle.Normal;
			labelStyle.alignment = TextAnchor.MiddleCenter;
			
			// Draw the sprites
			foreach (SPSpriteInfo info in sprites)
			{
				bool isSelected = this.IsSelected(info.GetHashCode());
				Color boxColor = (isSelected ? SPInstanceEditor.spriteBoxHighlightColor : SPInstanceEditor.spriteBoxNormalColor);
				
				GUILayout.Space(6f);
				EditorGUILayout.BeginHorizontal(this.paddingStyle);
				
				if (info.targetSprite != null)
				{
					// Determine the control height
					float thumbnailHeight = (info.targetSprite.rect.height > thumbnailMaxHeight) ? thumbnailMaxHeight : info.targetSprite.rect.height;
					
					// Apply the thumb padding to the height
					float thumbnailHeightWithPadding = thumbnailHeight + thumbnailPadding.top + thumbnailPadding.bottom;
					
					// Generate a working rect for the control
					Rect controlRect = GUILayoutUtility.GetRect(0.0f, (thumbnailHeightWithPadding + labelHeight + (isSelected ? (selectedExtensionHeight + padding.top) : 0f)), GUILayout.ExpandWidth(true));
					
					// Determine the click rect
					Rect clickRect = new Rect(controlRect.x, controlRect.y, controlRect.width, (controlRect.height - (isSelected ? (selectedExtensionHeight + padding.top) : 0f)));
					
					// Sprite box background
					GUI.color = boxColor;
					GUI.Box(new Rect(controlRect.x - padding.left, controlRect.y - padding.top, controlRect.width + (padding.left + padding.right), controlRect.height + (padding.top + padding.bottom)), "", this.boxStyle);
					GUI.color = Color.white;
					
					// Draw the thumbnail
					if (info.targetSprite.texture != null)
						this.DrawThumbnail(info, thumbnailHeight, controlRect, thumbnailPadding);
					
					// Draw the sprite name label
					GUI.Label(new Rect(controlRect.x, (controlRect.y + thumbnailHeightWithPadding + 1f), controlRect.width, labelHeight), info.targetSprite.name + " (" + info.targetSprite.rect.width + "x" + info.targetSprite.rect.height + ")", labelStyle);
					
					// Remove button
					if (GUI.Button(new Rect((controlRect.width - 9f), (controlRect.y + thumbnailHeightWithPadding + 2f), 18f, 18f), "X"))
					{
						this.m_SPInstance.QueueAction_RemoveSprite(info);
					}
					// Detect sprite clicks
					else if (Event.current.type == EventType.MouseUp && clickRect.Contains(Event.current.mousePosition))
					{
						EditorGUIUtility.PingObject(info.targetSprite);
						
						// Remove the focus of the focused control
						GUI.FocusControl("");
						
						// Set as selected
						if (!isSelected) this.SetSelected(info.GetHashCode());
					}
					
					// Draw the selected extension
					if (isSelected)
					{
						Rect extensionRect = new Rect((controlRect.x - padding.left), 
						                              (controlRect.y + thumbnailHeightWithPadding + labelHeight + padding.top), 
						                              (controlRect.width + (padding.left + padding.right)), 
						                              selectedExtensionHeight);
						
						// Box that looks like a separator
						if (Event.current.type == EventType.Repaint)
						{
							GUI.color = boxColor;
							this.boxStyle.Draw(new Rect(extensionRect.x, extensionRect.y, extensionRect.width, 1f), GUIContent.none, 0);
							GUI.color = Color.white;
						}
						
						// Draw the source label
						GUI.Label(new Rect(extensionRect.x + padding.left + 2f, extensionRect.y + padding.top + 3f, 60f, 20f), "Source:", EditorStyles.label);
						
						Rect sourceFieldRect = new Rect((extensionRect.x + 60f), (extensionRect.y + padding.top + 3f), (extensionRect.width - 66f), 18f);
						
						// Draw the sprite source field
						EditorGUI.BeginChangeCheck();
						Object source = EditorGUI.ObjectField(sourceFieldRect, info.source, typeof(Object), false);
						if (EditorGUI.EndChangeCheck())
							this.m_SPInstance.ChangeSpriteSource(info, source);
					}
				}
				
				EditorGUILayout.EndHorizontal();
			}
			GUILayout.Space(6f);
			
			EditorGUILayout.EndVertical();
			GUILayout.Space(6f);
			EditorGUILayout.EndHorizontal();
		}
		
		private void DrawThumbnail(SPSpriteInfo info, float height, Rect controlRect, RectOffset padding)
		{
			// Calculate the sprite rect inside the texture
			Rect spriteRect = new Rect(info.targetSprite.textureRect.x / info.targetSprite.texture.width, 
			                           info.targetSprite.textureRect.y / info.targetSprite.texture.height, 
			                           info.targetSprite.textureRect.width / info.targetSprite.texture.width, 
			                           info.targetSprite.textureRect.height / info.targetSprite.texture.height);
			
			// Get the original sprite size
			Vector2 spriteSize = new Vector2(info.targetSprite.rect.width, info.targetSprite.rect.height);
			
			// Determine the max size of the thumb
			Vector2 thumbMaxSize = new Vector2((controlRect.width - (padding.left + padding.right)), height);
			
			// Clamp the sprite size based on max width and height of the control
			if (spriteSize.x > thumbMaxSize.x)
			{
				spriteSize *= thumbMaxSize.x / spriteSize.x;
			}
			if (spriteSize.y > thumbMaxSize.y)
			{
				spriteSize *= thumbMaxSize.y / spriteSize.y;
			}
			
			// Prepare the rect for the texture draw
			Rect thumbRect = new Rect(0f, 0f, spriteSize.x, spriteSize.y);
			
			// Position in the middle of the control rect
			thumbRect.x = controlRect.x + ((controlRect.width - spriteSize.x) / 2f);
			thumbRect.y = controlRect.y + padding.top + ((height - spriteSize.y) / 2f);
			
			// Draw the thumbnail
			GUI.DrawTextureWithTexCoords(thumbRect, info.targetSprite.texture, spriteRect, true);
		}
		
		private void DrawSpritesSimple()
		{
			// Get a copy of the sprites list
			List<SPSpriteInfo> sprites = this.m_SPInstance.copyOfSprites;
			sprites.Sort();
			
			// Draw the sprites
			foreach (SPSpriteInfo info in sprites)
			{
				EditorGUILayout.Space();
				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(8f);
				
				if (info.targetSprite != null)
				{
					EditorGUILayout.ObjectField(info.targetSprite, typeof(Sprite), false, GUILayout.Height(20f));
				}
				else
				{
					EditorGUILayout.TextField("Missing sprite reference");
				}
				
				// Remove button
				if (GUILayout.Button("X", GUILayout.Width(20f)))
				{
					this.m_SPInstance.QueueAction_RemoveSprite(info);
				}
				
				GUILayout.Space(6f);
				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.Space();
		}
		
		private void DrawPendingActions()
		{
			if (this.m_SPInstance == null)
				return;
			
			EditorGUILayout.LabelField("Pending Actions", EditorStyles.boldLabel);
			
			float labelWidth = 90f;
			
			if (this.m_SPInstance.pendingActions.Count == 0)
			{
				this.DrawMessage("There are no pending actions.");
				return;
			}
			
			EditorGUILayout.BeginVertical();
			
			List<SPAction> unqueueList = new List<SPAction>();
			
			// Draw the actions
			foreach (SPAction action in this.m_SPInstance.pendingActions)
			{
				switch (action.actionType)
				{
					case SPAction.ActionType.Sprite_Add:
					{
						GUI.color = SPInstanceEditor.green;
						EditorGUILayout.BeginHorizontal(this.boxStyle);
						GUI.color = Color.white;
						
						EditorGUILayout.LabelField("Add Sprite", GUILayout.Width(labelWidth));
						EditorGUILayout.ObjectField(action.resource, action.resource.GetType(), false);
						
						// Remove action button
						if (GUILayout.Button("X", GUILayout.Width(20f)))
						{
							unqueueList.Add(action);
						}
						
						EditorGUILayout.EndHorizontal();
						break;
					}
					case SPAction.ActionType.Sprite_Remove:
					{
						GUI.color = SPInstanceEditor.red;
						EditorGUILayout.BeginHorizontal(this.boxStyle);
						GUI.color = Color.white;
						
						EditorGUILayout.LabelField("Remove Sprite", GUILayout.Width(labelWidth));
						EditorGUILayout.ObjectField(action.spriteInfo.targetSprite, typeof(Sprite), false);
						
						// Remove action button
						if (GUILayout.Button("X", GUILayout.Width(20f)))
						{
							unqueueList.Add(action);
						}
						
						EditorGUILayout.EndHorizontal();
						break;
					}
				}
			}
			
			// Unqueue actions in the list
			foreach (SPAction a in unqueueList)
			{
				this.m_SPInstance.UnqueueAction(a);
			}
			
			EditorGUILayout.EndVertical();
			EditorGUILayout.Space();
		}
		
		private void DropAreaGUI()
		{
			Event evt = Event.current;
			Rect drop_area = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
			this.boxStyle.alignment = TextAnchor.MiddleCenter;
			GUI.color = SPInstanceEditor.green;
			GUI.Box(drop_area, "Add Sprite (Drop Here)", this.boxStyle);
			GUI.color = Color.white;
			
			switch (evt.type)
			{
				case EventType.DragUpdated:
				case EventType.DragPerform:
				{
					if (!drop_area.Contains(evt.mousePosition))
						return;
					
					DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
					
					if (evt.type == EventType.DragPerform)
					{
						DragAndDrop.AcceptDrag();
						
						Object[] filtered = SPTools.FilterResourcesForAtlasImport(DragAndDrop.objectReferences);
						
						// Disallow miltiple sprites of the same source, if set so
						if (!SPTools.GetEditorPrefBool(SPTools.Settings_AllowMuliSpritesOneSource))
						{
							// Additional filtering specific to the instance
							for (int i = 0; i < filtered.Length; i++)
							{
								if (this.m_SPInstance.sprites.Find(s => s.source == filtered[i]) != null)
								{
									Debug.LogWarning("A sprite with source \"" + SimpleSpritePackerEditor.SPTools.GetAssetPath(filtered[i]) + "\" already exists in the atlas, consider changing the Sprite Packer settings to allow multiple sprites from the same source.");
									System.Array.Clear(filtered, i, 1);
								}
							}
						}
						
						// Types are handled internally
						this.m_SPInstance.QueueAction_AddSprites(filtered);
					}
					break;
				}
			}
		}
		
		private void DrawMessage(string message)
		{
			Rect rect = GUILayoutUtility.GetRect(0.0f, 25.0f, GUILayout.ExpandWidth(true));
			this.boxStyle.alignment = TextAnchor.MiddleCenter;
			GUI.Box(rect, message, this.boxStyle);
		}
		
		private bool IsSelected(int id)
		{
			return (this.m_SelectedSpriteInstanceID == id);
		}
		
		private void SetSelected(int id)
		{
			this.m_SelectedSpriteInstanceID = id;
			base.Repaint();
		}
		
		private static string GetSavePath()
		{
			string path = "Assets";
			Object obj = Selection.activeObject;
			
			if (obj != null)
			{
				path = AssetDatabase.GetAssetPath(obj.GetInstanceID());
				
				if (path.Length > 0)
				{
					if (!System.IO.Directory.Exists(path))
					{
						string[] pathParts = path.Split("/"[0]);
						pathParts[pathParts.Length - 1] = "";
						path = string.Join("/", pathParts);
					}
				}
			}
			
			return EditorUtility.SaveFilePanelInProject("Sprite Packer", "Sprite Packer", "asset", "Create a new sprite packer instance.", path);
		}
		
		[MenuItem("Assets/Create/Sprite Packer")]
		public static void CreateInstance()
		{
			string assetPath = GetSavePath();
			
			if (string.IsNullOrEmpty(assetPath))
				return;
			
			// Create the sprite packer instance
			SPInstance asset = ScriptableObject.CreateInstance("SPInstance") as SPInstance;
			AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(assetPath));
			AssetDatabase.Refresh();
			
			// Save the instance id in the editor prefs
			EditorPrefs.SetInt(SPTools.Settings_SavedInstanceIDKey, asset.GetInstanceID());
			
			// Repaint the SPDropWindow
			EditorWindow.GetWindow(typeof(SPDropWindow)).Repaint();
			
			// Get a name for the texture
			string texturePath = assetPath.Replace(".asset", ".png");
			
			// Create blank texture
			if (SPTools.CreateBlankTexture(texturePath, true))
			{
				// Set the texture reff in the sprite packer instance
				asset.texture = AssetDatabase.LoadAssetAtPath(texturePath, typeof(Texture2D)) as Texture2D;
			}
			
			// Focus on the new sprite packer
			EditorUtility.FocusProjectWindow();
			Selection.activeObject = asset;
		}
	}
}