using System;
using System.Collections.Generic;
using Bencodex.Types;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public abstract class ItemBase : IState
    {
        public ItemSheet.Row Data { get; }

        protected ItemBase(ItemSheet.Row data)
        {
            Data = data;
        }

        protected bool Equals(ItemBase other)
        {
            return Data.Id == other.Data.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ItemBase)obj);
        }

        public override int GetHashCode()
        {
            return Data != null ? Data.GetHashCode() : 0;
        }

        public virtual IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text)"data"] = Data.Serialize(),
            });
    }
}
