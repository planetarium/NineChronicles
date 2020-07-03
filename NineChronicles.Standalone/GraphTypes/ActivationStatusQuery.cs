using Bencodex.Types;
using GraphQL;
using GraphQL.Types;
using Libplanet;
using Libplanet.Blockchain;
using Nekoyume.Model.State;
using NineChroniclesActionType = Libplanet.Action.PolymorphicAction<Nekoyume.Action.ActionBase>;

namespace NineChronicles.Standalone.GraphTypes
{
    public class ActivationStatusQuery : ObjectGraphType
    {
        public ActivationStatusQuery(StandaloneContext standaloneContext)
        {
            Field<NonNullGraphType<BooleanGraphType>>(
                name: "activationStatus",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<ByteStringType>>
                    {
                        Name = "address",
                        Description = "Address to check if it is activated."
                    }),
                resolve: context =>
                {
                    byte[] rawAddress = context.GetArgument<byte[]>("address");
                    var address = new Address(rawAddress);
                    BlockChain<NineChroniclesActionType> blockChain = standaloneContext.BlockChain;
                    IValue state = blockChain.GetState(ActivatedAccountsState.Address);

                    if (state is Bencodex.Types.Dictionary asDict)
                    {
                        var activatedAccountsState = new ActivatedAccountsState(asDict);
                        return activatedAccountsState.Accounts.Contains(address);
                    }

                    return false;
                }
            );
        }
    }
}
