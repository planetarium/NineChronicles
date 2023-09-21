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
        private readonly Address _recipientAvatarAddress;
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

            var tableSheets = new TableSheets(sheets);
            _itemIds = tableSheets.CostumeItemSheet.Values.Take(3).Select(x => x.Id).ToList();
            _currencies = _itemIds.Select(id => Currency.Legacy($"IT_{id}", 0, minters: null)).ToList();

            _signerAddress = new PrivateKey().ToAddress();
            var recipientAddress = new PrivateKey().ToAddress();
            var recipientAgentState = new AgentState(recipientAddress);
            _recipientAvatarAddress = recipientAddress.Derive("avatar");
            var rankingMapAddress = new PrivateKey().ToAddress();
            var recipientAvatarState = new AvatarState(
                _recipientAvatarAddress,
                recipientAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInShop),
            };
            recipientAgentState.avatarAddresses[0] = _recipientAvatarAddress;

            var context = new ActionContext();
            _initialState = _initialState
                .SetState(recipientAddress, recipientAgentState.Serialize())
                .SetState(_recipientAvatarAddress, recipientAvatarState.Serialize())
                .MintAsset(context, _signerAddress, _currencies[0] * 1)
                .MintAsset(context, _signerAddress, _currencies[1] * 1)
                .MintAsset(context, _signerAddress, _currencies[2] * 1);
        }

        [Fact]
        public void Execute_Throws_ArgumentException_TickerInvalid()
        {
            var currency = Currencies.Crystal;
            var action = new ClaimItems(_recipientAvatarAddress, new[] { currency * 1 });
            Assert.Throws<ArgumentException>(() =>
                action.Execute(new ActionContext
                {
                    PreviousState = _initialState,
                    Signer = _signerAddress,
                    BlockIndex = 100,
                    Random = new TestRandom(),
                }));
        }

        [Fact]
        public void Execute_Throws_WhenNotEnoughBalance()
        {
            var currency = _currencies.First();
            var action = new ClaimItems(_recipientAvatarAddress, new[] { currency * 2 });
            Assert.Throws<NotEnoughFungibleAssetValueException>(() =>
                action.Execute(new ActionContext
                {
                    PreviousState = _initialState,
                    Signer = _signerAddress,
                    BlockIndex = 100,
                    Random = new TestRandom(),
                }));
        }

        [Fact]
        public void Execute()
        {
            var fungibleAssetValues = _currencies.Select(currency => currency * 1);
            var action = new ClaimItems(_recipientAvatarAddress, fungibleAssetValues);
            var states = action.Execute(new ActionContext
            {
                PreviousState = _initialState,
                Signer = _signerAddress,
                BlockIndex = 0,
                Random = new TestRandom(),
            });

            var avatarState = states.GetAvatarState(_recipientAvatarAddress);
            foreach (var i in Enumerable.Range(0, 3))
            {
                Assert.Equal(_currencies[i] * 0, states.GetBalance(_signerAddress, _currencies[i]));
                Assert.Equal(
                    1,
                    avatarState.inventory.Items.First(x => x.item.Id == _itemIds[i]).count);
            }
        }
    }
}
