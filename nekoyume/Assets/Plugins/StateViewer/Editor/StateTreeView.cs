#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Lib9c.DevExtensions;
using Lib9cCommonTool.Runtime;
using Libplanet.Crypto;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using StateViewer.Runtime;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Boolean = Bencodex.Types.Boolean;

namespace StateViewer.Editor
{
    [Serializable]
    public class StateTreeView : TreeView
    {
        private const int RootTreeViewItemId = 0;
        private static readonly string[] ValueKindNames = Enum.GetNames(typeof(ValueKind));
        private static readonly string[] ItemTypeNames = Enum.GetNames(typeof(ItemType));

        // SerializeField is used to ensure the view state is written to the window
        // layout file. This means that the state survives restarting Unity as long as the window
        // is not closed. If the attribute is omitted then the state is still serialized/deserialized.
        [SerializeField]
        private TreeViewState treeViewState;

        private TableSheets _tableSheets;
        private StateTreeViewItemModel? _itemModel;

        public Address? AccountAddress { get; private set; }

        public Address? Address { get; private set; }

        public ContentKind ContentKind { get; private set; }

        public (Address? accountAddr, Address? addr, IValue value) Serialize()
        {
            return (AccountAddress, Address, _itemModel?.Serialize() ?? Null.Value);
        }

        public StateTreeView(
            TableSheets tableSheets,
            ContentKind contentKind = ContentKind.None,
            (bool visible, string[] headerContents)? visibleHeaderContents = null)
            : base(new TreeViewState(), new StateTreeViewHeader())
        {
            treeViewState = state;
            _tableSheets = tableSheets;
            rowHeight = EditorGUIUtility.singleLineHeight;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            SetTableSheet(tableSheets);
            SetContentKind(contentKind);
            if (visibleHeaderContents is { } tuple)
            {
                SetVisibleHeaderColumns(tuple.visible, tuple.headerContents);
            }

            multiColumnHeader.ResizeToFit();
        }

        public void SetVisibleHeaderColumns(bool visible, params string[] headerContents)
        {
            var visibleColumns = new List<int>();
            var headerStateColumns = multiColumnHeader.state.columns;
            if (visible)
            {
                for (var i = 0; i < headerStateColumns.Length; i++)
                {
                    var stateColumn = headerStateColumns[i];
                    if (headerContents.Contains(stateColumn.headerContent.text))
                    {
                        visibleColumns.Add(i);
                    }
                }
            }
            else
            {
                for (var i = 0; i < headerStateColumns.Length; i++)
                {
                    var stateColumn = headerStateColumns[i];
                    if (headerContents.Contains(stateColumn.headerContent.text))
                    {
                        continue;
                    }

                    visibleColumns.Add(i);
                }
            }


            multiColumnHeader.state.visibleColumns = visibleColumns.ToArray();
        }

        public void SetTableSheet(TableSheets tableSheets)
        {
            _tableSheets = tableSheets;
        }

        public void SetData(
            Address? accountAddr,
            Address? addr,
            IValue? data,
            ContentKind? contentKind = null)
        {
            AccountAddress = accountAddr;
            Address = addr;
            _itemModel = new StateTreeViewItemModel(
                data ?? Null.Value,
                alias: "root");
            SetContentKind(contentKind ?? ContentKind, force: true);
            ProcessWhenItemModelHierarchyChanged(initialize: true);
        }

