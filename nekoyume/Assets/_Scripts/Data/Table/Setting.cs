using System.Collections.Generic;
using Nekoyume.Action;
using UnityEngine;

namespace Nekoyume.Data.Table
{
    // simple key-value data
    public class Setting : Row
    {
        public string Key = "";
        public string Value = "";

        public int GetValueAsInt()
        {
            int value = 0;
            int.TryParse(Value, out value);
            return value;
        }

        public float GetValueAsFloat()
        {
            float value = 0;
            float.TryParse(Value, out value);
            return value;
        }
    }
}
