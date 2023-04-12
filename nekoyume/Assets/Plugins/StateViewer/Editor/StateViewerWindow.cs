#nullable enable

using StateViewer.Editor.Features;
using UnityEditor;
using UnityEngine;

namespace StateViewer.Editor
{
    public class StateViewerWindow : EditorWindow
    {
        public enum FeatureMode
        {
            StateAndBalance, // FIXME: Separate this mode to State and Balance.
            SetInventory,
        }

        private string[] _featureModeNames = {
            "State & Balance",
            "Set Inventory",
        };
        private int _selectedFeatureIndex;

        private StateAndBalanceFeature _stateAndBalanceFeature;
        private SetInventoryFeature _setInventoryFeature;

        [SerializeField]
        private bool initialized;

        [MenuItem("Tools/Lib9c/State Viewer")]
        private static void ShowWindow() =>
            GetWindow<StateViewerWindow>("State Viewer", true).Show();

        private void OnEnable()
        {
            minSize = new Vector2(800f, 400f);
            _selectedFeatureIndex = 0;
            _stateAndBalanceFeature = new StateAndBalanceFeature(this);
            _setInventoryFeature = new SetInventoryFeature(this);
            initialized = true;
        }

        private void OnGUI()
        {
            if (!initialized)
            {
                return;
            }

            _selectedFeatureIndex = GUILayout.Toolbar(
                _selectedFeatureIndex,
                _featureModeNames);
            switch (_selectedFeatureIndex)
            {
                case 0:
                    _stateAndBalanceFeature.OnGUI();
                    break;
                case 1:
                    _setInventoryFeature.OnGUI();
                    break;
            }
        }
    }
}
