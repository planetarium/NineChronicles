using System.Collections.Immutable;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Crypto;

namespace Libplanet.Extensions.ActionEvaluatorCommonComponents.Tests;

public class ActionEvaluationSerializerTest
{
    [Fact]
    public void Serialization()
    {
        var addresses = Enumerable.Repeat(0, 4).Select(_ => new PrivateKey().ToAddress()).ToImmutableList();
        AccountStateDelta outputStates = (AccountStateDelta)new AccountStateDelta()
            .SetState(addresses[0], Null.Value)
            .SetState(addresses[1], (Text)"foo")
            .SetState(addresses[2], new List((Text)"bar"));

        var previousStates = new AccountStateDelta();

        var actionEvaluation = new ActionEvaluation(
            Null.Value,
            new ActionContext(null,
                addresses[0],
                null,
                addresses[1],
                0,
                0,
                false,
                previousStates,
                123,
                null,
                true),
            outputStates,
            new Libplanet.Action.UnexpectedlyTerminatedActionException("", null, null, null, null, new NullAction(), null));
        var serialized = ActionEvaluationMarshaller.Serialize(actionEvaluation);
        var deserialized = ActionEvaluationMarshaller.Deserialize(serialized);

        Assert.Equal(Null.Value, deserialized.Action);
        Assert.Equal(123, deserialized.InputContext.RandomSeed);
        Assert.Equal(0, deserialized.InputContext.BlockIndex);
        Assert.Equal(0, deserialized.InputContext.BlockProtocolVersion);
        Assert.Equal(addresses[0], deserialized.InputContext.Signer);
        Assert.Equal(addresses[1], deserialized.InputContext.Miner);
        Assert.Equal(Null.Value, deserialized.OutputState.GetState(addresses[0]));
        Assert.Equal((Text)"foo", deserialized.OutputState.GetState(addresses[1]));
        Assert.Equal(new List((Text)"bar"), deserialized.OutputState.GetState(addresses[2]));
    }
}
