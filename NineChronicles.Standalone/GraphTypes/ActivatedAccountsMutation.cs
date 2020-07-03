using System;
using GraphQL;
using GraphQL.Types;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model;
using Log = Serilog.Log;

namespace NineChronicles.Standalone.GraphTypes
{
    public class ActivatedAccountsMutation : ObjectGraphType
    {
        public ActivatedAccountsMutation(StandaloneContext standaloneContext)
        {
            Field<NonNullGraphType<BooleanGraphType>>("activateAccount",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<StringGraphType>>
                    {
                        Name = "encodedActivationKey",
                    }),
                resolve: context =>
                {
                    try
                    {
                        string encodedActivationKey =
                            context.GetArgument<string>("encodedActivationKey");
                        NineChroniclesNodeService service = standaloneContext.NineChroniclesNodeService;
                        PrivateKey privateKey = service.PrivateKey;
                        ActivationKey activationKey = ActivationKey.Decode(encodedActivationKey);
                        var action = new ActivateAccount(
                            activationKey.PendingAddress,
                            activationKey.PrivateKey.ByteArray);

                        BlockChain<PolymorphicAction<ActionBase>> blockChain = service.Swarm.BlockChain;
                        var actions = new PolymorphicAction<ActionBase>[] {action};
                        blockChain.MakeTransaction(privateKey, actions);
                    }
                    catch (Exception e)
                    {
                        Log.Error("Unexpected exception occurred during ActivatedAccountsMutation: {e}", e);
                        return false;
                    }

                    return true;
                });
        }
    }
}
