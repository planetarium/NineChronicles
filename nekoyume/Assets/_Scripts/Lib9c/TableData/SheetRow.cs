using System;
using System.Collections.Generic;

namespace Nekoyume.TableData
{
    [Serializable]
    public abstract class SheetRow<T>
    {
        public abstract T Key { get; }

        public abstract void Set(IReadOnlyList<string> fields);

        public virtual void Validate()
        {
        }

        public virtual void EndOfSheetInitialize()
        {
        }

        #region Equals

        private bool Equals(SheetRow<T> other)
        {
            return Key.Equals(other.Key);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SheetRow<T>) obj);
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        #endregion
    }
}
