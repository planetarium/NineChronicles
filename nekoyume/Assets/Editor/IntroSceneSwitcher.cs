using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityToolbarExtender;

namespace Nekoyume.Editor
{
    [InitializeOnLoad]
    public class SceneSwitchLeftButton
    {
        private static class ToolbarStyles
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

        private static void OnToolbarGUI()
        {
            GUILayout.FlexibleSpace();
            const string toolTip = "Start IntroScene";
            const string sceneName = "IntroScene";

            var sprite = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Unknown)
                .FirstOrDefault();
            if (sprite == null)
            {
                if (GUILayout.Button(new GUIContent("Intro", toolTip), ToolbarStyles.commandButtonStyle))
                {
                    SceneHelper.StartScene(sceneName);
                }
            }
            else
            {
                if (GUILayout.Button(new GUIContent(sprite, toolTip), ToolbarStyles.commandButtonStyle))
                {
                    SceneHelper.StartScene(sceneName);
                }
            }
        }
    }
}
