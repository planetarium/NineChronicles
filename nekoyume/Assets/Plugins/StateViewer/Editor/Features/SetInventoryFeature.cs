using UnityEditor;
using UnityEngine;

namespace StateViewer.Editor.Features
{
    public class SetInventoryFeature : IStateViewerFeature
    {
        private EditorWindow _editorWindow;

        public SetInventoryFeature(EditorWindow editorWindow)
        {
            _editorWindow = editorWindow;
        }

        public void OnGUI()
        {
            GUILayout.Label("Here is the SetInventory feature.");
        }
    }
}
