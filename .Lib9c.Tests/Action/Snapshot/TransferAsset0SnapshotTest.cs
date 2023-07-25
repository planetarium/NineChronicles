namespace Lib9c.Tests.Action.Snapshot
{
    using System.Collections.Immutable;
    using System.Numerics;
    using System.Threading.Tasks;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using VerifyTests;
    using VerifyXunit;
    using Xunit;
    using static ActionUtils;

    [UsesVerify]
    public class TransferAsset0SnapshotTest
    {
        public TransferAsset0SnapshotTest()
        {
            VerifierSettings.SortPropertiesAlphabetically();
        }

        [Fact]
        public Task PlainValue()
        {
            var action = new TransferAsset0(
                default(Address),
                default(Address),
                Currency.Legacy("NNN", 2, null) * 100);

            return Verifier
                .Verify(action.PlainValue)
                .UseTypeName((Text)GetActionTypeId<TransferAsset0>());
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
            var context = new ActionContext();
            IAccountStateDelta state = new MockStateDelta().MintAsset(context, senderAddress, crystal * 100);
            var actionContext = new ActionContext
            {
                Signer = senderAddress,
                PreviousState = state,
            };
            var action = new TransferAsset0(
                senderAddress,
                recipientAddress,
                crystal * 100);
            var outputState = action.Execute(actionContext);

            // Verifier does not handle tuples well when nested.
            var summary = Verifier
                .Verify(outputState)
                .IgnoreMembersWithType<IImmutableSet<(Address, Currency)>>()
                .IgnoreMembersWithType<IImmutableDictionary<(Address, Currency), BigInteger>>()
                .UseTypeName((Text)GetActionTypeId<TransferAsset0>())
                .UseMethodName($"{nameof(TransferCrystal)}.summary");
            var fungibles = Verifier
                .Verify(outputState.Delta.Fungibles)
                .UseTypeName((Text)GetActionTypeId<TransferAsset0>())
                .UseMethodName($"{nameof(TransferCrystal)}.fungibles");
            return Task.WhenAll(summary, fungibles);
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
            var context = new ActionContext();
            var state = new MockStateDelta().MintAsset(context, senderAddress, crystal * 100);
            var actionContext = new ActionContext
            {
                Signer = senderAddress,
                PreviousState = state,
            };
            var action = new TransferAsset0(
                senderAddress,
                recipientAddress,
                crystal * 100,
                "MEMO");
            var outputState = action.Execute(actionContext);

            // Verifier does not handle tuples well when nested.
            var summary = Verifier
                .Verify(outputState)
                .IgnoreMembersWithType<IImmutableSet<(Address, Currency)>>()
                .IgnoreMembersWithType<IImmutableDictionary<(Address, Currency), BigInteger>>()
                .UseTypeName((Text)GetActionTypeId<TransferAsset0>())
                .UseMethodName($"{nameof(TransferWithMemo)}.summary");
            var fungibles = Verifier
                .Verify(outputState.Delta.Fungibles)
                .UseTypeName((Text)GetActionTypeId<TransferAsset0>())
                .UseMethodName($"{nameof(TransferWithMemo)}.fungibles");
            return Task.WhenAll(summary, fungibles);
        }
    }
}
