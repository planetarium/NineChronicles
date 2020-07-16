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
            Field<NonNullGraphType<PrivateKeyType>>("createPrivateKey",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "passphrase",
                    },
                    new QueryArgument<ByteStringType>
                    {
                        Name = "privateKey",
                    }),
                resolve: context =>
                {
                    var keyStore = context.Source;
                    var passphrase = context.GetArgument<string>("passphrase");
                    var privateKeyBytes = context.GetArgument<byte[]>("privateKey");

                    var privateKey = privateKeyBytes is null ? new PrivateKey() : new PrivateKey(privateKeyBytes);
                    var protectedPrivateKey = ProtectedPrivateKey.Protect(privateKey, passphrase);

                    keyStore.Add(protectedPrivateKey);
                    return privateKey;
                });

            Field<NonNullGraphType<ProtectedPrivateKeyType>>("revokePrivateKey",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<AddressType>>
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
