using System;
using System.Text.Json;
using GraphQL;
using GraphQL.Types;
using Libplanet.Crypto;
using Serilog;

namespace NineChronicles.Standalone.GraphTypes
{
    public class ValidationQuery : ObjectGraphType
    {
        public ValidationQuery(StandaloneContext standaloneContext)
        {
            Field<NonNullGraphType<BooleanGraphType>>(
                name: "metadata",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "raw",
                        Description = "The raw value of json metadata."
                    }),
                resolve: context =>
                {
                    var raw = context.GetArgument<string>("raw");
                    try
                    {
                        var remoteIndex = JsonDocument.Parse(raw).RootElement.GetProperty("Index").GetInt32();
                        Log.Debug("Remote: {index1}, Local: {index2}",
                            remoteIndex, standaloneContext.BlockChain.Tip?.Index ?? -1);
                        var ret = remoteIndex > (standaloneContext.BlockChain.Tip?.Index ?? -1);
                        return ret;
                    }
                    catch (JsonException je)
                    {
                        Log.Warning(je, "Given metadata is invalid. (raw: {raw})", raw);
                        return false;
                    }
                    catch (Exception e)
                    {
                        Log.Warning(e, "Unexpected exception occurred. (raw: {raw})", raw);
                        return false;
                    }
                }
            );

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
