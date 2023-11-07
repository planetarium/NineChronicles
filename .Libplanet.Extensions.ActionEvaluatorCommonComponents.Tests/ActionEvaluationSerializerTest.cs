using System.Collections.Immutable;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Common;
using Libplanet.Crypto;

namespace Libplanet.Extensions.ActionEvaluatorCommonComponents.Tests;

public class ActionEvaluationSerializerTest
{
    [Fact]
    public void Serialization()
    {
        var addresses = Enumerable.Repeat(0, 4).Select(_ => new PrivateKey().ToAddress()).ToImmutableList();

        var random = new System.Random();
        var buffer = new byte[HashDigest<SHA256>.Size];
        random.NextBytes(buffer);
        var prevState = new HashDigest<SHA256>(buffer);
        random.NextBytes(buffer);
        var outputState = new HashDigest<SHA256>(buffer);

        var committed = new CommittedActionEvaluation(
            action: Null.Value,
            inputContext: new CommittedActionContext(
                signer: addresses[0],
                txId: null,
                miner: addresses[1],
                blockIndex: 0,
                blockProtocolVersion: 0,
                rehearsal: false,
                previousState: prevState,
                randomSeed: 123,
                blockAction: true),
            outputState: outputState,
            exception: new UnexpectedlyTerminatedActionException(
                "",
                null,
                null,
                null,
                null,
                new NullAction(),
                new Exception()));
        var serialized = ActionEvaluationMarshaller.Serialize(committed);
        var deserialized = ActionEvaluationMarshaller.Deserialize(serialized);

        Assert.Equal(Null.Value, deserialized.Action);
        Assert.Equal(123, deserialized.InputContext.RandomSeed);
        Assert.Equal(0, deserialized.InputContext.BlockIndex);
        Assert.Equal(0, deserialized.InputContext.BlockProtocolVersion);
        Assert.Equal(addresses[0], deserialized.InputContext.Signer);
        Assert.Equal(addresses[1], deserialized.InputContext.Miner);
        Assert.Equal(prevState, deserialized.InputContext.PreviousState);
        Assert.Equal(outputState, deserialized.OutputState);
    }
}
