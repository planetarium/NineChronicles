using Bencodex;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Model.State;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Serialization;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("transfer_asset")]
    public class TransferAsset : ActionBase, ISerializable
    {
        public TransferAsset()
        {
        }

        public TransferAsset(Address sender, Address recipient, BigInteger amount, Currency currency)
        {
            Sender = sender;
            Recipient = recipient;
            Amount = amount;
            Currency = currency;
        }

        protected TransferAsset(SerializationInfo info, StreamingContext context)
        {
            var rawBytes = (byte[])info.GetValue("serialized", typeof(byte[]));
            Dictionary pv = (Dictionary) new Codec().Decode(rawBytes);

            LoadPlainValue(pv);
        }

        public Address Sender { get; private set; }
        public Address Recipient { get; private set; }
        public BigInteger Amount { get; private set; }
        public Currency Currency { get; private set; }

        public override IValue PlainValue => new Dictionary(
            new[]
            {
                new KeyValuePair<IKey, IValue>((Text) "sender", Sender.Serialize()),
                new KeyValuePair<IKey, IValue>((Text) "recipient", Recipient.Serialize()),
                new KeyValuePair<IKey, IValue>((Text) "amount", Amount.Serialize()),
                new KeyValuePair<IKey, IValue>((Text) "currency", Currency.Serialize()),
            }
        );

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var state = context.PreviousStates;
            if (context.Rehearsal)
            {
                return state.MarkBalanceChanged(Currency, new[] { Sender, Recipient });
            }

            if (Sender != context.Signer)
            {
                throw new InvalidTransferSignerException(context.Signer, Sender, Recipient);
            }

            return state.TransferAsset(Sender, Recipient, Currency, Amount);
        }

        public override void LoadPlainValue(IValue plainValue)
        {
            var asDict = (Dictionary) plainValue;

            Sender = asDict["sender"].ToAddress();
            Recipient = asDict["recipient"].ToAddress();
            Amount = asDict["amount"].ToBigInteger();
            Currency = CurrencyExtensions.Deserialize((Dictionary)asDict["currency"]);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("serialized", new Codec().Encode(PlainValue));
        }
    }
}
