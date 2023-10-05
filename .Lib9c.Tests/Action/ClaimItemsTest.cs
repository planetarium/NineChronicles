namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class ClaimItemsTest
    {
        private readonly IAccount _initialState;
        private readonly Address _signerAddress;

        private readonly TableSheets _tableSheets;
        private readonly List<Currency> _currencies;
        private readonly List<int> _itemIds;

        public ClaimItemsTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new MockStateDelta();

            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);
            _itemIds = _tableSheets.CostumeItemSheet.Values.Take(3).Select(x => x.Id).ToList();
            _currencies = _itemIds.Select(id => Currency.Legacy($"Item_T_{id}", 0, minters: null)).ToList();

            _signerAddress = new PrivateKey().ToAddress();

            var context = new ActionContext();
            _initialState = _initialState
                .MintAsset(context, _signerAddress, _currencies[0] * 5)
                .MintAsset(context, _signerAddress, _currencies[1] * 5)
                .MintAsset(context, _signerAddress, _currencies[2] * 5);
        }

        [Fact]
        public void Serialize()
        {
            var states = GenerateAvatar(_initialState, out var avatarAddress1);
            GenerateAvatar(states, out var avatarAddress2);

            var action = new ClaimItems(new List<(Address, IReadOnlyList<FungibleAssetValue>)>
                {
                    (avatarAddress1, new List<FungibleAssetValue> { _currencies[0] * 1, _currencies[1] * 1 }),
                    (avatarAddress2, new List<FungibleAssetValue> { _currencies[0] * 1 }),
                });
            var deserialized = new ClaimItems();
            deserialized.LoadPlainValue(action.PlainValue);

            var orderedClaimData = action.ClaimData.OrderBy(x => x.address).ToList();

            foreach (var i in Enumerable.Range(0, 2))
            {
                Assert.Equal(orderedClaimData[i].address, deserialized.ClaimData[i].address);
                Assert.True(orderedClaimData[i].fungibleAssetValues
                    .SequenceEqual(deserialized.ClaimData[i].fungibleAssetValues));
            }
        }

        [Fact]
        public void Execute_Throws_ArgumentException_TickerInvalid()
        {
            var state = GenerateAvatar(_initialState, out var recipientAvatarAddress);

            var currency = Currencies.Crystal;
            var action = new ClaimItems(new List<(Address, IReadOnlyList<FungibleAssetValue>)>
            {
                (recipientAvatarAddress, new List<FungibleAssetValue> { currency * 1 }),
            });
            Assert.Throws<ArgumentException>(() =>
                action.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = _signerAddress,
                    BlockIndex = 100,
                    Random = new TestRandom(),
                }));
        }

        [Fact]
        public void Execute_Throws_WhenNotEnoughBalance()
        {
            var state = GenerateAvatar(_initialState, out var recipientAvatarAddress);

            var currency = _currencies.First();
            var action = new ClaimItems(new List<(Address, IReadOnlyList<FungibleAssetValue>)>
            {
                (recipientAvatarAddress, new List<FungibleAssetValue> { currency * 6 }),
            });
            Assert.Throws<InsufficientBalanceException>(() =>
                action.Execute(new ActionContext
                {
                    PreviousState = state,
                    Signer = _signerAddress,
                    BlockIndex = 100,
                    Random = new TestRandom(),
                }));
        }

        [Fact]
        public void Execute()
        {
            var state = GenerateAvatar(_initialState, out var recipientAvatarAddress);

            var fungibleAssetValues = _currencies.Select(currency => currency * 1).ToList();
            var action = new ClaimItems(new List<(Address, IReadOnlyList<FungibleAssetValue>)>
            {
                (recipientAvatarAddress, fungibleAssetValues),
            });
            var states = action.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = _signerAddress,
                BlockIndex = 0,
                Random = new TestRandom(),
            });

            var inventory = states.GetInventory(recipientAvatarAddress.Derive(SerializeKeys.LegacyInventoryKey));
            foreach (var i in Enumerable.Range(0, 3))
            {
                Assert.Equal(_currencies[i] * 4, states.GetBalance(_signerAddress, _currencies[i]));
                Assert.Equal(
                    1,
                    inventory.Items.First(x => x.item.Id == _itemIds[i]).count);
            }
        }

        [Fact]
        public void Execute_WithMultipleRecipients()
        {
            var state = GenerateAvatar(_initialState, out var recipientAvatarAddress1);
            state = GenerateAvatar(state, out var recipientAvatarAddress2);

            var recipientAvatarAddresses = new List<Address>
            {
                recipientAvatarAddress1, recipientAvatarAddress2,
            };
            var fungibleAssetValues = _currencies.Select(currency => currency * 1).ToList();

            var action = new ClaimItems(new List<(Address, IReadOnlyList<FungibleAssetValue>)>
            {
                (recipientAvatarAddress1, fungibleAssetValues.Take(2).ToList()),
                (recipientAvatarAddress2, fungibleAssetValues),
            });

            var states = action.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = _signerAddress,
                BlockIndex = 0,
                Random = new TestRandom(),
            });

            Assert.Equal(states.GetBalance(_signerAddress, _currencies[0]), _currencies[0] * 3);
            Assert.Equal(states.GetBalance(_signerAddress, _currencies[1]), _currencies[1] * 3);
            Assert.Equal(states.GetBalance(_signerAddress, _currencies[2]), _currencies[2] * 4);

            var inventory1 = states.GetInventory(recipientAvatarAddress1.Derive(SerializeKeys.LegacyInventoryKey));
            Assert.Equal(1, inventory1.Items.First(x => x.item.Id == _itemIds[0]).count);
            Assert.Equal(1, inventory1.Items.First(x => x.item.Id == _itemIds[1]).count);

            var inventory2 = states.GetInventory(recipientAvatarAddress2.Derive(SerializeKeys.LegacyInventoryKey));
            Assert.Equal(1, inventory2.Items.First(x => x.item.Id == _itemIds[0]).count);
            Assert.Equal(1, inventory2.Items.First(x => x.item.Id == _itemIds[1]).count);
            Assert.Equal(1, inventory2.Items.First(x => x.item.Id == _itemIds[2]).count);
        }

        private IAccount GenerateAvatar(IAccount state, out Address avatarAddress)
        {
            var address = new PrivateKey().ToAddress();
            var agentState = new AgentState(address);
            avatarAddress = address.Derive("avatar");
            var rankingMapAddress = new PrivateKey().ToAddress();
            var avatarState = new AvatarState(
                avatarAddress,
                address,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            agentState.avatarAddresses[0] = avatarAddress;

            state = state
                .SetState(address, agentState.Serialize())
                .SetState(avatarAddress, avatarState.Serialize())
                .SetState(
                    avatarAddress.Derive(SerializeKeys.LegacyInventoryKey),
                    avatarState.inventory.Serialize());

            return state;
        }
    }
}
