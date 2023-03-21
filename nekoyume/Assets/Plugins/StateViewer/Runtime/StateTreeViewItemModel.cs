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
        private Dictionary<string, string>? _reversedSerializeKeys;

        /// <summary>
        /// <see cref="UnityEditor.IMGUI.Controls.TreeViewItem"/> has unique id.
        /// </summary>
        public int TreeViewItemId { get; set; }

        public IValue Value { get; }

        public string ValueContent { get; set; }

        public StateTreeViewItemModel? Parent { get; }

        public int? Index { get; }

        public IKey? Key { get; }

        public string IndexOrKeyContent { get; private set; }

        public string AliasContent { get; }

        public List<StateTreeViewItemModel> Children { get; } = new();

        public StateTreeViewItemModel(
            int treeViewItemId,
            IValue data,
            StateTreeViewItemModel? parent = null,
            int? index = null,
            IKey? key = null,
            string? alias = null) : this(data, parent, index, key, alias)
        {
            TreeViewItemId = treeViewItemId;
        }

        public StateTreeViewItemModel(
            IValue data,
            StateTreeViewItemModel? parent = null,
            int? index = null,
            IKey? key = null,
            string? alias = null)
        {
            Value = data;
            switch (Value.Kind)
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
                    Children.AddRange(list.Select((childValue, i) =>
                        new StateTreeViewItemModel(
                            childValue,
                            parent: this,
                            index: i)));
                    break;
                case ValueKind.Dictionary:
                    ValueContent = ParseToString(Value);
                    var dict = (Dictionary)Value;
                    Children.AddRange(dict.Select(pair =>
                        new StateTreeViewItemModel(
                            pair.Value,
                            parent: this,
                            key: pair.Key)));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            Parent = parent;
            Index = index;
            Key = key;
            IndexOrKeyContent = Index is null
                ? Key is null
                    ? string.Empty
                    : ParseToString(Key)
                : Index.ToString();
            AliasContent = alias ?? GetAlias(IndexOrKeyContent);
        }

        public IValue Serialize()
        {
            switch (Value.Kind)
            {
                case ValueKind.Null:
                case ValueKind.Boolean:
                case ValueKind.Binary:
                case ValueKind.Integer:
                case ValueKind.Text:
                    return ParseToValue(ValueContent, Value.Kind);
                case ValueKind.List:
                    return new List(Children.Select(child =>
                        child.Serialize()));
                case ValueKind.Dictionary:
                    return new Dictionary(Children.Select(child =>
                    {
                        if (child.Key is null)
                        {
                            throw new Exception("Key of child is null.");
                        }

                        return new KeyValuePair<IKey, IValue>(
                            (IKey)ParseToValue(child.IndexOrKeyContent, child.Key.Kind),
                            child.Serialize());
                    }));
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void SetIndexOrKeyContent(string value)
        {
            IndexOrKeyContent = value;
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
                    var inner = string.Join(
                        string.Empty,
                        ((Binary)value).ByteArray
                        .Select(b => b.ToString("X2")));
                    return $"0x{inner}";
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
                    var hex = value.StartsWith("0x")
                        ? value[2..]
                        : value;
                    return new Binary(Enumerable.Range(0, hex.Length)
                        .Where(x => x % 2 == 0)
                        .Select(x => Convert.ToByte(hex[x..(x + 2)], 16))
                        .ToArray());
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

        private string GetAlias(string key)
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

        // public void AddChild(StateTreeViewItemModel? child)
        // {
        //     if (child is null)
        //     {
        //         return;
        //     }
        //
        //     child.Parent?.RemoveChild(child);
        //     Children.Add(child);
        //     child.Parent = this;
        // }
        //
        // public void RemoveChild(StateTreeViewItemModel? child)
        // {
        //     if (child is null)
        //     {
        //         return;
        //     }
        //
        //     if (Children.Contains(child))
        //     {
        //         Children.Remove(child);
        //         child.Parent = null;
        //     }
        // }

        // private static IValue? Convert(string value, bool bom = true)
        // {
        //     var sanitized = value.Replace("[", "").Replace("]", "");
        //     var converter = new Bencodex.Json.BencodexJsonConverter();
        //     var serializerOptions = new JsonSerializerOptions();
        //     var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(
        //         bom ? $"\"\\uFEFF{sanitized}\"" : $"\"{sanitized}\""));
        //     return converter.Read(ref reader, typeof(Binary), serializerOptions);
        // }
    }
}
