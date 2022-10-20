namespace Lib9c.Tests.Action.Snapshot
{
    using System.Threading.Tasks;
    using Libplanet;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using VerifyXunit;
    using Xunit;
    using static ActionUtils;

    [UsesVerify]
    public class TransferAsset0SnapshotTest
    {
        [Fact]
        public Task PlainValue()
        {
            var action = new TransferAsset0(
                default(Address),
                default(Address),
                Currency.Legacy("NNN", 2, null) * 100);

            return Verifier.Verify(action.PlainValue)
                .UseTypeName(GetActionTypeId<TransferAsset0>());
        }

        [Fact]
        public Task TransferCrystal()
        {
            var senderPrivateKey =
                new PrivateKey(ByteUtil.ParseHex(
                    "810234bc093e2b66406b06dd0c2d2d3320bc5f19caef7acd3f800424bd46cb60"));
            var recipientPrivateKey =
                new PrivateKey(ByteUtil.ParseHex(
                    "f8960846e9ae4ad1c23686f74c8e5f80f22336b6f2175be21db82afa8823c92d"));
            var senderAddress = senderPrivateKey.ToAddress();
            var recipientAddress = recipientPrivateKey.ToAddress();
            var crystal = CrystalCalculator.CRYSTAL;
            var state = new State().MintAsset(senderAddress, crystal * 100);
            var actionContext = new ActionContext
            {
                Signer = senderAddress,
                PreviousStates = state,
            };
            var action = new TransferAsset0(
                senderAddress,
                recipientAddress,
                crystal * 100);
            var states = action.Execute(actionContext);

            return Verifier.Verify(states)
                .UseTypeName(GetActionTypeId<TransferAsset0>());
        }

        [Fact]
        public Task TransferWithMemo()
        {
            var senderPrivateKey =
                new PrivateKey(ByteUtil.ParseHex(
                    "810234bc093e2b66406b06dd0c2d2d3320bc5f19caef7acd3f800424bd46cb60"));
            var recipientPrivateKey =
                new PrivateKey(ByteUtil.ParseHex(
                    "f8960846e9ae4ad1c23686f74c8e5f80f22336b6f2175be21db82afa8823c92d"));
            var senderAddress = senderPrivateKey.ToAddress();
            var recipientAddress = recipientPrivateKey.ToAddress();
            var crystal = CrystalCalculator.CRYSTAL;
            var state = new State().MintAsset(senderAddress, crystal * 100);
            var actionContext = new ActionContext
            {
                Signer = senderAddress,
                PreviousStates = state,
            };
            var action = new TransferAsset0(
                senderAddress,
                recipientAddress,
                crystal * 100,
                "MEMO");
            var states = action.Execute(actionContext);

            return Verifier.Verify(states)
                .UseTypeName(GetActionTypeId<TransferAsset0>());
        }
    }
}
