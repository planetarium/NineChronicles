using System;
using GraphQL;
using GraphQL.Types;
using Libplanet.Action;
using Nekoyume.Action;
using Nekoyume.Model;
using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;
using Log = Serilog.Log;

namespace NineChronicles.Standalone.GraphTypes
{
    public class ActivatedAccountsMutation : ObjectGraphType<NineChroniclesNodeService>
    {
        public ActivatedAccountsMutation()
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
                        var encodedActivationKey =
                            context.GetArgument<string>("encodedActivationKey");
                        var service = context.Source;
                        var privateKey = service.PrivateKey;
                        var activationKey = ActivationKey.Decode(encodedActivationKey);
                        var action = new ActivateAccount(
                            activationKey.PendingAddress,
                            activationKey.PrivateKey.ByteArray);

                        var blockChain = service.Swarm.BlockChain;
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
