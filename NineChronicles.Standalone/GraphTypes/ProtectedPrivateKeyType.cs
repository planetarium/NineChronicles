using GraphQL.Types;
using Libplanet.KeyStore;

namespace NineChronicles.Standalone.GraphTypes
{
    public class ProtectedPrivateKeyType : ObjectGraphType<ProtectedPrivateKey>
    {
        public ProtectedPrivateKeyType()
        {
            Field<AddressType>(nameof(ProtectedPrivateKey.Address));
        }
    }
}
