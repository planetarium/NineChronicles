using Bencodex.Types;
using System;
using GraphQL.Types;
using Libplanet;
using Libplanet.Blockchain;
using Libplanet.Crypto;
using Nekoyume.Model.State;
using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;
using Log = Serilog.Log;

namespace NineChronicles.Standalone.GraphTypes
{
    public class ActivationStatusQuery : ObjectGraphType
    {
        public ActivationStatusQuery(StandaloneContext standaloneContext)
        {
            Field<NonNullGraphType<BooleanGraphType>>(
                name: "activationStatus",
                resolve: context =>
                {
                    try
                    {
                        PrivateKey privateKey = standaloneContext.NineChroniclesNodeService.PrivateKey;
                        Address address = privateKey.ToAddress();
                        BlockChain<NineChroniclesActionType> blockChain = standaloneContext.BlockChain;
                        IValue state = blockChain.GetState(ActivatedAccountsState.Address);

                        if (state is Bencodex.Types.Dictionary asDict)
                        {
                            var activatedAccountsState = new ActivatedAccountsState(asDict);
                            return activatedAccountsState.Accounts.Contains(address);
                        }

                        return true;
                    }
                    catch (Exception e)
                    {
                        Log.Error("Unexpected exception occurred during ActivatedAccountsMutation: {e}", e);
                        return false;
                    }
                }
            );
        }
    }
}
