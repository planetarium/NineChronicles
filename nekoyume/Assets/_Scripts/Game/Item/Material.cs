using System;
using System.Collections.Generic;
using System.Text;
using Nekoyume.Data;
using Nekoyume.EnumType;
using Nekoyume.TableData;

namespace Nekoyume.Game.Item
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
