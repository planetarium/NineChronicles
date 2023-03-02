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
    [ActionType("transfer_assets")]
    public class TransferAssets : ActionBase, ISerializable, ITransferAssets, ITransferAssetsV1
    {
        public const int RecipientsCapacity = 100;
        private const int MemoMaxLength = 80;

        public TransferAssets()
        {
        }

        public TransferAssets(Address sender, List<(Address, FungibleAssetValue)> recipients, string memo = null)
        {
            Sender = sender;
            Recipients = recipients;

            CheckMemoLength(memo);
            Memo = memo;
        }

        protected TransferAssets(SerializationInfo info, StreamingContext context)
        {
            var rawBytes = (byte[])info.GetValue("serialized", typeof(byte[]));
            Dictionary pv = (Dictionary) new Codec().Decode(rawBytes);

            LoadPlainValue(pv);
        }

        public Address Sender { get; private set; }
        public List<(Address recipient, FungibleAssetValue amount)> Recipients { get; private set; }
        public string Memo { get; private set; }

        Address ITransferAssetsV1.Sender => Sender;

        List<(Address recipient, FungibleAssetValue amount)> ITransferAssetsV1.Recipients =>
            Recipients;
        string ITransferAssetsV1.Memo => Memo;

        public override IValue PlainValue
        {
            get
            {
                IEnumerable<KeyValuePair<IKey, IValue>> pairs = new[]
                {
                    new KeyValuePair<IKey, IValue>((Text) "sender", Sender.Serialize()),
                    new KeyValuePair<IKey, IValue>((Text) "recipients", Recipients.Aggregate(List.Empty, (list, t) => list.Add(List.Empty.Add(t.recipient.Serialize()).Add(t.amount.Serialize())))),
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
                return Recipients.Aggregate(state, (current, t) => current.MarkBalanceChanged(t.amount.Currency, new[] {Sender, t.recipient}));
            }

            if (Recipients.Count > RecipientsCapacity)
            {
                throw new ArgumentOutOfRangeException($"{nameof(Recipients)} must be less than or equal {RecipientsCapacity}.");
            }
            var addressesHex = GetSignerAndOtherAddressesHex(context, context.Signer);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}transfer_assets exec started", addressesHex);

            var activatedAccountsState = state.GetState(Addresses.ActivatedAccount) is Dictionary asDict
                ? new ActivatedAccountsState(asDict)
                : new ActivatedAccountsState();

            state = Recipients.Aggregate(state, (current, t) => Transfer(current, context.Signer, t.recipient, t.amount, activatedAccountsState, context.BlockIndex));
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}transfer_assets Total Executed Time: {Elapsed}", addressesHex, ended - started);

            return state;
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary) plainValue;

            Sender = asDict["sender"].ToAddress();
            var rawMap = (List)asDict["recipients"];
            Recipients = new List<(Address recipient, FungibleAssetValue amount)>();
            foreach (var iValue in rawMap)
            {
                var list = (List) iValue;
                Recipients.Add((list[0].ToAddress(), list[1].ToFungibleAssetValue()));
            }
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

        private IAccountStateDelta Transfer(IAccountStateDelta state, Address signer, Address recipient, FungibleAssetValue amount, ActivatedAccountsState activatedAccountsState, long blockIndex)
        {
            if (Sender != signer)
            {
                throw new InvalidTransferSignerException(signer, Sender, recipient);
            }

            if (Sender == recipient)
            {
                throw new InvalidTransferRecipientException(Sender, recipient);
            }

            Address recipientAddress = recipient.Derive(ActivationKey.DeriveKey);

            // Check new type of activation first.
            // If result of GetState is not null, it is assumed that it has been activated.
            if (
                state.GetState(recipientAddress) is null &&
                state.GetState(recipient) is null
            )
            {
                var activatedAccounts = activatedAccountsState.Accounts;
                // if ActivatedAccountsState is empty, all user is activate.
                if (activatedAccounts.Count != 0
                    && !activatedAccounts.Contains(recipient)
                    && state.GetState(recipient) is null)
                {
                    throw new InvalidTransferUnactivatedRecipientException(Sender, recipient);
                }
            }

            Currency currency = amount.Currency;
            if (!(currency.Minters is null) &&
                (currency.Minters.Contains(Sender) || currency.Minters.Contains(recipient)))
            {
                throw new InvalidTransferMinterException(
                    currency.Minters,
                    Sender,
                    recipient
                );
            }

            TransferAsset.CheckCrystalSender(currency, blockIndex, Sender);
            return state.TransferAsset(Sender, recipient, amount);
        }
    }
}
