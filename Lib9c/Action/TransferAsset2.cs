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
using Lib9c.Abstractions;
using Nekoyume.Model;
using Serilog;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/636
    /// Updated at https://github.com/planetarium/lib9c/pull/957
    /// </summary>
    [Serializable]
    [ActionType("transfer_asset2")]
    public class TransferAsset2 : ActionBase, ISerializable, ITransferAsset, ITransferAssetV1
    {
        private const int MemoMaxLength = 80;

        public TransferAsset2()
        {
        }

        public TransferAsset2(Address sender, Address recipient, FungibleAssetValue amount, string memo = null)
        {
            Sender = sender;
            Recipient = recipient;
            Amount = amount;

            CheckMemoLength(memo);
            Memo = memo;
        }

        protected TransferAsset2(SerializationInfo info, StreamingContext context)
        {
            var rawBytes = (byte[])info.GetValue("serialized", typeof(byte[]));
            Dictionary pv = (Dictionary) new Codec().Decode(rawBytes);

            LoadPlainValue(pv);
        }

        public Address Sender { get; private set; }
        public Address Recipient { get; private set; }
        public FungibleAssetValue Amount { get; private set; }
        public string Memo { get; private set; }

        Address ITransferAssetV1.Sender => Sender;
        Address ITransferAssetV1.Recipient => Recipient;
        FungibleAssetValue ITransferAssetV1.Amount => Amount;
        string ITransferAssetV1.Memo => Memo;

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

            var addressesHex = GetSignerAndOtherAddressesHex(context, context.Signer);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}TransferAsset2 exec started", addressesHex);
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

            Address recipientAddress = Recipient.Derive(ActivationKey.DeriveKey);

            // Check new type of activation first.
            if (state.GetState(recipientAddress) is null && state.GetState(Addresses.ActivatedAccount) is Dictionary asDict )
            {
                var activatedAccountsState = new ActivatedAccountsState(asDict);
                var activatedAccounts = activatedAccountsState.Accounts;
                // if ActivatedAccountsState is empty, all user is activate.
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

            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}TransferAsset2 Total Executed Time: {Elapsed}", addressesHex, ended - started);
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
