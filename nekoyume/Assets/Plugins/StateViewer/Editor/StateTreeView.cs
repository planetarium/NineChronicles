using System;
using System.Collections.Generic;
using Bencodex.Types;
using Libplanet;
using StateViewer.Runtime;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace StateViewer.Editor
{
    public class StateTreeView : TreeView
    {
        private const int RootTreeViewItemId = 0;

        public event Action<bool> OnDirty;

        private Address _addr;
        private StateTreeViewItemModel _itemModel;
        private int _treeViewItemId;

        public (Address addr, IValue value) Serialize()
        {
            return (_addr, _itemModel.Serialize());
        }

        public StateTreeView(TreeViewState treeViewState, MultiColumnHeader multiColumnHeader)
            : base(treeViewState, multiColumnHeader)
        {
            rowHeight = EditorGUIUtility.singleLineHeight;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
        }

        public void SetData(Address addr, IValue data)
        {
            _addr = addr;
            _itemModel = new StateTreeViewItemModel(
                data,
                alias: "root");
            var treeViewItemId = RootTreeViewItemId + 1;
            void SetIdRecursive(StateTreeViewItemModel model)
            {
                model.TreeViewItemId = treeViewItemId++;
                foreach (var childModel in model.Children)
                {
                    childModel.TreeViewItemId = treeViewItemId++;
                    if (childModel.Children.Count > 0)
                    {
                        SetIdRecursive(childModel);
                    }
                }
            }

            SetIdRecursive(_itemModel);
            Reload();
            OnDirty?.Invoke(false);
        }

        public void ClearData() => SetData(default, Null.Value);

        protected override TreeViewItem BuildRoot()
        {
            return new TreeViewItem
            {
                id = RootTreeViewItemId,
                depth = -1,
            };
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var totalRows = GetRows() ?? new List<TreeViewItem>();
            totalRows.Clear();

            if (_itemModel is null)
            {
                return totalRows;
            }

            var item = new StateTreeViewItem(_itemModel);
            root.AddChild(item);
            totalRows.Add(item);
            if (_itemModel.Children.Count >= 1)
            {
                if (IsExpanded(_itemModel.TreeViewItemId))
                {
                    AddChildrenRecursive(_itemModel, item, totalRows);
                }
                else
                {
                    item.children = CreateChildListForCollapsedParent();
                }
            }

            SetupDepthsFromParentsAndChildren(root);
            return totalRows;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (StateTreeViewItem)args.item;
            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                var columnIndex = args.GetColumn(i);
                var cellRect = args.GetCellRect(i);
                CenterRectUsingSingleLineHeight(ref cellRect);

                var viewModel = item.ViewModel;
                switch (columnIndex)
                {
                    case 0: // Index or Key
                        var offset = GetContentIndent(item) + extraSpaceBeforeIconAndLabel;
                        cellRect.xMin += offset;
                        if (viewModel.Parent is null ||
                            viewModel.Parent.Value.Kind != ValueKind.Dictionary)
                        {
                            GUI.Label(cellRect, viewModel.IndexOrKeyContent);
                        }
                        else
                        {
                            var indexOrKey = GUI.TextField(cellRect, viewModel.IndexOrKeyContent);
                            if (indexOrKey != viewModel.IndexOrKeyContent)
                            {
                                viewModel.SetIndexOrKeyContent(indexOrKey);
                            }
                        }

                        break;
                    case 1: // Alias
                        GUI.Label(cellRect, viewModel.AliasContent);
                        break;
                    case 2: // ValueKind
                        var names = Enum.GetNames(typeof(ValueKind));
                        GUI.Label(cellRect, viewModel.Value.Kind.ToString());
                        // viewModel.ValueType = Enum.Parse<ValueKind>(
                        //     names[EditorGUI.Popup(
                        //         cellRect,
                        //         Array.IndexOf(names, viewModel.ValueType.ToString()),
                        //         names)]);

                        break;
                    case 3
                        when viewModel.Value.Kind is ValueKind.List or ValueKind.Dictionary: // Value
                        GUI.Label(cellRect, viewModel.ValueContent);
                        break;
                    case 3: // Value
                        var value = GUI.TextField(cellRect, viewModel.ValueContent);
                        if (value != viewModel.ValueContent)
                        {
                            // TODO: Validate value
                            viewModel.ValueContent = value;
                            OnDirty?.Invoke(true);
                        }

                        break;
                    case 4: // Edit
                        if (viewModel.Value.Kind is ValueKind.List or ValueKind.Dictionary &&
                            GUI.Button(cellRect, "Add"))
                        {
                            // if (viewModel.Children.Count > 0)
                            // {
                            //     viewModel.Children[0]
                            // }
                            //
                            // viewModel.AddChild(new StateTreeViewItemModel(
                            //     _treeViewItemId++,
                            //     viewModel.Children.Count == 0
                            //         ? ValueKind.Text
                            //         : viewModel.Children[0].KeyType,
                            //     viewModel.ValueType is ValueKind.List
                            //         ? $"{viewModel.Children.Count}"
                            //         : "New Key",
                            //     viewModel.Children.Count == 0
                            //         ? ValueKind.Text
                            //         : viewModel.Children[0].ValueType,
                            //     string.Empty,
                            //     editable: viewModel.Parent is
                            //         { ValueType: ValueKind.Dictionary }));
                            // viewModel.ValueContent = $"Count: {viewModel.Children.Count}";
                            // Reload();
                            // OnDirty?.Invoke(true);
                        }

                        break;
                    case 5:
                        if (viewModel.Parent is not null &&
                            GUI.Button(cellRect, "Remove"))
                        {
                            // viewModel.Parent.RemoveChild(viewModel);
                            // Reload();
                            // OnDirty?.Invoke(true);
                        }

                        break;
                }
            }
        }

        private void AddChildrenRecursive(
            StateTreeViewItemModel parentModel,
            TreeViewItem parentItem,
            ICollection<TreeViewItem> totalRows)
        {
            foreach (var childModel in parentModel.Children)
            {
                var childItem = new StateTreeViewItem(childModel);
                parentItem.AddChild(childItem);
                totalRows.Add(childItem);
                if (childModel.Children.Count >= 1)
                {
                    if (IsExpanded(childModel.TreeViewItemId))
                    {
                        AddChildrenRecursive(childModel, childItem, totalRows);
                        continue;
                    }

                    childItem.children = CreateChildListForCollapsedParent();
                }
            }
        }
    }
}
