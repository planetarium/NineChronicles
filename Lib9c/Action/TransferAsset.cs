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
using Nekoyume.Helper;
using Nekoyume.Model;
using Serilog;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/636
    /// Updated at https://github.com/planetarium/lib9c/pull/1718
    /// </summary>
    [Serializable]
    [ActionType("transfer_asset3")]
    public class TransferAsset : ActionBase, ISerializable, ITransferAsset, ITransferAssetV1
    {
        private const int MemoMaxLength = 80;

        // FIXME justify this policy.
        public const long CrystalTransferringRestrictionStartIndex = 6_220_000L;

        // FIXME justify this policy.
        public static readonly IReadOnlyList<Address> AllowedCrystalTransfers = new Address[]
        {
            // world boss service
            new Address("CFCd6565287314FF70e4C4CF309dB701C43eA5bD"),
            // world boss ops
            new Address("3ac40802D359a6B51acB0AC0710cc90de19C9B81"),
        };

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
            Log.Debug("{AddressesHex}TransferAsset3 exec started", addressesHex);
            if (Sender != context.Signer)
            {
                throw new InvalidTransferSignerException(context.Signer, Sender, Recipient);
            }

            if (Sender == Recipient)
            {
                throw new InvalidTransferRecipientException(Sender, Recipient);
            }

            Address recipientAddress = Recipient.Derive(ActivationKey.DeriveKey);

            // Check new type of activation first.
            // If result of GetState is not null, it is assumed that it has been activated.
            if (
                state.GetState(recipientAddress) is null &&
                state.GetState(Addresses.ActivatedAccount) is Dictionary asDict &&
                state.GetState(Recipient) is null
            )
            {
                var activatedAccountsState = new ActivatedAccountsState(asDict);
                var activatedAccounts = activatedAccountsState.Accounts;
                // if ActivatedAccountsState is empty, all user is activate.
                if (activatedAccounts.Count != 0
                    && !activatedAccounts.Contains(Recipient)
                    && state.GetState(Recipient) is null)
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

            CheckCrystalSender(currency, context.BlockIndex, Sender);
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}TransferAsset3 Total Executed Time: {Elapsed}", addressesHex, ended - started);
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

        public static void CheckCrystalSender(Currency currency, long blockIndex, Address sender)
        {
            if (currency.Equals(CrystalCalculator.CRYSTAL) &&
                blockIndex >= CrystalTransferringRestrictionStartIndex && !AllowedCrystalTransfers.Contains(sender))
            {
                throw new InvalidTransferCurrencyException($"transfer crystal not allowed {sender}");
            }
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
