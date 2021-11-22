using System;
using System.Buffers;
using Bencodex;
using Bencodex.Types;
using Libplanet.Assets;
using MessagePack;
using MessagePack.Formatters;
using Nekoyume.Model.State;

namespace Lib9c.Formatters
{
    public class FungibleAssetValueFormatter : IMessagePackFormatter<FungibleAssetValue>
    {
        public void Serialize(ref MessagePackWriter writer, FungibleAssetValue value,
            MessagePackSerializerOptions options)
        {
            writer.Write(new Codec().Encode(value.Serialize()));
        }

        public FungibleAssetValue Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            options.Security.DepthStep(ref reader);

            IValue value = new Codec().Decode(reader.ReadBytes()?.ToArray() ?? throw new InvalidOperationException());
            return value.ToFungibleAssetValue();
        }
    }
}
