using UnityEditor;
using UnityEngine;
using UnityToolbarExtender;

namespace Nekoyume.Editor
{
	[InitializeOnLoad]
	public class SceneSwitchLeftButton
	{
		static class ToolbarStyles
		{
			public static readonly GUIStyle commandButtonStyle;

			static ToolbarStyles()
			{
				commandButtonStyle = new GUIStyle("Command")
				{
					fontSize = 16,
					alignment = TextAnchor.MiddleCenter,
					imagePosition = ImagePosition.ImageAbove,
					fontStyle = FontStyle.Bold
				};
			}
		}
		
		static SceneSwitchLeftButton()
		{
			ToolbarExtender.LeftToolbarGUI.Add(OnToolbarGUI);
		}

		static void OnToolbarGUI()
		{
			GUILayout.FlexibleSpace();

			if(GUILayout.Button(new GUIContent("1", "Start IntroScene"), ToolbarStyles.commandButtonStyle))
			{
				SceneHelper.StartScene("IntroScene");
			}
		}
	}
}

