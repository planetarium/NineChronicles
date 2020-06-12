using System;
using GraphQL.Language.AST;
using GraphQL.Types;
using Libplanet;

namespace NineChronicles.Standalone.GraphTypes
{
    // Copied from planetarium/libplanet-explorer:59a2d3045e99100fb33d79cc7d0ee6fdc09a1eb4
    // https://git.io/JfXZA
    public class AddressType : StringGraphType
    {
        public AddressType()
        {
            Name = "Address";
        }

        public override object Serialize(object value)
        {
            if (value is Address addr)
            {
                return addr.ToString();
            }

            return value;
        }

        public override object ParseValue(object value)
        {
            switch (value)
            {
                case null:
                    return null;
                case string hex:
                    if (hex.Substring(0, 2).ToLower().Equals("0x"))
                    {
                        hex = hex.Substring(2);
                    }

                    return new Address(hex);
                default:
                    throw new ArgumentException(
                        $"Expected a hexadecimal string but {value}", nameof(value));
            }
        }

        public override object ParseLiteral(IValue value)
        {
            if (value is StringValue)
            {
                return ParseValue(value.Value);
            }

            return null;
        }
    }
}
