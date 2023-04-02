#nullable enable

using System;
using UnityEditor.IMGUI.Controls;
using StateViewer.Runtime;

namespace StateViewer.Editor
{
    [Serializable]
    public sealed class StateTreeViewItem : TreeViewItem
    {
        public StateTreeViewItemModel ViewModel { get; }

        public StateTreeViewItem(StateTreeViewItemModel viewModel) : base(viewModel.TreeViewItemId)
        {
            displayName = viewModel.IndexOrKeyContent;
            ViewModel = viewModel;
        }
    }
}
