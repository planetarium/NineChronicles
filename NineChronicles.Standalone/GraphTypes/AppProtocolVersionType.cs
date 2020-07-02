using Bencodex;
using Libplanet.Net;
using GraphQL.Types;

namespace NineChronicles.Standalone.GraphTypes
{
    public sealed class AppProtocolVersionType : ObjectGraphType<AppProtocolVersion>
    {
        private static Codec _codec = new Codec();

        public AppProtocolVersionType()
        {
            Field<NonNullGraphType<IntGraphType>>(
                name: "version",
                resolve: context => context.Source.Version);
            Field<NonNullGraphType<AddressType>>(
                name: "signer",
                resolve: context => context.Source.Signer);
            Field<NonNullGraphType<ByteStringType>>(
                name: "signature",
                resolve: context => context.Source.Signature.ToBuilder().ToArray());
            Field<ByteStringType>(
                name: "extra",
                resolve: context => _codec.Encode(context.Source.Extra));
        }
    }   
}
