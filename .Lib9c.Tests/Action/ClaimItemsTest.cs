namespace Lib9c.Tests.Action
{
    using System;
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
        private readonly TableSheets _tableSheets;
        private readonly Address _signerAddress;
        private readonly Address _recipientAddress;
        private readonly Address _recipientAvatarAddress;
        private readonly Currency _currency;

        private readonly int _itemId;

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
            _itemId = _tableSheets.CostumeItemSheet.Values.First().Id;
            _currency = Currency.Legacy($"it_{_itemId}", 0, minters: null);

            _signerAddress = new PrivateKey().ToAddress();
            _recipientAddress = new PrivateKey().ToAddress();
            var recipientAgentState = new AgentState(_recipientAddress);
            _recipientAvatarAddress = _recipientAddress.Derive("avatar");
            var rankingMapAddress = new PrivateKey().ToAddress();
            var recipientAvatarState = new AvatarState(
                _recipientAvatarAddress,
                _recipientAddress,
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
            recipientAgentState.avatarAddresses[0] = _recipientAvatarAddress;

            var context = new ActionContext();
            _initialState = _initialState
                .SetState(_recipientAddress, recipientAgentState.Serialize())
                .SetState(_recipientAvatarAddress, recipientAvatarState.Serialize())
                .MintAsset(context, _signerAddress, _currency * 1);
        }

        [Fact]
        public void Execute_Throws_ArgumentException_TickerInvalid()
        {
            var currency = Currencies.Crystal;
            var action = new ClaimItems(_recipientAvatarAddress, currency * 1);
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
            var action = new ClaimItems(_recipientAvatarAddress, _currency * 2);
            Assert.Throws<NotEnoughFungibleAssetValueException>(() =>
                action.Execute(new ActionContext
                {
                    PreviousState = _initialState,
                    Signer = _signerAddress,
                    BlockIndex = 100,
                    Random = new TestRandom(),
                }));

            var differentItemId = _tableSheets.ItemSheet.Values.First().Id;
            var differentCurrency = Currency.Legacy($"it_{differentItemId}", 0, minters: null);
            action = new ClaimItems(_recipientAddress, differentCurrency * 1);
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
            var fungibleAssetValue = new FungibleAssetValue(_currency, 1, 0);
            var action = new ClaimItems(_recipientAvatarAddress, fungibleAssetValue);
            var states = action.Execute(new ActionContext
            {
                PreviousState = _initialState,
                Signer = _signerAddress,
                BlockIndex = 0,
                Random = new TestRandom(),
            });

            var avatarState = states.GetAvatarState(_recipientAvatarAddress);
            Assert.Equal(_currency * 0, states.GetBalance(_signerAddress, _currency));
            Assert.Equal(
                1,
                avatarState.inventory.Items.First(x => x.item.Id == _itemId).count);
        }
    }
}
