using System;
using GraphQL.Language.AST;
using GraphQL.Types;
using Libplanet;

namespace NineChronicles.Standalone.GraphTypes
{
    // Copied from planetarium/libplanet-explorer:59a2d3045e99100fb33d79cc7d0ee6fdc09a1eb4
    // https://git.io/JfXOt
    public class ByteStringType : StringGraphType
    {
        public ByteStringType()
        {
            Name = "ByteString";
        }

        public override object Serialize(object value)
        {
            return value is byte[] b ? ByteUtil.Hex(b) : null;
        }

        public override object ParseValue(object value)
        {
            switch (value)
            {
                case null:
                    return null;
                case string hex:
                    return ByteUtil.ParseHex(hex);
                default:
                    throw new ArgumentException("Expected a hexadecimal string.", nameof(value));
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
