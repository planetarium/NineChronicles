#nullable enable

namespace Lib9c.Tests.Action.Garages
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action.Garages;
    using Xunit;

    public class UnloadFromGaragesTest
    {
        private static readonly Address AgentAddress = new PrivateKey().ToAddress();
        private static readonly int AvatarIndex = 0;

        public static IEnumerable<object[]> Get_Sample_PlainValue()
        {
            var avatarAddress = Addresses.GetAvatarAddress(AgentAddress, AvatarIndex);
            var fungibleAssetValues = GetFungibleAssetValues(AgentAddress, avatarAddress);
            var hex = string.Join(
                string.Empty,
                Enumerable.Range(0, 64).Select(i => (i % 10).ToString()));
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)> fungibleIdAndCounts = new[]
            {
                (HashDigest<SHA256>.FromString(hex), 1),
                (HashDigest<SHA256>.FromString(hex), int.MaxValue),
            };

            yield return new object[]
            {
                (avatarAddress, fungibleAssetValues, fungibleIdAndCounts, memo: "memo"),
            };
        }

        [Theory]
        [MemberData(nameof(Get_Sample_PlainValue))]
        public void Serialize(
            (
                Address recipientAvatarAddress,
                IEnumerable<(Address balanceAddress, FungibleAssetValue value)>?
                fungibleAssetValues,
                IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
                string? memo) unloadData)
        {
            var actions = new[]
            {
                new UnloadFromGarages(),
                new UnloadFromGarages(new[] { unloadData }),
            };

            foreach (var action in actions)
            {
                var serialized = action.PlainValue;
                var deserialized = new UnloadFromGarages();
                deserialized.LoadPlainValue(serialized);

                Assert.Equal(action.UnloadData.Count, deserialized.UnloadData.Count);
                Assert.Equal(serialized, deserialized.PlainValue);

                for (var i = 0; i < action.UnloadData.Count; i++)
                {
                    var deserializedData = deserialized.UnloadData[i];
                    var actionData = action.UnloadData[i];

                    Assert.Equal(
                        actionData.recipientAvatarAddress,
                        deserializedData.recipientAvatarAddress);
                    Assert.True(
                        actionData.fungibleAssetValues?.SequenceEqual(deserializedData
                            .fungibleAssetValues!)
                        ?? deserializedData.fungibleAssetValues is null);
                    Assert.True(
                        actionData.fungibleIdAndCounts?.SequenceEqual(deserializedData
                            .fungibleIdAndCounts!)
                        ?? deserializedData.fungibleIdAndCounts is null);
                    Assert.Equal(actionData.memo, deserializedData.memo);
                }
            }
        }

        private static IEnumerable<(Address balanceAddr, FungibleAssetValue value)>
            GetFungibleAssetValues(
                Address agentAddr,
                Address avatarAddr)
        {
            return CurrenciesTest.GetSampleCurrencies()
                .Select(objects => (FungibleAssetValue)objects[0])
                .Where(fav => fav.Sign > 0)
                .Select(fav =>
                {
                    if (Currencies.IsRuneTicker(fav.Currency.Ticker) ||
                        Currencies.IsSoulstoneTicker(fav.Currency.Ticker))
                    {
                        return (avatarAddr, fav);
                    }

                    return (agentAddr, fav);
                })
                .ToArray();
        }
    }
}
