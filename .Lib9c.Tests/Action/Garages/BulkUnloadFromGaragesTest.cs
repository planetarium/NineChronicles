#nullable enable

namespace Lib9c.Tests.Action.Garages
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using Bencodex.Types;
    using Lib9c.Tests.Util;
    using Libplanet.Action.State;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Action.Garages;
    using Nekoyume.Model.Garages;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Xunit;

    public class BulkUnloadFromGaragesTest
    {
        private const int AvatarIndex = 0;

        private static readonly Address AgentAddress = new PrivateKey().ToAddress();

        private static readonly Address AvatarAddress =
            Addresses.GetAvatarAddress(AgentAddress, AvatarIndex);

        private readonly TableSheets _tableSheets;
        private readonly Currency _ncg;
        private readonly IAccount _previousStates;

        public BulkUnloadFromGaragesTest()
        {
            var initializeStates = InitializeUtil.InitializeStates(
                agentAddr: AgentAddress,
                avatarIndex: AvatarIndex);
            _tableSheets = initializeStates.tableSheets;
            _previousStates = initializeStates.initialStatesWithAvatarStateV2;
            _ncg = initializeStates.initialStatesWithAvatarStateV2.GetGoldCurrency();
        }

        public static IEnumerable<object[]> Get_Sample_PlainValue()
        {
            var avatarAddress = Addresses.GetAvatarAddress(AgentAddress, AvatarIndex);
            var fungibleAssetValues = GetFungibleAssetValues(AgentAddress, avatarAddress)
                as IEnumerable<(Address balanceAddr, FungibleAssetValue value)>;
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
                new BulkUnloadFromGarages(),
                new BulkUnloadFromGarages(new[] { unloadData }),
            };

            foreach (var action in actions)
            {
                var serialized = action.PlainValue;
                var deserialized = new BulkUnloadFromGarages();
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

        [Fact]
        public void Execute_Success()
        {
            const long blockIndex = 0L;
            var (states, unloadDataEnumerable) = RegisterPlainValue(_previousStates);
            var action = new BulkUnloadFromGarages(new[] { unloadDataEnumerable });
            states = action.Execute(new ActionContext
            {
                Signer = AgentAddress,
                BlockIndex = blockIndex,
                Rehearsal = false,
                PreviousState = states,
                RandomSeed = new TestRandom().Seed,
            });

            // Test fungibleAssetValues
            var unloadData = action.UnloadData.ToArray();
            var garageBalanceAddress = Addresses.GetGarageBalanceAddress(AgentAddress);
            if (unloadData[0].fungibleAssetValues is { } fungibleAssetValues)
            {
                foreach (var (balanceAddress, value) in fungibleAssetValues)
                {
                    Assert.Equal(value, states.GetBalance(balanceAddress, value.Currency));
                    Assert.Equal(
                        value.Currency * 0,
                        states.GetBalance(garageBalanceAddress, value.Currency));
                }
            }

            // Test fungibleItems
            if (unloadData[0].fungibleIdAndCounts is { } fungibleIdAndCounts)
            {
                var inventoryAddress = unloadData[0].recipientAvatarAddress
                    .Derive(SerializeKeys.LegacyInventoryKey);
                var inventory = states.GetInventory(inventoryAddress);

                foreach (var (fungibleId, count) in fungibleIdAndCounts)
                {
                    var garageAddress = Addresses.GetGarageAddress(AgentAddress, fungibleId);
                    Assert.Equal(0, new FungibleItemGarage(states.GetState(garageAddress)).Count);
                    Assert.True(inventory.HasFungibleItem(fungibleId, blockIndex: 0, count));
                }
            }

            // Test Mailing
            var avatarDict = (Dictionary)states.GetState(unloadData[0].recipientAvatarAddress)!;
            var mailBox = new MailBox((List)avatarDict[SerializeKeys.MailBoxKey]);
            Assert.Single(mailBox);

            var mail = Assert.IsType<UnloadFromMyGaragesRecipientMail>(mailBox.First());
            Assert.Equal(blockIndex, mail.blockIndex);
            Assert.Equal(blockIndex, mail.requiredBlockIndex);
            Assert.True(action.UnloadData[0].fungibleAssetValues?.SequenceEqual(mail.FungibleAssetValues!) ??
                        mail.FungibleAssetValues is null);
            Assert.True(action.UnloadData[0].fungibleIdAndCounts?.SequenceEqual(mail.FungibleIdAndCounts!) ??
                        mail.FungibleIdAndCounts is null);
            Assert.Equal(action.UnloadData[0].memo, mail.Memo);
        }

        private static (Address balanceAddr, FungibleAssetValue value)[]
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

        private (IAccount states, (
            Address recipientAvatarAddress,
            IEnumerable<(Address balanceAddress, FungibleAssetValue value)>? fungibleAssetValues,
            IEnumerable<(HashDigest<SHA256> fungibleId, int count)>? fungibleIdAndCounts,
            string? memo))
            RegisterPlainValue(IAccount previousStates)
        {
            var states = previousStates;

            var recipientAvatarAddress = AvatarAddress;
            var fungibleAssetValues = GetFungibleAssetValues(AgentAddress, AvatarAddress);
            var fungibleItemAndCounts = _tableSheets.MaterialItemSheet.OrderedList!
                .Take(3)
                .Select(ItemFactory.CreateMaterial)
                .Select((material, index) =>
                {
                    var garageAddress =
                        Addresses.GetGarageAddress(AgentAddress, material.FungibleId);
                    var count = index + 1;
                    var garage = new FungibleItemGarage(material, count);
                    states = states.SetState(garageAddress, garage.Serialize());
                    return (FungibleItem: (IFungibleItem)material, count);
                })
                .ToArray();
            var fungibleItemIdAndCounts = fungibleItemAndCounts
                .Select(tuple => (fungibleId: tuple.FungibleItem.FungibleId, tuple.count))
                .ToArray();

            var actionContext = new ActionContext { Signer = Addresses.Admin };
            var garageBalanceAddress = Addresses.GetGarageBalanceAddress(AgentAddress);
            foreach (var (_, value) in fungibleAssetValues)
            {
                if (value.Currency.Equals(_ncg))
                {
                    states = states.TransferAsset(
                        actionContext,
                        Addresses.Admin,
                        garageBalanceAddress,
                        value);
                }
                else
                {
                    states = states.MintAsset(
                        actionContext,
                        garageBalanceAddress,
                        value);
                }
            }

            return (
                states,
                (recipientAvatarAddress, fungibleAssetValues, fungibleItemIdAndCounts, "memo"));
        }
    }
}
