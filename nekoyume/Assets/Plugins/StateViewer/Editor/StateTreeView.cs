using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Bencodex.Json;
using Bencodex.Types;
using Libplanet;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using Boolean = Bencodex.Types.Boolean;

namespace StateViewer.Editor
{
    public class StateTreeView : TreeView
    {
        public event Action<bool> OnDirty;

        private StateTreeViewItem.Model[] _itemModels;
        private Address _addr;
        private int _elementId;

        public (Address addr, IValue value) Serialize()
        {
            return (_addr, _itemModels[0].Serialize());
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
            var model = MakeItemModelRecursive("", data);
            _itemModels = new[] { model };
            Reload();
            OnDirty?.Invoke(false);
        }

        private static string Convert(IValue value)
        {
            var converter = new BencodexJsonConverter();
            var serializerOption = new JsonSerializerOptions
            {
                WriteIndented = false,
            };
            using var stream = new MemoryStream();
            var writerOption = new JsonWriterOptions
            {
                Indented = false,
            };
            var writer = new Utf8JsonWriter(stream, writerOption);
            converter.Write(writer, value, serializerOption);
            return Encoding.UTF8.GetString(stream.ToArray()).Replace("\\uFEFF", "")
                .Replace("\"", "");
        }

        private StateTreeViewItem.Model MakeItemModelRecursive(
            string key,
            IValue data,
            bool editable = false
        )
        {
            StateTreeViewItem.Model viewModel;
            switch (data)
            {
                case Null:
                case Binary:
                case Boolean:
                case Integer:
                case Text:
                {
                    viewModel = new StateTreeViewItem.Model(
                        _elementId++,
                        key,
                        data.Kind,
                        Convert(data),
                        editable: editable
                    );

                    return viewModel;
                }
                case List list:
                {
                    viewModel = new StateTreeViewItem.Model(
                        _elementId++,
                        key,
                        data.Kind,
                        $"Count: {list.Count}"
                    );
                    for (var i = 0; i < list.Count; i++)
                    {
                        var item = list[i];
                        var childViewModel = MakeItemModelRecursive(
                            $"{i.ToString()}",
                            item
                        );
                        viewModel.AddChild(childViewModel);
                    }

                    return viewModel;
                }
                case Dictionary dict:
                {
                    viewModel = new StateTreeViewItem.Model(
                        _elementId++,
                        key,
                        data.Kind,
                        $"Count: {dict.Count}"
                    );
                    foreach (var pair in dict)
                    {
                        StateTreeViewItem.Model childViewModel;
                        childViewModel = MakeItemModelRecursive(
                            $"{Convert(pair.Key)}",
                            pair.Value
                        );
                        viewModel.AddChild(childViewModel);
                    }

                    return viewModel;
                }
                default:
                    Debug.LogError($"data type {data.GetType()} is not supported.");
                    return null;
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            return new TreeViewItem
            {
                id = _elementId++,
                depth = -1,
                displayName = "Root",
            };
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var totalRows = GetRows() ?? new List<TreeViewItem>();
            totalRows.Clear();

            if (_itemModels is null)
            {
                return totalRows;
            }

            foreach (var model in _itemModels)
            {
                var item = new StateTreeViewItem(model);
                root.AddChild(item);
                totalRows.Add(item);
                if (model.Children.Count >= 1)
                {
                    if (IsExpanded(model.Id))
                    {
                        AddChildrenRecursive(model, item, totalRows);
                        continue;
                    }

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
                    case 0: // Key
                        base.RowGUI(args);
                        // if (viewModel.Editable)
                        // {
                        //     GUI.TextField(cellRect, viewModel.Key);
                        // }
                        // else
                        // {
                        //     GUI.Label(cellRect, viewModel.Key);
                        // }

                        break;
                    case 1: // DisplayKey
                        GUI.Label(cellRect, viewModel.DisplayKey);
                        break;
                    case 2: // Type
                        EditorGUI.BeginChangeCheck();
                        viewModel.Type =
                            (ValueKind)EditorGUI.EnumPopup(cellRect, "Type", viewModel.Type);
                        // GUI.Label(cellRect, viewModel.Type);
                        if (EditorGUI.EndChangeCheck())
                        {
                            Debug.LogFormat($"{viewModel.Type} is selected");
                        }

                        break;
                    case 3 when viewModel.Type is ValueKind.List or ValueKind.Dictionary: // Value
                        GUI.Label(cellRect, viewModel.Value);
                        break;
                    case 3:
                        if (viewModel.Editable)
                        {
                            var value = GUI.TextField(cellRect, viewModel.Value);
                            if (value != viewModel.Value)
                            {
                                viewModel.SetValue(value);
                                OnDirty?.Invoke(true);
                            }
                        }
                        else
                        {
                            GUI.Label(cellRect, viewModel.Value);
                        }

                        break;
                    case 4:
                        if (viewModel.Type is ValueKind.List or ValueKind.Dictionary)
                        {
                            if (GUI.Button(cellRect, "Add"))
                            {
                                viewModel.AddChild(new StateTreeViewItem.Model(
                                    _elementId++,
                                    viewModel.Type is ValueKind.List
                                        ? $"{viewModel.Children.Count}"
                                        : "New Key",
                                    viewModel.Children[0].Type,
                                    string.Empty,
                                    editable: viewModel.Parent is { Type: ValueKind.Dictionary }));
                                viewModel.Value = $"Count: {viewModel.Children.Count}";
                                Reload();
                                OnDirty?.Invoke(true);
                            }
                        }
                        else if (viewModel.Editable)
                        {
                            if (GUI.Button(cellRect, "Save"))
                            {
                                viewModel.Editable = false;
                                // Save changed value and update treeview
                                Reload();
                                OnDirty?.Invoke(true);
                            }
                        }
                        else
                        {
                            if (GUI.Button(cellRect, "Edit"))
                            {
                                viewModel.Editable = true;
                                // Make key and type editable
                                // Update displayKey
                                Reload();
                                OnDirty?.Invoke(true);
                            }
                        }

                        break;
                    case 5:
                        if (!(viewModel.Parent is null))
                        {
                            if (GUI.Button(cellRect, "Remove"))
                            {
                                viewModel.Parent.RemoveChild(viewModel);
                                Reload();
                                OnDirty?.Invoke(true);
                            }
                        }

                        break;
                }
            }
        }

        private void AddChildrenRecursive(
            StateTreeViewItem.Model parentModel,
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
                    if (IsExpanded(childModel.Id))
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
