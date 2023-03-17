using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Bencodex.Types;
using Lib9c;
using Nekoyume.Model.State;
using UnityEngine;

namespace StateViewer.Runtime
{
    public class StateTreeViewItemModel : IState
    {
        private readonly Dictionary<string, string> _reversedSerializeKeys = new();

        /// <summary>
        /// <see cref="UnityEditor.IMGUI.Controls.TreeViewItem"/> has unique id.
        /// </summary>
        public int TreeViewItemId { get; }

        /// <summary>
        /// <see cref="Bencodex.Types.Dictionary"/> or <see cref="Bencodex.Types.List"/>
        /// element's key or index.
        /// </summary>
        public string Key { get; private set; }
        public ValueKind KeyType { get; }
        public string DisplayKey { get; }
        public ValueKind ValueType { get; set; }
        public string Value { get; set; }
        public bool Editable { get; set; }
        public StateTreeViewItemModel Parent { get; private set; }
        public List<StateTreeViewItemModel> Children { get; } = new();

        public StateTreeViewItemModel(
            int treeViewItemId,
            ValueKind keyType,
            string key,
            ValueKind valueType,
            string value,
            bool editable = true)
        {
            if (_reversedSerializeKeys.Count == 0)
            {
                ReverseSerializedKeys();
            }

            TreeViewItemId = treeViewItemId;
            KeyType = keyType;
            Key = key;
            DisplayKey = $"[{GetReversedKey(key)}]";
            ValueType = valueType;
            Value = value;
            Editable = editable;
        }

        private void ReverseSerializedKeys()
        {
            foreach (var field in typeof(SerializeKeys).GetFields(
                         BindingFlags.Public | BindingFlags.Static |
                         BindingFlags.FlattenHierarchy
                     )
                    )
            {
                if (field.IsLiteral && !field.IsInitOnly)
                {
                    _reversedSerializeKeys.Add(field.GetRawConstantValue().ToString(),
                        field.Name);
                }
            }
        }

        private string GetReversedKey(string key)
        {
            return _reversedSerializeKeys.ContainsKey(key)
                ? _reversedSerializeKeys[key]
                : key;
        }

        public void SetKey(string key)
        {
            Key = key;
        }

        public void SetValue(string value)
        {
            Value = value;
        }

        public void AddChild(StateTreeViewItemModel? child)
        {
            if (child is null)
            {
                return;
            }

            child.Parent?.RemoveChild(child);
            Children.Add(child);
            child.Parent = this;
        }

        public void RemoveChild(StateTreeViewItemModel? child)
        {
            if (child is null)
            {
                return;
            }

            if (Children.Contains(child))
            {
                Children.Remove(child);
                child.Parent = null;
            }
        }

        private static IValue? Convert(string value, bool bom = true)
        {
            var sanitized = value.Replace("[", "").Replace("]", "");
            var converter = new Bencodex.Json.BencodexJsonConverter();
            var serializerOptions = new JsonSerializerOptions();
            var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(
                bom ? $"\"\\uFEFF{sanitized}\"" : $"\"{sanitized}\""));
            return converter.Read(ref reader, typeof(Binary), serializerOptions);
        }

        public IValue Serialize()
        {
            switch (ValueType)
            {
                case ValueKind.Null:
                    return Null.Value;
                case ValueKind.Boolean:
                    return Value.Serialize();
                case ValueKind.Binary:
                case ValueKind.Integer:
                    return Convert(Value, false);
                case ValueKind.Text:
                    return Convert(Value);
                case ValueKind.List:
                    return new List(Children.Select(child =>
                        child.Serialize()));
                case ValueKind.Dictionary:
                {
                    IKey? key;
                    if (KeyType is ValueKind.Binary)
                    {
                        key = (IKey?)Convert(Key, bom: false);
                    }
                    else if (KeyType is ValueKind.Text)
                    {
                        key = (Text)Key;
                    }
                    else
                    {
                        throw new NotSupportedException($"KeyType{KeyType} is not supported.");
                    }

                    Debug.LogFormat(key.ToString());

                    return new Dictionary(Children.Aggregate(
                        ImmutableDictionary<IKey, IValue>.Empty,
                        (current, child) => current.SetItem(
                            (IKey)Convert(child.Key, bom: child.KeyType == ValueKind.Text),
                            child.Serialize()))
                    );
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
