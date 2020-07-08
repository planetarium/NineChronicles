using GraphQL.Types;
using Libplanet;
using Libplanet.Crypto;

namespace NineChronicles.Standalone.GraphTypes
{
    public class PrivateKeyType : ObjectGraphType<PrivateKey>
    {
        public PrivateKeyType()
        {
            Field<NonNullGraphType<ByteStringType>>(
                name: "hex",
                description: "A representation of private-key with hexadecimal format.",
                resolve: context => context.Source.ByteArray);

            Field<NonNullGraphType<PublicKeyType>>(
                name: nameof(PrivateKey.PublicKey),
                description: "A public-key derived from the private-key.");
        }
    }
}
