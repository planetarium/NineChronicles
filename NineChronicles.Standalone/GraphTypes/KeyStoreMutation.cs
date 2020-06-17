using GraphQL;
using GraphQL.Types;
using Libplanet.Crypto;
using Libplanet.KeyStore;

namespace NineChronicles.Standalone.GraphTypes
{
    public class KeyStoreMutation : ObjectGraphType<IKeyStore>
    {
        public KeyStoreMutation()
        {
            Field<ProtectedPrivateKeyType>("createPrivateKey",
                arguments: new QueryArguments(
                    new QueryArgument<StringGraphType>
                    {
                        Name = "passphrase",
                    }),
                resolve: context =>
                {
                    var keyStore = context.Source;
                    var passphrase = context.GetArgument<string>("passphrase");

                    var privateKey = new PrivateKey();
                    var protectedPrivateKey = ProtectedPrivateKey.Protect(privateKey, passphrase);

                    keyStore.Add(protectedPrivateKey);
                    return protectedPrivateKey;
                });
        }
    }
}
