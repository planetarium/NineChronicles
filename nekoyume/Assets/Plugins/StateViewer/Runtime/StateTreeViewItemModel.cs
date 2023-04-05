#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;
using System.Reflection;
using Bencodex.Types;
using Lib9c;
using Nekoyume.Model.State;
using Boolean = Bencodex.Types.Boolean;

namespace StateViewer.Runtime
{
    public class StateTreeViewItemModel : IState
    {
        private static Dictionary<string, string>? _reversedSerializeKeys;

        private IValue _originValue;
        private readonly List<StateTreeViewItemModel> _children;

        /// <summary>
        /// <see cref="UnityEditor.IMGUI.Controls.TreeViewItem"/> has unique id.
        /// </summary>
        public int TreeViewItemId { get; private set; }

        public int TreeViewItemIdPrev { get; private set; }

        public IValue Value { get; private set; }

        public ValueKind ValueType => Value.Kind;

        public string ValueContent { get; private set; }

        public StateTreeViewItemModel? Parent { get; private set; }

        public int? Index { get; private set; }

        public IKey? Key { get; private set; }

        public string IndexOrKeyContent { get; private set; }

        public string AliasContent { get; private set; }

        public IReadOnlyList<StateTreeViewItemModel> Children => _children;

        public IReadOnlyList<StateTreeViewItemModel>? Siblings => Parent?.Children;

#pragma warning disable CS8618
        public StateTreeViewItemModel(
#pragma warning restore CS8618
            IValue data,
            StateTreeViewItemModel? parent = null,
            int? index = null,
            IKey? key = null,
            string? alias = null)
        {
            _originValue = data;
            _children = new List<StateTreeViewItemModel>();
            SetValue(_originValue);

            Parent = parent;
            var indexOrKeyContent = index is null
                ? key is null
                    ? null
                    : ParseToString(key)
                : index.ToString();
            if (Parent is null && indexOrKeyContent is not null)
            {
                throw new Exception("index or key parameter is not null when parent is null.");
            }

            SetIndexOrKeyContent(indexOrKeyContent, alias);
        }

