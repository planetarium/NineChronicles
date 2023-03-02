using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace StateViewer.Editor
{
    public class StateView : TreeView
    {
        private class StateViewItem : TreeViewItem
        {
            public StateTreeElement Data { get; set; }
        }

        private int Id { get; set; }

        private IValue State;

        public int MaxChildren { get; set; } = 10;

        public int ElementsCount => _treeElements.Length;

        private StateTreeElement[] _treeElements;

        public StateView(TreeViewState treeViewState, MultiColumnHeader multiColumnHeader)
            : base(treeViewState, multiColumnHeader)
        {
            rowHeight = EditorGUIUtility.singleLineHeight;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            Reload();
        }

        public void SetData(IValue data)
        {
            // 이 부분에서 IValue를 StateTreeElement로 변환하는 작업을 해야 한다.
            // 트리 구조가 만들어지는 부분이다.
            var (element, _) = MakeElementsRecursive("", data, 0);
            _treeElements = new[] { element };
            Reload();
        }

        private (StateTreeElement element, int currentId)
            MakeElementsRecursive(
                string key,
                IValue data,
                int currentId)
        {
            StateTreeElement e;
            switch (data)
            {
                case Null:
                case Binary:
                case Boolean:
                case Integer:
                case Text:
                    return (
                        new StateTreeElement(
                            currentId++,
                            key,
                            data.Kind.ToString(),
                            data.Inspect(false)),
                        currentId);
                    break;
                case List list:
                {
                    e = new StateTreeElement(
                        currentId++,
                        key,
                        data.Kind.ToString(),
                        $"Count: {list.Count}");
                    var count = math.min(MaxChildren, list.Count);
                    for (var i = 0; i < count; i++)
                    {
                        var item = list[i];
                        StateTreeElement child;
                        (child, currentId) = MakeElementsRecursive(
                            $"[{i}]",
                            item,
                            currentId);
                        e.AddChild(child);
                    }

                    return (e, currentId);
                }
                case Dictionary dict:
                {
                    e = new StateTreeElement(
                        currentId++,
                        key,
                        data.Kind.ToString(),
                        $"Count: {dict.Count}");
                    var count = math.min(MaxChildren, dict.Count);
                    foreach (var pair in dict)
                    {
                        if (count <= 0)
                        {
                            break;
                        }

                        count--;
                        StateTreeElement child;
                        (child, currentId) = MakeElementsRecursive(
                            $"[{pair.Key.Inspect(false)}]",
                            pair.Value,
                            currentId);
                        e.AddChild(child);
                    }

                    return (e, currentId);
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

            if (_treeElements is null)
            {
                return rows;
            }

            foreach (var e in _treeElements)
            {
                var item = CreateStateViewItem(e);
                root.AddChild(item);
                rows.Add(item);
                if (e.Children.Count >= 1)
                {
                    if (IsExpanded(item.id))
                    {
                        AddChildrenRecursive(e, item, rows);
                    }
                    else
                    {
                        item.children = CreateChildListForCollapsedParent();
                    }
                }
            }

            SetupDepthsFromParentsAndChildren(root);
            return rows;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var item = (StateViewItem)args.item;
            var e = item.Data;
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

                switch (columnIndex)
                {
                    case 1:
                        GUI.Label(cellRect, e.Type);
                        break;
                    case 2 when e.Type == ValueKind.List.ToString() ||
                                e.Type == ValueKind.Dictionary.ToString():
                        GUI.Label(cellRect, e.Value);
                        break;
                    case 2:
                        e.Value = GUI.TextField(cellRect, e.Value);
                        break;
                }
            }
        }

        private static StateViewItem CreateStateViewItem(StateTreeElement element) => new()
        {
            id = element.Id,
            displayName = element.Key,
            Data = element,
        };

        private static void AddChildrenRecursive(
            StateTreeElement element,
            TreeViewItem item,
            ICollection<TreeViewItem> rows)
        {
            foreach (var e in element.Children)
            {
                var childItem = CreateStateViewItem(e);
                item.AddChild(childItem);
                rows.Add(childItem);
                if (e.Children.Count >= 1)
                {
                    AddChildrenRecursive(e, childItem, rows);
                }
                else
                {
                    childItem.children = CreateChildListForCollapsedParent();
                }
            }
        }

        private TreeViewItem AsTreeViewItem(IValue value)
        {
            var item = new TreeViewItem();
            if (value is Bencodex.Types.List list)
            {
                item.children = AsTreeViewItems(list).Select((child, index) =>
                {
                    child.displayName = index.ToString();
                    return child;
                }).ToList();
            }
            else if (value is Bencodex.Types.Dictionary dictionary)
            {
                item.children = dictionary.Select(AsTreeViewItem).ToList();
            }
            else
            {
                item.displayName = (value is Bencodex.Types.Boolean boolean
                    ? boolean.Value.ToString()
                    : value.ToString());
            }

            return item;
        }

        private TreeViewItem AsTreeViewItem(KeyValuePair<IKey, IValue> pair)
        {
            var item = new TreeViewItem();
            item.displayName = pair.Key.ToString();
            if (pair.Value is Bencodex.Types.List list)
            {
                item.children = AsTreeViewItems(list).Select((child, index) =>
                {
                    child.displayName = index.ToString();
                    return child;
                }).ToList();
            }
            else if (pair.Value is Bencodex.Types.Dictionary dictionary)
            {
                item.children = dictionary.Select(AsTreeViewItem).ToList();
            }
            else
            {
                item.displayName += " = " + (pair.Value is Bencodex.Types.Boolean boolean
                    ? boolean.Value.ToString()
                    : pair.Value.ToString());
            }

            return item;
        }

        private IEnumerable<TreeViewItem> AsTreeViewItems(Bencodex.Types.List list)
        {
            return list.Select(AsTreeViewItem);
        }

        private void SetupDepth(TreeViewItem item, int depth)
        {
            item.depth = depth;
            item.id = Id++;
            if (item.hasChildren)
            {
                foreach (var child in item.children)
                {
                    SetupDepth(child, depth + 1);
                }
            }
        }
    }
}
