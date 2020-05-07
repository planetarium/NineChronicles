using System;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Costume : ItemBase
    {
        public bool equipped = false;

        public new CostumeItemSheet.Row Data { get; }

        public Costume(CostumeItemSheet.Row data) : base(data)
        {
            Data = data;
        }

        public override IValue Serialize() =>
            new Dictionary(new Dictionary<IKey, IValue>
            {
                [(Text) "equipped"] = equipped.Serialize(),
            }.Union((Dictionary) base.Serialize()));

        protected bool Equals(Material other)
        {
            return Data.Id == other.Data.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Material) obj);
        }

        public override int GetHashCode()
        {
            return (Data != null ? Data.GetHashCode() : 0);
        }
    }
}