        /// <summary>
        /// Returns the serialized value of the current state.
        /// This method replace the original value.
        /// </summary>
        public IValue Serialize()
        {
            switch (Value.Kind)
            {
                case ValueKind.Null:
                case ValueKind.Boolean:
                case ValueKind.Binary:
                case ValueKind.Integer:
                case ValueKind.Text:
                    _originValue = ParseToValue(ValueContent, Value.Kind);
                    break;
                case ValueKind.List:
                    _originValue = new List(Children
                        .OrderBy(child => child.Index)
                        .Select(child => child.Serialize()));
                    break;
                case ValueKind.Dictionary:
                    _originValue = new Dictionary(Children.Select(child =>
                    {
                        if (child.Key is null)
                        {
                            throw new Exception("Key of child is null.");
                        }

                        return new KeyValuePair<IKey, IValue>(
                            child.Key,
                            child.Serialize());
                    }));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return _originValue;
        }

        public int SetTreeViewItemIdRecursive(int treeViewItemId, bool alsoSetPrev = false)
        {
            TreeViewItemIdPrev = alsoSetPrev ? treeViewItemId : TreeViewItemId;
            TreeViewItemId = treeViewItemId;
            foreach (var child in Children)
            {
                treeViewItemId = child.SetTreeViewItemIdRecursive(treeViewItemId + 1);
            }

            return treeViewItemId;
        }

        public void SetValue(IValue value)
        {
            Value = value;
            _children.Clear();
            switch (ValueType)
            {
                case ValueKind.Null:
                case ValueKind.Boolean:
                case ValueKind.Integer:
                case ValueKind.Binary:
                case ValueKind.Text:
                    ValueContent = ParseToString(Value);
                    break;
                case ValueKind.List:
                    ValueContent = ParseToString(Value);
                    var list = (List)Value;
                    _children.AddRange(list.Select((childValue, i) =>
                        new StateTreeViewItemModel(
                            childValue,
                            parent: this,
                            index: i)));
                    break;
                case ValueKind.Dictionary:
                    ValueContent = ParseToString(Value);
                    var dict = (Dictionary)Value;
                    _children.AddRange(dict.Select(pair =>
                        new StateTreeViewItemModel(
                            pair.Value,
                            parent: this,
                            key: pair.Key)));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetValueKindContent(ValueKind valueKind)
        {
            if (valueKind == ValueType)
            {
                return;
            }

            if (valueKind == _originValue.Kind)
            {
                SetValue(_originValue);
                return;
            }

            switch (valueKind)
            {
                case ValueKind.Null:
                    SetValue(Null.Value);
                    break;
                case ValueKind.Boolean:
                    SetValue(new Boolean());
                    break;
                case ValueKind.Integer:
                    SetValue(new Integer());
                    break;
                case ValueKind.Binary:
                    SetValue(new Binary());
                    break;
                case ValueKind.Text:
                    SetValue(new Text());
                    break;
                case ValueKind.List:
                    SetValue(List.Empty);
                    break;
                case ValueKind.Dictionary:
                    SetValue(Dictionary.Empty);
                    break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public void SetValueContent(string valueContent, bool updateValue = true)
        {
            if (updateValue)
            {
                try
                {
                    var value = ParseToValue(valueContent, ValueType);
                    SetValue(value);
                    return;
                }
                catch
                {
                    // ignored.
                }
            }

            ValueContent = valueContent;
            // TODO: We need a way for the user to recognize that the Value hasn't changed.
        }

        // FIXME: Separate alias.
        public void SetIndexOrKeyContent(
            string? indexOrKey,
            string? alias = null)
        {
            if (indexOrKey is null)
            {
                IndexOrKeyContent = string.Empty;
                AliasContent = alias ?? string.Empty;
                return;
            }

            if (Parent is null)
            {
                throw new Exception("Parent is null.");
            }

            IndexOrKeyContent = indexOrKey;
            alias ??= GetAlias(indexOrKey);
            if (Parent.ValueType == ValueKind.List)
            {
                Index = int.Parse(indexOrKey);
                AliasContent = alias;
                return;
            }

            if (Parent.ValueType != ValueKind.Dictionary)
            {
                throw new Exception("Parent's value type is not list or dictionary.");
            }

            ValueKind keyType;
            try
            {
                Key = (IKey)ParseToValue(IndexOrKeyContent, ValueKind.Binary);
                keyType = ValueKind.Binary;
            }
            catch
            {
                Key = (IKey)ParseToValue(IndexOrKeyContent, ValueKind.Text);
                keyType = ValueKind.Text;
            }

            AliasContent = string.IsNullOrEmpty(alias)
                ? $"({keyType})"
                : $"({keyType}){alias}";
        }

        public void AddChild(IValue child)
        {
            switch (ValueType)
            {
                case ValueKind.List:
                    var list = ((List)Value).Add(child);
                    Value = list;
                    _children.Add(new StateTreeViewItemModel(
                        child,
                        parent: this,
                        index: list.Count - 1));
                    break;
                case ValueKind.Dictionary:
                    var dict = (Dictionary)Value;
                    var key = dict.Count.ToString();
                    dict = dict.Add(key, child);
                    Value = dict;
                    _children.Add(new StateTreeViewItemModel(
                        child,
                        parent: this,
                        key: (IKey)ParseToValue(key, ValueKind.Text)));
                    break;
                default:
                    return;
            }

            ValueContent = ParseToString(Value);
        }

        public void RemoveChild(StateTreeViewItemModel? child)
        {
            if (child is null ||
                !Children.Contains(child))
            {
                return;
            }

            switch (ValueType)
            {
                case ValueKind.List:
                    IImmutableList<IValue> list = (List)Value;
                    list = list.RemoveAt(child.Index!.Value);
                    Value = new List(list);
                    _children.Remove(child);
                    for (var i = 0; i < Children.Count; i++)
                    {
                        var c = Children[i];
                        c.SetIndexOrKeyContent(i.ToString());
                    }

                    break;
                case ValueKind.Dictionary:
                    var dict = (Dictionary)Value;
                    Value = new Dictionary(dict.Remove(child.Key!));
                    _children.Remove(child);
                    break;
                default:
                    return;
            }

            child.Parent = null;
            ValueContent = ParseToString(Value);
        }

        public int FindIdRecursive(int prevId)
        {
            if (TreeViewItemIdPrev == prevId)
            {
                return TreeViewItemId;
            }

            foreach (var child in Children)
            {
                if (child is null)
                {
                    continue;
                }

                var id = child.FindIdRecursive(prevId);
                if (id != -1)
                {
                    return id;
                }
            }

            return -1;
        }

        private static string ParseToString(IValue value)
        {
            switch (value.Kind)
            {
                case ValueKind.Null:
                    return "null";
                case ValueKind.Boolean:
                    return ((Boolean)value).Inspect(false);
                case ValueKind.Integer:
                    return ((Integer)value).Inspect(false);
                case ValueKind.Binary:
                    return $"0x{((Binary)value).ToHex()}";
                case ValueKind.Text:
                    return (Text)value;
                case ValueKind.List:
                    return $"[{((List)value).Count}]";
                case ValueKind.Dictionary:
                    return $"{{{((Dictionary)value).Count}}}";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static IValue ParseToValue(string value, ValueKind valueKind)
        {
            switch (valueKind)
            {
                case ValueKind.Null:
                    return Null.Value;
                case ValueKind.Boolean:
                    return new Boolean(bool.Parse(value));
                case ValueKind.Binary:
                    if (value.StartsWith("0x"))
                    {
                        value = value[2..];
                    }

                    return Binary.FromHex(value);
                case ValueKind.Integer:
                    return new Integer(BigInteger.Parse(value));
                case ValueKind.Text:
                    return new Text(value);
                case ValueKind.List:
                case ValueKind.Dictionary:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static string GetAlias(string key)
        {
            _reversedSerializeKeys ??= typeof(SerializeKeys)
                .GetFields(
                    BindingFlags.Public |
                    BindingFlags.Static)
                .ToDictionary(
                    field => field.GetRawConstantValue().ToString(),
                    field => field.Name);

            return _reversedSerializeKeys.ContainsKey(key)
                ? _reversedSerializeKeys[key]
                : string.Empty;
        }
    }
}
