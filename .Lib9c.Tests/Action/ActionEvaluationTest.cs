namespace Lib9c.Tests.Action
{
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Assets;
    using Nekoyume.Action;
    using Xunit;

    public class ActionEvaluationTest
    {
        [Fact]
        public void SerializeWithDotnetAPI()
        {
            var currency = new Currency("NCG", 2, minters: null);
            var signer = default(Address);
            var blockIndex = 1234;
            var states = new State()
                .SetState(signer, (Text)"ANYTHING")
                .SetState(default, Dictionary.Empty.Add("key", "value"))
                .MintAsset(signer, currency * 10000);
            var action = new TransferAsset(
                sender: default,
                recipient: default,
                amount: 100,
                currency
            );

            var evaluation = new ActionBase.ActionEvaluation<ActionBase>()
            {
                Action = action,
                Signer = signer,
                BlockIndex = blockIndex,
                PreviousStates = states,
                OutputStates = states,
            };

            var formatter = new BinaryFormatter();
            using var ms = new MemoryStream();
            formatter.Serialize(ms, evaluation);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = (ActionBase.ActionEvaluation<ActionBase>)formatter.Deserialize(ms);

            // FIXME We should equality check more precisely.
            Assert.Equal(evaluation.Signer, deserialized.Signer);
            Assert.Equal(evaluation.BlockIndex, deserialized.BlockIndex);
            var dict = (Dictionary)deserialized.OutputStates.GetState(default);
            Assert.Equal("value", (Text)dict["key"]);
        }
    }
}
