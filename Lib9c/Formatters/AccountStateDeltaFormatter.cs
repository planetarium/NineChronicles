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
using Nekoyume;
using Nekoyume.Action;

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
                                new KeyValuePair<IKey, IValue>((Text) "address", (Binary) ua.Key.ToByteArray()),
                                new KeyValuePair<IKey, IValue>((Text) "currency", CurrencyExtensions.Serialize(c)),
                                new KeyValuePair<IKey, IValue>((Text) "amount", (Integer) b.RawValue),
                            });
                        }
                    )
                ).Cast<IValue>()
            );

            var bdict = new Dictionary(new[]
            {
                new KeyValuePair<IKey, IValue>((Text) "states", state),
                new KeyValuePair<IKey, IValue>((Text) "balances", balance),
            });

            writer.Write(new Codec().Encode(bdict));
        }

        public IAccountStateDelta Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            options.Security.DepthStep(ref reader);

            IValue value = new Codec().Decode(reader.ReadBytes()?.ToArray() ?? throw new InvalidOperationException());
            return new ActionBase.AccountStateDelta(value);
        }
    }
}
