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
            _currencies = _itemIds.Select(id => Currency.Legacy($"IT_{id}", 0, minters: null)).ToList();

            _signerAddress = new PrivateKey().ToAddress();

            var context = new ActionContext();
            _initialState = _initialState
                .MintAsset(context, _signerAddress, _currencies[0] * 5)
                .MintAsset(context, _signerAddress, _currencies[1] * 5)
                .MintAsset(context, _signerAddress, _currencies[2] * 5);
        }

        [Fact]
        public void Execute_Throws_ArgumentException_TickerInvalid()
        {
            var state = GenerateAvatar(_initialState, out var recipientAvatarAddress);

            var currency = Currencies.Crystal;
            var action = new ClaimItems(new[] { recipientAvatarAddress }, new[] { currency * 1 });
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
            var action = new ClaimItems(new[] { recipientAvatarAddress }, new[] { currency * 6 });
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

            var fungibleAssetValues = _currencies.Select(currency => currency * 1);
            var action = new ClaimItems(new[] { recipientAvatarAddress }, fungibleAssetValues);
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
            state = GenerateAvatar(state, out var recipientAvatarAddress3);

            var recipientAvatarAddresses = new[]
            {
                recipientAvatarAddress1, recipientAvatarAddress2, recipientAvatarAddress3,
            };
            var fungibleAssetValues = _currencies.Select(currency => currency * 1);
            var action = new ClaimItems(recipientAvatarAddresses, fungibleAssetValues);
            var states = action.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = _signerAddress,
                BlockIndex = 0,
                Random = new TestRandom(),
            });

            foreach (var avatarAddress in recipientAvatarAddresses)
            {
                var inventory = states.GetInventory(avatarAddress.Derive(SerializeKeys.LegacyInventoryKey));
                foreach (var i in Enumerable.Range(0, 3))
                {
                    Assert.Equal(_currencies[i] * 2, states.GetBalance(_signerAddress, _currencies[i]));
                    Assert.Equal(
                        1,
                        inventory.Items.First(x => x.item.Id == _itemIds[i]).count);
                }
            }
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
