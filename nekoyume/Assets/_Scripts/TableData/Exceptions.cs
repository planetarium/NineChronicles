using System;

namespace Nekoyume.TableData
{
    public class SheetRowColumnException : Exception
    {
        public SheetRowColumnException(string message) : base(message)
        {
        }
    }
    
    public class SheetRowValidateException : Exception
    {
        public SheetRowValidateException(string message) : base(message)
        {
        }
    }

    public class SheetRowNotFoundException : Exception
    {
        public SheetRowNotFoundException(string sheetName, string key) : this(sheetName, "Key", key)
        {
        }

        public SheetRowNotFoundException(string sheetName, string condition, string value) : base(
            $"{sheetName}: {condition} - {value}")
        {
        }
    }
}
