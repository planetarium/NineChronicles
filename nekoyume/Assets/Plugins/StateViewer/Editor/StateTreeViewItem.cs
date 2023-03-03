using System;
using System.Collections.Generic;
using Bencodex.Types;
using Nekoyume.Model.State;
using UnityEditor.IMGUI.Controls;

namespace StateViewer.Editor
{
    [Serializable]
    public sealed class StateTreeViewItem : TreeViewItem
    {
        public class Model : IState
        {
            public int Id { get; }
            public string Key { get; }
            public string DisplayKey { get; }
            public string Type { get; }
            public string Value { get; private set; }
            public bool Editable { get; }
            public Model Parent { get; private set; }
            public List<Model> Children { get; } = new();

            public Model(int id, string key, string displayKey, string type, string value,
                bool editable = true)
            {
                Id = id;
                Key = key;
                DisplayKey = displayKey;
                Type = type;
                Value = value;
                Editable = editable;
            }

            public void SetValue(string value)
            {
                Value = value;
            }

            public void AddChild(Model child)
            {
                if (child is null)
                {
                    return;
                }

                child.Parent?.RemoveChild(child);
                Children.Add(child);
                child.Parent = this;
            }

            public void RemoveChild(Model child)
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

            public IValue Serialize()
            {
                throw new NotImplementedException();
            }
        }

        public Model ViewModel { get; }

        public StateTreeViewItem(Model viewModel) : base(viewModel.Id)
        {
            displayName = viewModel.Key;
            ViewModel = viewModel;
        }
    }
}
