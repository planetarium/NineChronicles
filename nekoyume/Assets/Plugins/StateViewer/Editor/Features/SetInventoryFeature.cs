using UnityEditor;
using UnityEngine;

namespace StateViewer.Editor.Features
{
    public class SetInventoryFeature : IStateViewerFeature
    {
        private const string ItemDataCsvHeader = "item_id,level,count";

        private readonly EditorWindow _editorWindow;

        private string _itemDataCsv = ItemDataCsvHeader;

        private Vector2 _itemDataCsvScrollPos;

        private bool _showExpectedState;

        public SetInventoryFeature(StateViewerWindow editorWindow)
        {
            _editorWindow = editorWindow;
        }

        public void OnGUI()
        {
            GUILayout.Label("Inputs", EditorStyles.boldLabel);
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            GUILayout.Label("Item Data Csv");
            _itemDataCsvScrollPos = GUILayout.BeginScrollView(
                _itemDataCsvScrollPos,
                false,
                true);
            var itemDataCsv = EditorGUI.TextArea(
                GetRect(minLineCount: 3),
                _itemDataCsv);
            if (itemDataCsv != _itemDataCsv)
            {
                _itemDataCsv = itemDataCsv;
                if (_showExpectedState)
                {
                    //
                }
            }

            GUILayout.EndScrollView();
            GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);

            // TODO: Draw state tree view.
            GUILayout.Label("Expected State", EditorStyles.boldLabel);
            var showExpectedState = GUILayout.Toggle(_showExpectedState, "Show Expected State");
            if (showExpectedState != _showExpectedState)
            {
                _showExpectedState = showExpectedState;
                if (_showExpectedState)
                {
                    //
                }
            }

            GUILayout.Label("Hmm...");
            if (GUILayout.Button("Set"))
            {
                // SetInventory();
            }
        }

        private static Rect GetRect(
            float? minWidth = null,
            int? minLineCount = null,
            float? maxHeight = null)
        {
            var minHeight = minLineCount.HasValue
                ? EditorGUIUtility.singleLineHeight * minLineCount.Value +
                  EditorGUIUtility.standardVerticalSpacing * (minLineCount.Value - 1)
                : EditorGUIUtility.singleLineHeight;
            maxHeight ??= minHeight;

            return GUILayoutUtility.GetRect(
                minWidth ?? 10f,
                10f,
                minHeight,
                maxHeight.Value,
                GUILayout.ExpandWidth(true));
        }
    }
}
