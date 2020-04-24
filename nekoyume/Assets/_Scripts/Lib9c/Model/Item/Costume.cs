using System;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Costume : ItemBase
    {
        public new CostumeItemSheet.Row Data { get; }

        public Costume(CostumeItemSheet.Row data) : base(data)
        {
            Data = data;
        }

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
