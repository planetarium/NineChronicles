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
    public class TransferAssets : ActionBase, ISerializable, ITransferAssets
    {
        private const int MemoMaxLength = 80;

        public TransferAssets()
        {
        }

        public TransferAssets(Address sender, Dictionary<Address, FungibleAssetValue> map, string memo = null)
        {
            Sender = sender;
            Map = map;

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
        public Dictionary<Address, FungibleAssetValue> Map { get; private set; }
        public string Memo { get; private set; }

        public override IValue PlainValue
        {
            get
            {
                IEnumerable<KeyValuePair<IKey, IValue>> pairs = new[]
                {
                    new KeyValuePair<IKey, IValue>((Text) "sender", Sender.Serialize()),
                    new KeyValuePair<IKey, IValue>((Text) "map", Map.OrderBy(i => i.Key).Aggregate(List.Empty, (list, kv) => list.Add(List.Empty.Add(kv.Key.Serialize()).Add(kv.Value.Serialize())))),
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
                return Map.OrderBy(i => i.Key).Aggregate(state, (current, kv) => current.MarkBalanceChanged(kv.Value.Currency, new[] {Sender, kv.Key}));
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, context.Signer);
            var started = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}TransferAsset3 exec started", addressesHex);

            state = Map.OrderBy(i => i.Key).Aggregate(state, (current, kv) => Transfer(current, context.Signer, kv.Key, kv.Value));
            var ended = DateTimeOffset.UtcNow;
            Log.Debug("{AddressesHex}TransferAsset4 Total Executed Time: {Elapsed}", addressesHex, ended - started);

            return state;
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary) plainValue;

            Sender = asDict["sender"].ToAddress();
            var rawMap = (List)asDict["map"];
            Map = new Dictionary<Address, FungibleAssetValue>();
            foreach (var iValue in rawMap)
            {
                var list = (List) iValue;
                var key = list[0].ToAddress();
                var value = list[1].ToFungibleAssetValue();
                Map[key] = value;
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

        private IAccountStateDelta Transfer(IAccountStateDelta state, Address signer, Address recipient, FungibleAssetValue amount)
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
                state.GetState(Addresses.ActivatedAccount) is Dictionary asDict &&
                state.GetState(recipient) is null
            )
            {
                var activatedAccountsState = new ActivatedAccountsState(asDict);
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

            return state.TransferAsset(Sender, recipient, amount);
        }
    }
}
