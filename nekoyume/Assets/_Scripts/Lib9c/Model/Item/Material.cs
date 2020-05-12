using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Material : ItemBase
    {
        public new MaterialItemSheet.Row Data { get; }

        public Material(MaterialItemSheet.Row data) : base(data)
        {
            Data = data;
        }

        protected bool Equals(Material other)
        {
            return Data.ItemId.Equals(other.Data.ItemId);
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
