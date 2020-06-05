using System.Security.Cryptography;
using Bencodex;
using GraphQL;
using GraphQL.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Blockchain;
using Nekoyume.Action;

namespace NineChronicles.Standalone.GraphTypes
{
    public class StandaloneQuery : ObjectGraphType
    {
        public StandaloneQuery(StandaloneContext standaloneContext)
        {
            Field<ByteStringType>(
                name: "state",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<AddressType>> { Name = "address", Description = "The address of state to fetch from the chain." },
                    new QueryArgument<ByteStringType> { Name = "hash", Description = "The hash of the block used to fetch state from chain." }
                ),
                resolve: context =>
                {
                    if (!(standaloneContext.BlockChain is BlockChain<PolymorphicAction<ActionBase>> blockChain))
                    {
                        const string runStandaloneEndpoint = "/run-standalone";
                        throw new ExecutionError(
                            $"{nameof(StandaloneContext)}.{nameof(StandaloneContext.BlockChain)} was not set yet!" +
                                    $"You should run standalone through {runStandaloneEndpoint} endpoint");
                    }

                    var address = context.GetArgument<Address>("address");
                    var blockHashByteArray = context.GetArgument<byte[]>("hash");
                    var blockHash = blockHashByteArray is null
                        ? blockChain.Tip.Hash
                        : new HashDigest<SHA256>(blockHashByteArray);

                    var state = blockChain.GetState(address, blockHash);

                    return new Codec().Encode(state);
                }
            );
        }
    }
}
