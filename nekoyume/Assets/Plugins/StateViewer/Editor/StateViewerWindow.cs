#nullable enable

using StateViewer.Editor.Features;
using UnityEditor;
using UnityEngine;

namespace StateViewer.Editor
{
    public class StateViewerWindow : EditorWindow
    {
        private StateAndBalanceFeature _stateAndBalanceFeature;

        [SerializeField]
        private bool initialized;

        [MenuItem("Tools/Lib9c/State Viewer")]
        private static void ShowWindow() =>
            GetWindow<StateViewerWindow>("State Viewer", true).Show();

        private void OnEnable()
        {
            minSize = new Vector2(800f, 400f);
            _stateAndBalanceFeature = new StateAndBalanceFeature(this);
            initialized = true;
        }

        private void OnGUI()
        {
            if (!initialized)
            {
                return;
            }

            _stateAndBalanceFeature.DrawAll();
        }
    }
}
