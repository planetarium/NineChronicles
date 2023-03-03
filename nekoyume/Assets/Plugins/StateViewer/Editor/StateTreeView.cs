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

            Reload();
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

        public void SetData(Address addr, IValue data)
        {
            // 이 부분에서 IValue를 StateTreeElement로 변환하는 작업을 해야 한다.
            // 트리 구조가 만들어지는 부분이다.
            _addr = addr;
            var (model, _) = MakeItemModelRecursive("", data, 1);
            _itemModels = new[] { model };
            Reload();
            OnDirty?.Invoke(false);
        }

        private string GetReversedKey(string key)
        {
            return _reversedSerializeKeys.ContainsKey(key)
                ? _reversedSerializeKeys[key]
                : key;
        }

        private (StateTreeViewItem.Model viewModel, int currentId)
            MakeItemModelRecursive(
                string key,
                IValue data,
                int currentId)
        {
            var serializerOption = new JsonSerializerOptions
            {
                WriteIndented = false,
            };
            var converter = new Bencodex.Json.BencodexJsonConverter();
            StateTreeViewItem.Model viewModel;
            switch (data)
            {
                case Null:
                case Binary:
                case Boolean:
                case Integer:
                case Text:
                {
                    using var stream = new MemoryStream();
                    var writerOption = new JsonWriterOptions
                    {
                        Indented = false,
                    };
                    var writer = new Utf8JsonWriter(stream, writerOption);
                    converter.Write(writer, data, serializerOption);
                    viewModel = new StateTreeViewItem.Model(
                        currentId++,
                        key,
                        GetReversedKey(key),
                        data.Kind.ToString(),
                        Encoding.UTF8.GetString(stream.ToArray()));

                    return (viewModel, currentId);
                }
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
                            i.ToString(),
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
                        using var stream = new MemoryStream();
                        var writerOption = new JsonWriterOptions
                        {
                            Indented = false,
                        };
                        var writer = new Utf8JsonWriter(stream, writerOption);
                        converter.Write(writer, pair.Key, serializerOption);
                        StateTreeViewItem.Model childViewModel;
                        (childViewModel, currentId) = MakeItemModelRecursive(
                            Encoding.UTF8.GetString(stream.ToArray()),
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
            var rows = GetRows() ?? new List<TreeViewItem>();
            rows.Clear();

            if (_itemModels is null)
            {
                return rows;
            }

            foreach (var viewModel in _itemModels)
            {
                var itemView = new StateTreeViewItem(viewModel);
                root.AddChild(itemView);
                rows.Add(itemView);
                if (viewModel.Children.Count >= 1)
                {
                    if (IsExpanded(itemView.id))
                    {
                        AddChildrenRecursive(viewModel, itemView, rows);
                        continue;
                    }

                    itemView.children = CreateChildListForCollapsedParent();
                }
            }

            SetupDepthsFromParentsAndChildren(root);
            return rows;
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

        private static void AddChildrenRecursive(
            StateTreeViewItem.Model itemModel,
            TreeViewItem item,
            ICollection<TreeViewItem> rows)
        {
            foreach (var data in itemModel.Children)
            {
                var itemView = new StateTreeViewItem(data);
                item.AddChild(itemView);
                rows.Add(itemView);
                if (data.Children.Count >= 1)
                {
                    AddChildrenRecursive(data, itemView, rows);
                }
                else
                {
                    itemView.children = CreateChildListForCollapsedParent();
                }
            }
        }
    }
}
