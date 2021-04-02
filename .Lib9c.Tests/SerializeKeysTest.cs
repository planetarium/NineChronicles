namespace Lib9c.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Xunit;

    public class SerializeKeysTest
    {
        [Fact]
        public void Keys_Duplicate()
        {
            Type type = typeof(SerializeKeys);
            FieldInfo[] fields = type.GetFields(BindingFlags.Public
                           | BindingFlags.Static
                           | BindingFlags.FlattenHierarchy);
            Dictionary<string, string> keyMap = new Dictionary<string, string>();
            foreach (var info in fields.Where(fieldInfo => fieldInfo.IsLiteral && !fieldInfo.IsInitOnly))
            {
                string key = (string)info.GetValue(type);
                Assert.NotNull(key);
                string value = info.Name;
                if (keyMap.ContainsKey(key))
                {
                    throw new Exception($"`{info.Name}`s value `{key}` is already used in {keyMap[key]}.");
                }

                keyMap[key] = value;
            }
        }
    }
}
