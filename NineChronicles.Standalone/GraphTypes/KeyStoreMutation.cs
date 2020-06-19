using System;
using System.Linq;
using GraphQL;
using GraphQL.Types;
using Libplanet;
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

            Field<ProtectedPrivateKeyType>("revokePrivateKey",
                arguments: new QueryArguments(
                    new QueryArgument<AddressType>
                    {
                        Name = "address",
                    }),
                resolve: context =>
                {
                    var keyStore = context.Source;
                    var address = context.GetArgument<Address>("address");

                    keyStore.List()
                        .First(guidAndKey => guidAndKey.Item2.Address.Equals(address))
                        .Deconstruct(out Guid guid, out ProtectedPrivateKey protectedPrivateKey);

                    keyStore.Remove(guid);
                    return protectedPrivateKey;
                });
        }
    }
}
