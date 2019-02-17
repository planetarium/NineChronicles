using UnityEngine;
using UnityEditor;
using System.Collections;
using SimpleSpritePacker;

namespace SimpleSpritePackerEditor
{
	public class SPReferenceReplacerWindow : EditorWindow
	{
		public enum ReplaceMode : int
		{
			SourceWithAtlas,
			AtlasWithSource
		}
		
		public enum TargetMode : int
		{
			CurrentScene = 0,
			ProjectOnly = 1,
			CurrentSceneAndProject = 2,
			AllScenes = 3,
			AllScenesAndProject = 4,
		}
		
		private SPInstance m_Instance;
		private TargetMode m_TargetMode = TargetMode.AllScenesAndProject;
		private ReplaceMode m_ReplaceMode = ReplaceMode.SourceWithAtlas;
		
		private static RectOffset padding = new RectOffset(10, 10, 10, 10);
		public static string PrefsKey_TargetMode = "SPRefReplacer_TargetMode";
		public static string PrefsKey_SpriteRenderersOnly = "SPRefReplacer_SpriteRenderersOnly";
		
		[MenuItem ("Window/Simple Sprite Packer/Reference Replacer Tool")]
		public static void ShowWindow()
		{
			EditorWindow.GetWindow(typeof(SPReferenceReplacerWindow));
		}
		
		protected void OnEnable()
		{
			this.titleContent = new GUIContent("SP Reference Replacer");
			
			if (EditorPrefs.HasKey(SPTools.Settings_SavedInstanceIDKey))
			{
				string instancePath = AssetDatabase.GetAssetPath(EditorPrefs.GetInt(SPTools.Settings_SavedInstanceIDKey, 0));
				
				if (!string.IsNullOrEmpty(instancePath))
				{
					this.m_Instance = AssetDatabase.LoadAssetAtPath(instancePath, typeof(SPInstance)) as SPInstance;
				}
			}
			
			// Default prefs
			if (!EditorPrefs.HasKey(SPReferenceReplacerWindow.PrefsKey_TargetMode))
			{
				EditorPrefs.SetInt(SPReferenceReplacerWindow.PrefsKey_TargetMode, (int)this.m_TargetMode);
			}
			
			// Load target mode setting
			this.m_TargetMode = (TargetMode)EditorPrefs.GetInt(SPReferenceReplacerWindow.PrefsKey_TargetMode);
		}
		
		protected void OnGUI()
		{
			EditorGUIUtility.labelWidth = 100f;
			
			GUILayout.BeginVertical();
			GUILayout.Space((float)SPReferenceReplacerWindow.padding.top);
			GUILayout.BeginHorizontal();
			GUILayout.Space((float)SPReferenceReplacerWindow.padding.left);
			GUILayout.BeginVertical();
			
			GUI.changed = false;
			this.m_Instance = EditorGUILayout.ObjectField("Sprite Packer", this.m_Instance, typeof(SPInstance), false) as SPInstance;
			if (GUI.changed)
			{
				// Save the instance id
				EditorPrefs.SetInt(SPTools.Settings_SavedInstanceIDKey, (this.m_Instance == null) ? 0 : this.m_Instance.GetInstanceID());
			}
			
			GUILayout.Space(6f);
			
			GUILayout.BeginVertical(GUI.skin.box);
			GUILayout.Space(6f);
			
			GUILayout.BeginHorizontal();
			GUILayout.Space(6f);
			
			EditorGUILayout.LabelField("Replace mode", GUILayout.Width(130f));
			this.m_ReplaceMode = (ReplaceMode)EditorGUILayout.EnumPopup(this.m_ReplaceMode);
			
			GUILayout.Space(6f);
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			GUILayout.Space(6f);
			
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.LabelField("Replace references in", GUILayout.Width(130f));
			this.m_TargetMode = (TargetMode)EditorGUILayout.EnumPopup(this.m_TargetMode);
			if (EditorGUI.EndChangeCheck())
			{
				EditorPrefs.SetInt(SPReferenceReplacerWindow.PrefsKey_TargetMode, (int)this.m_TargetMode);
			}
			
			GUILayout.Space(6f);
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			GUILayout.Space(6f);
			GUI.changed = false;
			bool spriteRenderersOnly = GUILayout.Toggle(EditorPrefs.GetBool(SPReferenceReplacerWindow.PrefsKey_SpriteRenderersOnly), " Replace references in Sprite Renderers only ?");
			if (GUI.changed)
			{
				EditorPrefs.SetBool(SPReferenceReplacerWindow.PrefsKey_SpriteRenderersOnly, spriteRenderersOnly);
			}
			GUILayout.Space(6f);
			GUILayout.EndHorizontal();
			
			GUILayout.Space(6f);
			GUILayout.EndVertical();
			
			GUILayout.Space(6f);
			
			if (this.m_Instance == null)
			{
				EditorGUILayout.HelpBox("Please set the sprite packer instance reference in order to use this feature.", MessageType.Info);
			}
			else
			{
				if (GUILayout.Button("Replace"))
				{
					int replacedCount = 0;
					
					switch (this.m_TargetMode)
					{
					case TargetMode.CurrentScene:
					{
						replacedCount += SPTools.ReplaceReferencesInScene(this.m_Instance.copyOfSprites, this.m_ReplaceMode, spriteRenderersOnly);
						break;
					}
					case TargetMode.ProjectOnly:
					{
						replacedCount += SPTools.ReplaceReferencesInProject(this.m_Instance.copyOfSprites, this.m_ReplaceMode, spriteRenderersOnly);
						break;
					}
					case TargetMode.CurrentSceneAndProject:
					{
						replacedCount += SPTools.ReplaceReferencesInProject(this.m_Instance.copyOfSprites, this.m_ReplaceMode, spriteRenderersOnly);
						replacedCount += SPTools.ReplaceReferencesInScene(this.m_Instance.copyOfSprites, this.m_ReplaceMode, spriteRenderersOnly);
						break;
					}
					case TargetMode.AllScenes:
					{
						replacedCount += SPTools.ReplaceReferencesInAllScenes(this.m_Instance.copyOfSprites, this.m_ReplaceMode, spriteRenderersOnly, false);
						break;
					}
					case TargetMode.AllScenesAndProject:
					{
						replacedCount += SPTools.ReplaceReferencesInProject(this.m_Instance.copyOfSprites, this.m_ReplaceMode, spriteRenderersOnly);
						replacedCount += SPTools.ReplaceReferencesInScene(this.m_Instance.copyOfSprites, this.m_ReplaceMode, spriteRenderersOnly);
						EditorApplication.SaveScene();
						replacedCount += SPTools.ReplaceReferencesInAllScenes(this.m_Instance.copyOfSprites, this.m_ReplaceMode, spriteRenderersOnly, true);
						break;
					}
					}
					
					EditorUtility.DisplayDialog("Reference Replacer", "Replaced references count: " + replacedCount.ToString(), "Okay");
				}
			}
			
			GUILayout.EndVertical();
			GUILayout.Space((float)SPReferenceReplacerWindow.padding.right);
			GUILayout.EndHorizontal();
			GUILayout.Space((float)SPReferenceReplacerWindow.padding.bottom);
			GUILayout.EndVertical();
		}
	}
}