using Bencodex;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Model.State;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("transfer_asset2")]
    public class TransferAsset : ActionBase, ISerializable
    {
        private const int MemoMaxLength = 80;

        public TransferAsset()
        {
        }

        public TransferAsset(Address sender, Address recipient, FungibleAssetValue amount, string memo = null)
        {
            Sender = sender;
            Recipient = recipient;
            Amount = amount;

            CheckMemoLength(memo);
            Memo = memo;
        }

        protected TransferAsset(SerializationInfo info, StreamingContext context)
        {
            var rawBytes = (byte[])info.GetValue("serialized", typeof(byte[]));
            Dictionary pv = (Dictionary) new Codec().Decode(rawBytes);

            LoadPlainValue(pv);
        }

        public Address Sender { get; private set; }
        public Address Recipient { get; private set; }
        public FungibleAssetValue Amount { get; private set; }
        public string Memo { get; private set; }

        public override IValue PlainValue
        {
            get
            {
                IEnumerable<KeyValuePair<IKey, IValue>> pairs = new[]
                {
                    new KeyValuePair<IKey, IValue>((Text) "sender", Sender.Serialize()),
                    new KeyValuePair<IKey, IValue>((Text) "recipient", Recipient.Serialize()),
                    new KeyValuePair<IKey, IValue>((Text) "amount", Amount.Serialize()),
                };

                if (!(Memo is null))
                {
                    pairs = pairs.Append(new KeyValuePair<IKey, IValue>((Text) "memo", Memo.Serialize()));
                }

                return new Dictionary(pairs);
            }
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var state = context.PreviousStates;
            if (context.Rehearsal)
            {
                return state.MarkBalanceChanged(Amount.Currency, new[] { Sender, Recipient });
            }

            if (Sender != context.Signer)
            {
                throw new InvalidTransferSignerException(context.Signer, Sender, Recipient);
            }

            // This works for block after 380000. Please take a look at
            // https://github.com/planetarium/libplanet/pull/1133
            if (context.BlockIndex > 380000 && Sender == Recipient)
            {
                throw new InvalidTransferRecipientException(Sender, Recipient);
            }

            if (state.GetState(Addresses.ActivatedAccount) is Dictionary asDict)
            {
                var activatedAccountsState = new ActivatedAccountsState(asDict);
                var activatedAccounts = activatedAccountsState.Accounts;
                if (activatedAccounts.Count != 0
                    && !activatedAccounts.Contains(Recipient))
                {
                    throw new InvalidTransferUnactivatedRecipientException(Sender, Recipient);
                }
            }

            Currency currency = Amount.Currency;
            if (!(currency.Minters is null) &&
                (currency.Minters.Contains(Sender) || currency.Minters.Contains(Recipient)))
            {
                throw new InvalidTransferMinterException(
                    currency.Minters,
                    Sender,
                    Recipient
               );
            }

            return state.TransferAsset(Sender, Recipient, Amount);
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary) plainValue;

            Sender = asDict["sender"].ToAddress();
            Recipient = asDict["recipient"].ToAddress();
            Amount = asDict["amount"].ToFungibleAssetValue();
            Memo = asDict.TryGetValue((Text) "memo", out IValue memo) ? memo.ToDotnetString() : null;

            CheckMemoLength(Memo);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("serialized", new Codec().Encode(PlainValue));
        }

        private void CheckMemoLength(string memo)
        {
            if (memo?.Length > MemoMaxLength)
            {
                string msg = $"The length of the memo, {memo.Length}, " +
                             $"is overflowed than the max length, {MemoMaxLength}.";
                throw new MemoLengthOverflowException(msg);
            }
        }
    }
}
