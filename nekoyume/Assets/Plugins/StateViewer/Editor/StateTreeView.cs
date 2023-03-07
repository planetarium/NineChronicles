using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Bencodex.Types;
using Lib9c;
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
        private readonly Dictionary<string, string> _reversedSerializeKeys = new();

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

            if (_reversedSerializeKeys.Count == 0)
            {
                ReverseSerializedKeys();
            }
        }

        private void ReverseSerializedKeys()
        {
            foreach (var field in typeof(SerializeKeys).GetFields(
                         BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy
                     )
                    )
            {
                if (field.IsLiteral && !field.IsInitOnly)
                {
                    _reversedSerializeKeys.Add(field.GetRawConstantValue().ToString(), field.Name);
                }
            }

            Debug.LogFormat("SerializeKeys are reversed: {0}", _reversedSerializeKeys.Count);
        }

        private string GetReversedKey(string key)
        {
            return _reversedSerializeKeys.ContainsKey(key)
                ? _reversedSerializeKeys[key]
                : key;
        }


        public void SetData(Address addr, IValue data)
        {
            _addr = addr;
            var (model, _) = MakeItemModelRecursive("", data, 1);
            _itemModels = new[] { model };
            Reload();
            OnDirty?.Invoke(false);
        }

        private static string Convert(IValue value)
        {
            var converter = new Bencodex.Json.BencodexJsonConverter();
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

        private (StateTreeViewItem.Model viewModel, int currentId)
            MakeItemModelRecursive(
                string key,
                IValue data,
                int currentId)
        {
            StateTreeViewItem.Model viewModel;
            switch (data)
            {
                case Null:
                case Binary:
                case Boolean:
                case Integer:
                case Text:
                    viewModel = new StateTreeViewItem.Model(
                        currentId++,
                        key,
                        GetReversedKey(key),
                        data.Kind.ToString(),
                        Convert(data));

                    return (viewModel, currentId);
                case List list:
                {
                    viewModel = new StateTreeViewItem.Model(
                        currentId++,
                        key,
                        GetReversedKey(key),
                        data.Kind.ToString(),
                        $"Count: {list.Count}");
                    for (var i = 0; i < list.Count; i++)
                    {
                        var item = list[i];
                        StateTreeViewItem.Model childViewModel;
                        (childViewModel, currentId) = MakeItemModelRecursive(
                            $"[{i.ToString()}]",
                            item,
                            currentId);
                        viewModel.AddChild(childViewModel);
                    }

                    return (viewModel, currentId);
                }
                case Dictionary dict:
                {
                    viewModel = new StateTreeViewItem.Model(
                        currentId++,
                        key,
                        GetReversedKey(key),
                        data.Kind.ToString(),
                        $"Count: {dict.Count}");
                    foreach (var pair in dict)
                    {
                        StateTreeViewItem.Model childViewModel;
                        (childViewModel, currentId) = MakeItemModelRecursive(
                            $"[{Convert(pair.Key)}]",
                            pair.Value,
                            currentId);
                        viewModel.AddChild(childViewModel);
                    }

                    return (viewModel, currentId);
                }
                default:
                    Debug.LogError($"data type {data.GetType()} is not supported.");
                    return (null, currentId);
            }
        }

        protected override TreeViewItem BuildRoot() => new()
        {
            id = 0,
            depth = -1,
            displayName = "Root",
        };

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
                if (columnIndex == 0)
                {
                    base.RowGUI(args);
                    continue;
                }

                var cellRect = args.GetCellRect(i);
                CenterRectUsingSingleLineHeight(ref cellRect);

                var viewModel = item.ViewModel;
                switch (columnIndex)
                {
                    case 1:
                        GUI.Label(cellRect, viewModel.DisplayKey);
                        break;
                    case 2:
                        GUI.Label(cellRect, viewModel.Type);
                        break;
                    case 3 when viewModel.Type == ValueKind.List.ToString() ||
                                viewModel.Type == ValueKind.Dictionary.ToString():
                        GUI.Label(cellRect, viewModel.Value);
                        break;
                    case 3:
                        var value = GUI.TextField(cellRect, viewModel.Value);
                        if (value != viewModel.Value)
                        {
                            viewModel.SetValue(value);
                            OnDirty?.Invoke(true);
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
