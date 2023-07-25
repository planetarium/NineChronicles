using System.Collections.Immutable;
using Bencodex.Types;
using Lib9c.Abstractions;
using Libplanet.Action;
using Nekoyume.Model.State;
using System.Linq;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;

namespace Nekoyume.Action
{
    [ActionType("secure_mining_reward")]
    public class SecureMiningReward : ActionBase, ISecureMiningReward
    {
        // Copied from https://github.com/planetarium/lib9c/blob/v100361/Lib9c/Policy/BlockPolicySource.cs#L112-L118
        // FIXME consider better way to check.
        private static readonly ImmutableList<Address> AuthorizedMiners = new[]
        {
            new Address("ab1dce17dCE1Db1424BB833Af6cC087cd4F5CB6d"),
            new Address("3217f757064Cd91CAba40a8eF3851F4a9e5b4985"),
            new Address("474CB59Dea21159CeFcC828b30a8D864e0b94a6B"),
            new Address("636d187B4d434244A92B65B06B5e7da14b3810A9"),
        }.ToImmutableList();

        // You can check about the treasury address and following rates from NCIP-10
        // https://docs.google.com/document/d/1ErZ5JQia03KqXRG6IRZ7SORfnxMLZfJg4patVKFGX5Y/edit#
        private static readonly Address Treasury = new Address("0xB3bCa3b3c6069EF5Bdd6384bAD98F11378Dc360E");

        private static readonly Address Nil = new Address("0xFFfFfFffFFfffFFfFFfFFFFFffFFFffffFfFFFfF");

        private const int TreasuryRate = 40;

        private const int EarnRate = 20;

        internal static readonly Currency NCG =
            Currency.Legacy(
                "NCG",
                2,
                ImmutableHashSet.Create(new Address("0x47D082a115c63E7b58B1532d20E631538eaFADde"))
            );

        public SecureMiningReward()
        {
        }

        public SecureMiningReward(Address recipient)
        {
            Recipient = recipient;
        }

        public Address Recipient { get; private set; }

        public override IValue PlainValue => Dictionary.Empty
            .Add("type_id", "secure_mining_reward")
            .Add("values", Recipient.Bencoded);

        public override IAccountStateDelta Execute(IActionContext context)
        {
            context.UseGas(1);
            IAccountStateDelta state = context.PreviousState;
            if (context.Rehearsal)
            {
                return state.MarkBalanceChanged(
                    context,
                    NCG,
                    AuthorizedMiners.Add(Recipient).Add(Treasury).ToArray()
                );
            }

            CheckPermission(context);

            foreach (Address minerAddress in AuthorizedMiners)
            {
                FungibleAssetValue balance = state.GetBalance(minerAddress, NCG);
                FungibleAssetValue toTreasury = balance.DivRem(100, out _) * TreasuryRate;
                FungibleAssetValue toRecipient = balance.DivRem(100, out _) * EarnRate;
                FungibleAssetValue toBurn = balance - (toTreasury + toRecipient);

                state = state.TransferAsset(context, minerAddress, Treasury, toTreasury);
                state = state.TransferAsset(context, minerAddress, Recipient, toRecipient);
                state = state.TransferAsset(context, minerAddress, Nil, toBurn);
            }

            return state;
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            Recipient = ((Dictionary)plainValue)["values"].ToAddress();
        }
    }
}
