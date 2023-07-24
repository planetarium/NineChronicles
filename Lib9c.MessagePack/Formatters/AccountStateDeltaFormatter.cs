using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Bencodex;
using Bencodex.Types;
using Libplanet.Action.State;
using MessagePack;
using MessagePack.Formatters;

namespace Lib9c.Formatters
{
    public class AccountStateDeltaFormatter : IMessagePackFormatter<IAccountStateDelta>
    {
        public void Serialize(ref MessagePackWriter writer, IAccountStateDelta value,
            MessagePackSerializerOptions options)
        {
            var state = new Dictionary(
                value.Delta.UpdatedAddresses.Select(addr => new KeyValuePair<IKey, IValue>(
                    (Binary)addr.ToByteArray(),
                    value.GetState(addr) ?? new Bencodex.Types.Null()
                ))
            );
            var balance = new Bencodex.Types.List(
#pragma warning disable LAA1002
                value.Delta.UpdatedFungibleAssets.Select(pair =>
#pragma warning restore LAA1002
                    new Bencodex.Types.Dictionary(new[]
                    {
                        new KeyValuePair<IKey, IValue>((Text) "address", (Binary) pair.Item1.ByteArray),
                        new KeyValuePair<IKey, IValue>((Text) "currency", pair.Item2.Serialize()),
                        new KeyValuePair<IKey, IValue>((Text) "amount", (Integer) value.GetBalance(pair.Item1, pair.Item2).RawValue),
                    })
                ).Cast<IValue>()
            );
            var totalSupply = new Dictionary(
                value.Delta.UpdatedTotalSupplyCurrencies.Select(currency =>
                    new KeyValuePair<IKey, IValue>(
                        (Binary)new Codec().Encode(currency.Serialize()),
                        (Integer)value.GetTotalSupply(currency).RawValue)));

            var bdict = new Dictionary(new[]
            {
                new KeyValuePair<IKey, IValue>((Text) "states", state),
                new KeyValuePair<IKey, IValue>((Text) "balances", balance),
                new KeyValuePair<IKey, IValue>((Text) "totalSupplies", totalSupply),
            });

            writer.Write(new Codec().Encode(bdict));
        }

        public IAccountStateDelta Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            options.Security.DepthStep(ref reader);

            var bytes = reader.ReadBytes();
            if (bytes is null)
            {
                throw new NullReferenceException($"ReadBytes from serialized {nameof(IAccountStateDelta)} is null.");
            }

            return new AccountStateDelta(new Codec().Decode(bytes.Value.ToArray()));
        }
    }
}