        public void SetContentKind(ContentKind contentKind, bool force = false)
        {
            if (!force &&
                contentKind == ContentKind)
            {
                return;
            }

            ContentKind = contentKind;
            if (_itemModel is null)
            {
                return;
            }

            switch (ContentKind)
            {
                case ContentKind.None:
                    for (var i = 0; i < _itemModel.Children.Count; i++)
                    {
                        var child = _itemModel.Children[i];
                        child.OverrideAliasContent = null;
                    }

                    break;
                case ContentKind.Inventory:
                    for (var i = 0; i < _itemModel.Children.Count; i++)
                    {
                        var child = _itemModel.Children[i];
                        var count = child.Children[0].ValueContent;
                        var itemSheetId = child.Children[1].Children
                            .First(e => e.IndexOrKeyContent == "id")
                            .ValueContent;
                        var content = $"{itemSheetId} x{count}";
                        child.OverrideAliasContent = content;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ProcessWhenItemModelHierarchyChanged(
            bool initialize = false)
        {
            if (_itemModel is null)
            {
                return;
            }

            const int firstId = RootTreeViewItemId + 1;
            if (initialize)
            {
                _itemModel.SetTreeViewItemIdRecursive(firstId, alsoSetPrev: initialize);
                SetExpanded(firstId, true);
                Reload();
                return;
            }

            // Cache selection and expanded ids
            var selectionIdsPrev = GetSelection();
            var expandedIdsPrev = GetExpanded();
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

            SetContentKind(ContentKind, force: true);
            Reload();
        }

        public void ClearData() => SetData(default, default, Null.Value);

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

        // FIXME: It needs to be refactored.
        //        Implement a StateTreeViewItem for ItemBase and use it like below.
        //        ```
        //        var item = (StateTreeViewItem)args.item;
        //        item.RowGUI();
        //        ```
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
                    case 3: // Value
                        DrawValueColumn(viewModel, cellRect);
                        break;
                    case 4: // Add
                        DrawAddColumn(viewModel, cellRect);
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

        private void DrawValueColumn(
            StateTreeViewItemModel viewModel,
            Rect cellRect)
        {
            switch (viewModel.ValueType)
            {
                case ValueKind.Null:
                case ValueKind.List:
                case ValueKind.Dictionary:
                    GUI.Label(cellRect, viewModel.ValueContent);
                    return;
                case ValueKind.Boolean:
                    var from = (bool)(Boolean)viewModel.Value;
                    var to = GUI.Toggle(cellRect, from, from ? "(true)" : "(false)");
                    if (to != from)
                    {
                        viewModel.SetValue((Boolean)to);
                    }

                    return;
                case ValueKind.Integer:
                case ValueKind.Binary:
                case ValueKind.Text:
                default:
                    break;
            }

            var value = GUI.TextField(cellRect, viewModel.ValueContent);
            if (value == viewModel.ValueContent)
            {
                return;
            }

            viewModel.SetValueContent(value);
            if (ContentKind == ContentKind.None)
            {
                return;
            }

            // NOTE: This is for `ContentKind.Inventory`.
            //       Check if `viewModel` is <see cref="ItemBase.Id"> of `Inventory`.
            if (viewModel is null or not
                {
                    Parent:
                    {
                        IndexOrKeyContent: "item",
                        Parent:
                        {
                            ValueType: ValueKind.Dictionary,
                            Parent:
                            {
                                ValueType: ValueKind.List,
                                TreeViewItemId: RootTreeViewItemId + 1,
                            },
                        },
                    }
                })
            {
                return;
            }

            var random = new RandomImpl(DateTime.Now.Millisecond);
            var itemTypeContent = viewModel.Siblings!.First(child =>
                child.IndexOrKeyContent == "item_type").ValueContent;
            switch (viewModel.IndexOrKeyContent)
            {
                case "id":
                {
                    if (!int.TryParse(viewModel.ValueContent, out var itemId) ||
                        !_tableSheets.ItemSheet.TryGetValue(itemId, out var itemRow))
                    {
                        return;
                    }

                    if (itemRow.ItemType != ItemType.Equipment)
                    {
                        var item = ItemFactory.CreateItem(itemRow, random);
                        viewModel.Parent!.SetValue(
                            item.Serialize(),
                            reuseChildren: itemTypeContent == itemRow.ItemType.ToString());
                        ProcessWhenItemModelHierarchyChanged();
                        return;
                    }

                    if (itemTypeContent == ItemType.Equipment.ToString())
                    {
                        var nonFungibleItemIdContent = viewModel.Siblings!.First(child =>
                            child.IndexOrKeyContent == "itemId")?.ValueContent ?? null;
                        var nonFungibleItemId = nonFungibleItemIdContent is null
                            ? Guid.NewGuid()
                            : new Guid(
                                ((Binary)StateTreeViewItemModel.ParseToValue(
                                    nonFungibleItemIdContent, ValueKind.Binary)).ToByteArray()
                            );
                        var levelContent = viewModel.Siblings!.FirstOrDefault(child =>
                            child.IndexOrKeyContent == "level")?.ValueContent ?? "0";
                        var level = int.TryParse(levelContent, out var l) ? l : 0;
                        var equipment = BlacksmithMaster.CraftEquipment(
                            itemId,
                            nonFungibleItemId: nonFungibleItemId,
                            level: level,
                            tableSheets: _tableSheets,
                            random: random)!;
                        viewModel.Parent!.SetValue(equipment.Serialize(), reuseChildren: true);
                        ProcessWhenItemModelHierarchyChanged();
                    }
                    else
                    {
                        var equipment = BlacksmithMaster.CraftEquipment(
                            itemId,
                            tableSheets: _tableSheets,
                            random: random)!;
                        viewModel.Parent!.SetValue(equipment.Serialize());
                        ProcessWhenItemModelHierarchyChanged();
                    }

                    return;
                }
                case "level":
                {
                    if (itemTypeContent != "Equipment")
                    {
                        return;
                    }

                    var itemIdContent = viewModel.Siblings!.First(child =>
                        child.IndexOrKeyContent == "id").ValueContent;
                    var itemId = int.TryParse(itemIdContent, out var id) ? id : 0;
                    var nonFungibleItemIdContent = viewModel.Siblings!.First(child =>
                        child.IndexOrKeyContent == "itemId")?.ValueContent ?? null;
                    var nonFungibleItemId = nonFungibleItemIdContent is null
                        ? Guid.NewGuid()
                        : new Guid(
                            ((Binary) StateTreeViewItemModel.ParseToValue(
                                nonFungibleItemIdContent,
                                ValueKind.Binary)).ToByteArray()
                            );
                    var level = int.TryParse(viewModel.ValueContent, out var l) ? l : 0;
                    var equipment = BlacksmithMaster.CraftEquipment(
                        itemId,
                        nonFungibleItemId: nonFungibleItemId,
                        level: level,
                        tableSheets: _tableSheets,
                        random: random)!;
                    // FIXME: The expanded state of `viewModel` is not restored.
                    //        Maybe we should replace some valueContents not to set the IValue.
                    viewModel.Parent!.SetValue(equipment.Serialize(), reuseChildren: true);
                    ProcessWhenItemModelHierarchyChanged();
                    return;
                }
            }
        }

        private void DrawAddColumn(
            StateTreeViewItemModel viewModel,
            Rect cellRect)
        {
            if (viewModel.ValueType is not (ValueKind.List or ValueKind.Dictionary) ||
                !GUI.Button(cellRect, "Add"))
            {
                return;
            }

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
            void AddInventoryItem(string itemTypeName)
            {
                var itemType = Enum.Parse<ItemType>(itemTypeName);
                var pair = _tableSheets.ItemSheet.First(pair =>
                    pair.Value.ItemType == itemType);
                var itemBase = ItemFactory.CreateItem(
                    pair.Value,
                    new RandomImpl(DateTime.Now.Millisecond));
                var inventoryItem = new Inventory.Item(itemBase);
                viewModel.AddChild(inventoryItem.Serialize());
                ProcessWhenItemModelHierarchyChanged();
            }

            // Context menu for selecting item type.
            var menu = new GenericMenu();
            foreach (var itemTypeName in ItemTypeNames)
            {
                menu.AddItem(
                    new GUIContent(itemTypeName),
                    false,
                    () => AddInventoryItem(itemTypeName));
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
