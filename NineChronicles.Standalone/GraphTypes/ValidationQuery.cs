using System;
using GraphQL;
using GraphQL.Types;
using Libplanet.Crypto;

namespace NineChronicles.Standalone.GraphTypes
{
    public class ValidationQuery : ObjectGraphType
    {
        public ValidationQuery()
        {
            Field<NonNullGraphType<BooleanGraphType>>(
                name: "privateKey",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<ByteStringType>>
                    {
                        Name = "hex",
                        Description = "The raw value of private-key, presented as hexadecimal."
                    }),
                resolve: context =>
                {
                    try
                    {
                        var rawPrivateKey = context.GetArgument<byte[]>("hex");
                        var _ = new PrivateKey(rawPrivateKey);
                        return true;
                    }
                    catch (ArgumentException)
                    {
                        return false;
                    }
                }
            );

            Field<NonNullGraphType<BooleanGraphType>>(
                name: "publicKey",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<ByteStringType>>
                    {
                        Name = "hex",
                        Description = "The raw value of public-key, presented as hexadecimal."
                    }),
                resolve: context =>
                {
                    try
                    {
                        var rawPublicKey = context.GetArgument<byte[]>("hex");
                        var _ = new PublicKey(rawPublicKey);
                        return true;
                    }
                    catch (ArgumentException)
                    {
                        return false;
                    }
                    catch (FormatException)
                    {
                        return false;
                    }
                }
            );
        }
    }
}
