using System.Collections.Generic;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Action.State;
using Libplanet.Crypto;
using Libplanet.Types.Assets;
using Nekoyume.Exceptions;
using Nekoyume.Model.State;

namespace Nekoyume.Action
{
    [ActionType(TypeIdentifier)]
    public class BurnAsset : ActionBase
    {
        private const int MemoMaxLength = 80;
        public const string TypeIdentifier = "burn_asset";

        private string _memo;

        public BurnAsset()
        {
        }

        public BurnAsset(Address owner, FungibleAssetValue amount, string memo)
        {
            Owner = owner;
            Amount = amount;
            Memo = memo;
        }

        public override IAccount Execute(IActionContext context)
        {
            context.UseGas(1);

            IAccount state = context.PreviousState;

            if (!Addresses.CheckAgentHasPermissionOnBalanceAddr(context.Signer, Owner))
            {
                throw new InvalidActionFieldException(
                    actionType: TypeIdentifier,
                    addressesHex: context.Signer.ToHex(),
                    fieldName: nameof(Owner),
                    message: $"context.Signer doesn't own {Owner}"
                );
            }

            return state.BurnAsset(context, Owner, Amount);
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary)plainValue;
            var values = (List)asDict["values"];

            Owner = values[0].ToAddress();
            Amount = values[1].ToFungibleAssetValue();
            Memo = (Text)values[2];
        }

        public override IValue PlainValue =>
            new Dictionary(
                new[]
                {
                    new KeyValuePair<IKey, IValue>(
                        (Text)"type_id",
                        (Text)TypeIdentifier
                    ),
                    new KeyValuePair<IKey, IValue>(
                        (Text)"values",
                        new List(
                            Owner.Serialize(),
                            Amount.Serialize(),
                            (Text)Memo
                        )
                    ),
                }
            );

        public FungibleAssetValue Amount { get; private set; }

        public string Memo {
            get => _memo;
            private set
            {
                if (value.Length >= MemoMaxLength) {
                    string msg = $"The length of the memo, {value.Length}, is overflowed than " +
                    $"the max length, {MemoMaxLength}.";

                    throw new MemoLengthOverflowException(msg);
                }

                _memo = value;
            }
        }

        public Address Owner { get; private set; }

    }
}
