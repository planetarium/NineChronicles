using System;
using System.Collections.Generic;
using Bencodex.Types;

namespace StateViewer.Editor
{
    [Serializable]
    public class StateTreeElement
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public StateTreeElement Parent { get; set; }
        public List<StateTreeElement> Children { get; } = new();

        public StateTreeElement(int id, string key, IValue data)
        {
            Id = id;
            Key = key;
            Type = data.Kind.ToString();
            Value = data.Kind == ValueKind.Null ? "Null" : data.ToString();
        }

        public StateTreeElement(int id, string key, string type, string value)
        {
            Id = id;
            Key = key;
            Type = type;
            Value = value;
        }

        public void AddChild(StateTreeElement child)
        {
            if (child is null)
            {
                return;
            }

            child.Parent?.RemoveChild(child);
            Children.Add(child);
            child.Parent = this;
        }

        public void RemoveChild(StateTreeElement child)
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
    }
}
