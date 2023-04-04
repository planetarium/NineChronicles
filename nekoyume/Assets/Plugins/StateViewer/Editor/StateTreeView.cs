#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.DevExtensions;
using Libplanet;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using StateViewer.Runtime;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Boolean = Bencodex.Types.Boolean;

namespace StateViewer.Editor
{
    public class StateTreeView : TreeView
    {
        private const int RootTreeViewItemId = 0;
        private static readonly string[] ValueKindNames = Enum.GetNames(typeof(ValueKind));
        private static readonly string[] ItemTypeNames = Enum.GetNames(typeof(ItemType));

        private readonly TableSheets _tableSheets;
        private Address _addr;
        private StateTreeViewItemModel? _itemModel;

        public ContentKind ContentKind { get; set; }

        public (Address addr, IValue value) Serialize()
        {
            return (_addr, _itemModel?.Serialize() ?? Null.Value);
        }

        public StateTreeView(
            TreeViewState treeViewState,
            MultiColumnHeader multiColumnHeader,
            TableSheets tableSheets)
            : base(treeViewState, multiColumnHeader)
        {
            rowHeight = EditorGUIUtility.singleLineHeight;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            _tableSheets = tableSheets;
        }

        public void SetData(
            Address addr,
            IValue? data,
            ContentKind contentKind = ContentKind.None)
        {
            _addr = addr;
            _itemModel = new StateTreeViewItemModel(
                data ?? Null.Value,
                alias: "root");
            ContentKind = contentKind;
            ProcessWhenItemModelHierarchyChanged(initialize: true);
        }

        private void ProcessWhenItemModelHierarchyChanged(bool initialize = false)
        {
            if (_itemModel is null)
            {
                return;
            }

            // Cache selection ids
            var selectionIdsPrev = GetSelection();
            // Cache Expanded ids
            var expandedIdsPrev = GetExpanded();
            const int firstId = RootTreeViewItemId + 1;
            _itemModel.SetTreeViewItemIdRecursive(firstId, alsoSetPrev: initialize);
            // Restore selection ids
            if (selectionIdsPrev is not null)
            {
                var selectionIds = selectionIdsPrev
                    .Select(prevId => _itemModel.FindIdRecursive(prevId))
                    .Where(prevId => prevId >= firstId)
                    .ToList();
                SetSelection(selectionIds, TreeViewSelectionOptions.None);
            }

            // Restore Expanded ids
            if (expandedIdsPrev is not null)
            {
                var expendedIds = expandedIdsPrev
                    .Select(prevId => _itemModel.FindIdRecursive(prevId))
                    .Where(prevId => prevId >= firstId)
                    .ToArray();
                SetExpanded(expendedIds);
            }

            Reload();
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
                            viewModel.Parent.ValueType != ValueKind.Dictionary)
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
                        var valueKindFrom = viewModel.ValueType;
                        var fromIndex = Array.IndexOf(
                            ValueKindNames,
                            valueKindFrom.ToString());
                        var toIndex = EditorGUI.Popup(
                            cellRect,
                            fromIndex,
                            ValueKindNames);
                        if (toIndex != fromIndex &&
                            EditorUtility.DisplayDialog(
                                "Warning",
                                "If you change the value kind, the value" +
                                " that does not saved to blockchain state will be lost.",
                                "OK",
                                "Cancel"))
                        {
                            var valueKindTo = Enum.Parse<ValueKind>(ValueKindNames[toIndex]);
                            viewModel.SetValueKindContent(valueKindTo);
                            if (valueKindFrom is ValueKind.List or ValueKind.Dictionary ||
                                valueKindTo is ValueKind.List or ValueKind.Dictionary)
                            {
                                ProcessWhenItemModelHierarchyChanged();
                            }
                        }

                        break;
                    case 3 // Value
                        when viewModel.ValueType is
                            ValueKind.Null or
                            ValueKind.List or
                            ValueKind.Dictionary:
                        GUI.Label(cellRect, viewModel.ValueContent);
                        break;
                    case 3 // Value
                        when viewModel.ValueType is ValueKind.Boolean:
                        var from = (bool)(Boolean)viewModel.Value;
                        var to = GUI.Toggle(cellRect, from, from ? "(true)" : "(false)");
                        if (to != from)
                        {
                            viewModel.SetValue((Boolean)to);
                        }

                        break;
                    case 3: // Value
                        var value = GUI.TextField(cellRect, viewModel.ValueContent);
                        if (value != viewModel.ValueContent)
                        {
                            // TODO: Validate value
                            viewModel.SetValueContent(value);
                        }

                        break;
                    case 4: // Add
                        if (viewModel.ValueType is ValueKind.List or ValueKind.Dictionary &&
                            GUI.Button(cellRect, "Add"))
                        {
                            OnAddButtonClicked(viewModel);
                        }

                        break;
                    case 5: // Remove
                        if (viewModel.Parent is not null &&
                            GUI.Button(cellRect, "Remove"))
                        {
                            viewModel.Parent.RemoveChild(viewModel);
                            ProcessWhenItemModelHierarchyChanged();
                        }

                        break;
                }
            }
        }

        private void OnAddButtonClicked(StateTreeViewItemModel viewModel)
        {
            if (ContentKind != ContentKind.None &&
                ContentKind != ContentKind.Inventory)
            {
                Debug.LogWarning(
                    $"There's no Implementation for {ContentKind}({nameof(ContentKind)}).");
                return;
            }

            if (ContentKind == ContentKind.None ||
                // NOTE: This is for `ContentKind.Inventory`.
                //       Check if `viewModel` is root of `Inventory`.
                viewModel.Parent is not null and not { TreeViewItemId: RootTreeViewItemId })
            {
                // NOTE: Why `viewModel.Children[0].Serialize()` not `viewModel.Children[0].Value`?
                //       Because to use same value of edited content.
                viewModel.AddChild(viewModel.Children.Count == 0
                    ? Null.Value
                    : viewModel.Children[0].Serialize());
                ProcessWhenItemModelHierarchyChanged();
                return;
            }

            // NOTE: This is for the case that `viewModel` is root of `Inventory`.
            void AddItem(string itemTypeName)
            {
                var itemType = Enum.Parse<ItemType>(itemTypeName);
                var pair = _tableSheets.ItemSheet.First(pair =>
                    pair.Value.ItemType == itemType);
                var itemValue = ItemFactory.CreateItem(
                        pair.Value,
                        new RandomImpl(DateTime.Now.Millisecond))
                    .Serialize();
                viewModel.AddChild(itemValue);
                ProcessWhenItemModelHierarchyChanged();
            }

            // Context menu for selecting item type.
            var menu = new GenericMenu();
            foreach (var itemTypeName in ItemTypeNames)
            {
                menu.AddItem(
                    new GUIContent(itemTypeName),
                    false,
                    () => AddItem(itemTypeName));
            }

            menu.ShowAsContext();
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
