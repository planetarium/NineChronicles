using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace StateViewer.Editor
{
    public class StateTreeViewHeader : MultiColumnHeader
    {
        public StateTreeViewHeader() : base(CreateDefaultMultiColumnHeaderState())
        {
        }

        private static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
        {
            var indexOrKeyColumn = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Index/Key"),
                headerTextAlignment = TextAlignment.Center,
                canSort = false,
                width = 100,
                minWidth = 100,
                autoResize = true,
                allowToggleVisibility = false,
            };
            var aliasColumn = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Alias"),
                headerTextAlignment = TextAlignment.Center,
                canSort = false,
                width = 150,
                minWidth = 150,
                autoResize = true,
                allowToggleVisibility = true,
            };
            var valueKindColumn = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("ValueKind"),
                headerTextAlignment = TextAlignment.Center,
                canSort = false,
                width = 100,
                minWidth = 100,
                maxWidth = 100,
                autoResize = true,
                allowToggleVisibility = false,
            };
            var valueColumn = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Value"),
                headerTextAlignment = TextAlignment.Center,
                canSort = false,
                width = 300,
                minWidth = 100,
                autoResize = true,
                allowToggleVisibility = false,
            };
            var addColumn = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Add"),
                headerTextAlignment = TextAlignment.Center,
                canSort = false,
                width = 100,
                minWidth = 100,
                maxWidth = 100,
                autoResize = true,
                allowToggleVisibility = false,
            };
            var removeColumn = new MultiColumnHeaderState.Column
            {
                headerContent = new GUIContent("Remove"),
                headerTextAlignment = TextAlignment.Center,
                canSort = false,
                width = 100,
                minWidth = 100,
                maxWidth = 100,
                autoResize = true,
                allowToggleVisibility = false,
            };
            return new MultiColumnHeaderState(new[]
            {
                indexOrKeyColumn,
                aliasColumn,
                valueKindColumn,
                valueColumn,
                addColumn,
                removeColumn,
            });
        }
    }
}
