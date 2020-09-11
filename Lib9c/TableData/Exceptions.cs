using System;
using System.Globalization;
using System.Runtime.Serialization;

namespace Nekoyume.TableData
{
    [Serializable]
    public class SheetRowColumnException : Exception
    {
        public SheetRowColumnException(string message) : base(message)
        {
        }

        protected SheetRowColumnException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
    
    public class SheetRowValidateException : Exception
    {
        public SheetRowValidateException(string message) : base(message)
        {
        }
    }

    [Serializable]
    public class SheetRowNotFoundException : Exception
    {
        public SheetRowNotFoundException(string sheetName, int intKey)
            : this(sheetName, intKey.ToString(CultureInfo.InvariantCulture))
        {
        }

        public SheetRowNotFoundException(string sheetName, string key) : this(sheetName, "Key", key)
        {
        }

        public SheetRowNotFoundException(string sheetName, string condition, string value) : base(
            $"{sheetName}: {condition} - {value}")
        {
        }

        protected SheetRowNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
