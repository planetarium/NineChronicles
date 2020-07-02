using GraphQL;
using GraphQL.Types;
using Libplanet;
using Libplanet.Crypto;

namespace NineChronicles.Standalone.GraphTypes
{
    public class PublicKeyType : ObjectGraphType<PublicKey>
    {
        public PublicKeyType()
        {
            Field<NonNullGraphType<ByteStringType>>(
                name: "hex",
                description: "A representation of public-key with hexadecimal format.",
                arguments: new QueryArguments(
                    new QueryArgument<BooleanGraphType>
                    {
                        Name = "compress",
                        Description = "A flag to determine whether to compress public-key."
                    }),
                resolve: context =>
                {
                    var compress = context.GetArgument<bool>("compress");
                    return context.Source.Format(compress);
                });

            Field<NonNullGraphType<AddressType>>(
                name: "address",
                description: "An address derived from the public-key.",
                resolve: context => context.Source.ToAddress());
        }
    }
}
