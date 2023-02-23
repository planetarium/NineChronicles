using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using Bencodex;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Assets;
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
                value.UpdatedAddresses.Select(addr => new KeyValuePair<IKey, IValue>(
                    (Binary)addr.ToByteArray(),
                    value.GetState(addr) ?? new Bencodex.Types.Null()
                ))
            );
            var balance = new Bencodex.Types.List(
#pragma warning disable LAA1002
                value.UpdatedFungibleAssets.SelectMany(ua =>
#pragma warning restore LAA1002
                    ua.Value.Select(c =>
                        {
                            FungibleAssetValue b = value.GetBalance(ua.Key, c);
                            return new Bencodex.Types.Dictionary(new[]
                            {
                                new KeyValuePair<IKey, IValue>((Text) "address", (Binary) ua.Key.ByteArray),
                                new KeyValuePair<IKey, IValue>((Text) "currency", c.Serialize()),
                                new KeyValuePair<IKey, IValue>((Text) "amount", (Integer) b.RawValue),
                            });
                        }
                    )
                ).Cast<IValue>()
            );
            var totalSupply = new Dictionary(
                value.TotalSupplyUpdatedCurrencies.Select(currency =>
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
