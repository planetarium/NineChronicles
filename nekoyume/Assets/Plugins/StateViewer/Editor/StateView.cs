using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
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

        private StateTreeElement[] _treeElements;

        public StateView(TreeViewState treeViewState, MultiColumnHeader multiColumnHeader)
            : base(treeViewState, multiColumnHeader)
        {
            // columnIndexForTreeFoldouts = 2;
            rowHeight = EditorGUIUtility.singleLineHeight;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            // extraSpaceBeforeIconAndLabel = EditorGUIUtility.singleLineHeight; // kToggleWidth;
            Reload();
        }

        public void SetData(IValue data)
        {
            // 이 부분에서 IValue를 StateTreeElement로 변환하는 작업을 해야 한다.
            // 트리 구조가 만들어지는 부분이다.

            var result = new List<StateTreeElement>();
            MakeElementsRecursive("", data, result, 0);
            _treeElements = result.ToArray();
            Reload();
        }

        private static (List<StateTreeElement> elements, int currentId)
            MakeElementsRecursive(
                string key,
                IValue data,
                List<StateTreeElement> elements,
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
                    e = new StateTreeElement(
                        currentId++,
                        key,
                        data.Kind.ToString(),
                        data.Inspect(false));
                    break;
                case List list:
                {
                    e = new StateTreeElement(
                        currentId++,
                        key,
                        data.Kind.ToString(),
                        $"Count: {list.Count}");
                    for (var i = 0; i < list.Count; i++)
                    {
                        var item = list[i];
                        var children = new List<StateTreeElement>();
                        (children, currentId) =
                            MakeElementsRecursive($"[{i}]", item, children, currentId);
                        foreach (var child in children)
                        {
                            e.AddChild(child);
                        }
                    }

                    break;
                }
                case Dictionary dict:
                {
                    e = new StateTreeElement(
                        currentId++,
                        key,
                        data.Kind.ToString(),
                        $"Count: {dict.Count}");
                    foreach (var pair in dict)
                    {
                        var children = new List<StateTreeElement>();
                        (children, currentId) = MakeElementsRecursive(
                            $"[{pair.Key.Inspect(false)}]",
                            pair.Value,
                            children,
                            currentId);
                        foreach (var child in children)
                        {
                            e.AddChild(child);
                        }
                    }

                    break;
                }
                default:
                    Debug.LogError($"data type {data.GetType()} is not supported.");
                    return (elements, currentId);
            }

            elements.Add(e);
            return (elements, currentId);
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
                var cellRect = args.GetCellRect(i);
                CenterRectUsingSingleLineHeight(ref cellRect);
                var columnIndex = args.GetColumn(i);
                if (columnIndex == 0)
                {
                    base.RowGUI(args);
                }
                else if (columnIndex == 1)
                {
                    GUI.Box(cellRect, e.Type);
                }
                else if (columnIndex == 2)
                {
                    if (e.Type == ValueKind.List.ToString() ||
                        e.Type == ValueKind.Dictionary.ToString())
                    {
                        GUI.Box(cellRect, e.Value);
                    }
                    else
                    {
                        e.Value = GUI.TextField(cellRect, e.Value);
                    }
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
