using System;
using GraphQL.Language.AST;
using GraphQL.Types;
using Libplanet;
using Serilog;

namespace NineChronicles.Standalone.GraphTypes
{
    // Copied from planetarium/libplanet-explorer:59a2d3045e99100fb33d79cc7d0ee6fdc09a1eb4
    // https://git.io/JfXOt
    public class ByteStringType : ScalarGraphType
    {
        public ByteStringType()
        {
            Name = "ByteString";
        }

        public override object Serialize(object value)
        {
            // FIXME: ScalarGraphType에 동작 원리에 대한 이해가 필요합니다.
            // https://graphql-dotnet.github.io/docs/getting-started/custom-scalars/
            return value switch
            {
                byte[] b => ByteUtil.Hex(b),
                string s => s,
                _ => null
            };
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
