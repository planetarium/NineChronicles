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
                name: "activated",
                resolve: context =>
                {
                    var service = standaloneContext.NineChroniclesNodeService;

                    if (service is null)
                    {
                        return false;
                    }

                    try
                    {
                        PrivateKey privateKey = service.PrivateKey;
                        Address address = privateKey.ToAddress();
                        BlockChain<NineChroniclesActionType> blockChain = service.Swarm.BlockChain;
                        IValue state = blockChain.GetState(ActivatedAccountsState.Address);

                        if (state is Bencodex.Types.Dictionary asDict)
                        {
                            var activatedAccountsState = new ActivatedAccountsState(asDict);
                            var activatedAccounts = activatedAccountsState.Accounts;
                            return activatedAccounts.Count == 0
                                   || activatedAccounts.Contains(address);
                        }

                        return true;
                    }
                    catch (Exception e)
                    {
                        Log.Error("Unexpected exception occurred during ActivationStatusQuery: {e}", e);
                        return false;
                    }
                }
            );
        }
    }
}
