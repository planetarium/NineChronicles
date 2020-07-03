using System;
using Bencodex.Types;
using GraphQL;
using GraphQL.Types;
using Libplanet.Action;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Nekoyume.Action;
using Nekoyume.Model;
using Nekoyume.Model.State;
using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;
using Log = Serilog.Log;

namespace NineChronicles.Standalone.GraphTypes
{
    public class ActivationStatusMutation : ObjectGraphType<NineChroniclesNodeService>
    {
        public ActivationStatusMutation()
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
                        NineChroniclesNodeService service = context.Source;
                        PrivateKey privateKey = service.PrivateKey;
                        ActivationKey activationKey = ActivationKey.Decode(encodedActivationKey);
                        BlockChain<NineChroniclesActionType> blockChain = service.Swarm.BlockChain;
                        IValue state = blockChain.GetState(activationKey.PendingAddress);

                        if (!(state is Bencodex.Types.Dictionary asDict))
                        {
                            return false;
                        }

                        var pendingActivationState = new PendingActivationState(asDict);
                        ActivateAccount action = activationKey.CreateActivateAccount(
                            pendingActivationState.Nonce);

                        var actions = new PolymorphicAction<ActionBase>[] { action };
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
