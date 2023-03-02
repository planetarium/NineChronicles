using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace StateViewer.Editor
{
    public class StateView : TreeView
    {
        public StateView(TreeViewState treeViewState)
            : base(treeViewState)
        {
            // columnIndexForTreeFoldouts = 2;
            rowHeight = EditorGUIUtility.singleLineHeight;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            // extraSpaceBeforeIconAndLabel = EditorGUIUtility.singleLineHeight; // kToggleWidth;
            Reload();
        }

        private int Id { get; set; }

        private IValue State;

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { displayName = "Root" };

            root.AddChild(
                State is null
                    ? new TreeViewItem { displayName = "empty" }
                    : AsTreeViewItem(State));

            Id = 0;
            SetupDepth(root, -1);

            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            return base.BuildRows(root);
        }

        public void SetState(IValue state)
        {
            State = state;
            Reload();
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
