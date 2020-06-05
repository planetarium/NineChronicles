using System;
using System.Linq;
using GraphQL;
using GraphQL.Types;
using Libplanet;
using Libplanet.KeyStore;
using Org.BouncyCastle.Security;

namespace NineChronicles.Standalone.GraphTypes
{
    public class KeyStoreType : ObjectGraphType<IKeyStore>
    {
        public KeyStoreType()
        {
            Field<NonNullGraphType<ListGraphType<ProtectedPrivateKeyType>>>(
                name: "protectedPrivateKeys",
                resolve: context => context.Source.List().Select(t => t.Item2));

            // TODO: description을 적어야 합니다.
            Field<ByteStringType>(
                name: "decryptedPrivateKey",
                arguments: new QueryArguments(
                    new QueryArgument<AddressType> { Name = "address" },
                    new QueryArgument<StringGraphType> { Name = "passphrase" }),
                resolve: context =>
                {
                    var keyStore = context.Source;

                    var address = context.GetArgument<Address>("address");
                    var passphrase = context.GetArgument<string>("passphrase");

                    var protectedPrivateKeys = keyStore.List().Select(t => t.Item2);

                    try
                    {
                        var protectedPrivateKey = protectedPrivateKeys.Where(key => key.Address.Equals(address)).First();
                        var privateKey = protectedPrivateKey.Unprotect(passphrase);
                        return privateKey.ByteArray;
                    }
                    catch (InvalidOperationException)
                    {
                        return null;
                    }
                }
            );
        }
    }
}
