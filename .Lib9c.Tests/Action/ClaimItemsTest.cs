namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;

    public class ClaimItemsTest
    {
        private readonly IAccount _initialState;
        private readonly Address _signerAddress;

        private readonly TableSheets _tableSheets;
        private readonly List<Currency> _itemCurrencies;
        private readonly List<Currency> _wrappedFavCurrencies;
        private readonly List<int> _itemIds;
        private readonly Currency _wrappedCrystalCurrency;

        public ClaimItemsTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialState = new Account(MockState.Empty);

            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);
            _itemIds = _tableSheets.CostumeItemSheet.Values.Take(3).Select(x => x.Id).ToList();
            _itemCurrencies = _itemIds.Select(id => Currencies.GetItemCurrency(id, true)).ToList();
            _wrappedFavCurrencies = new List<Currency>();
            foreach (var currency in new[] { Currencies.Crystal, Currencies.StakeRune })
            {
                var wrappedCurrency = Currencies.GetWrappedCurrency(currency);
                _wrappedFavCurrencies.Add(wrappedCurrency);
                if (currency.Ticker == "CRYSTAL")
                {
                    _wrappedCrystalCurrency = wrappedCurrency;
                }
            }

            _signerAddress = new PrivateKey().ToAddress();

            var context = new ActionContext();
            _initialState = _initialState
                .MintAsset(context, _signerAddress, _itemCurrencies[0] * 5)
                .MintAsset(context, _signerAddress, _itemCurrencies[1] * 5)
                .MintAsset(context, _signerAddress, _itemCurrencies[2] * 5)
                .MintAsset(context, _signerAddress, _wrappedFavCurrencies[0] * 5)
                .MintAsset(context, _signerAddress, _wrappedFavCurrencies[1] * 5);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Serialize(bool memoExist)
        {
            var states = GenerateAvatar(_initialState, out var avatarAddress1, out _);
            GenerateAvatar(states, out var avatarAddress2, out _);
            string memo = memoExist ? "memo" : null;
            var action = new ClaimItems(
                new List<(Address, IReadOnlyList<FungibleAssetValue>)>
                {
                    (avatarAddress1, new List<FungibleAssetValue> { _itemCurrencies[0] * 1, _itemCurrencies[1] * 1 }),
                    (avatarAddress2, new List<FungibleAssetValue> { _itemCurrencies[0] * 1 }),
                },
                memo
            );
            Assert.Equal(!memoExist, string.IsNullOrEmpty(action.Memo));
            Dictionary serialized = (Dictionary)action.PlainValue;
            Dictionary values = (Dictionary)serialized["values"];
            Assert.Equal(memoExist, values.ContainsKey("m"));
            var deserialized = new ClaimItems();
            deserialized.LoadPlainValue(action.PlainValue);

            foreach (var i in Enumerable.Range(0, 2))
            {
                Assert.Equal(action.ClaimData[i].address, deserialized.ClaimData[i].address);
                Assert.True(action.ClaimData[i].fungibleAssetValues
                    .SequenceEqual(deserialized.ClaimData[i].fungibleAssetValues));
            }

            Assert.Equal(action.Memo, deserialized.Memo);
        }

        [Fact]
        public void Execute_Throws_ArgumentException_TickerInvalid()
        {
            var state = GenerateAvatar(_initialState, out var recipientAvatarAddress, out _);

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
                    RandomSeed = 0,
                }));
        }

        [Fact]
        public void Execute_Throws_WhenNotEnoughBalance()
        {
            var state = GenerateAvatar(_initialState, out var recipientAvatarAddress, out _);

            var currency = _itemCurrencies.First();
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
                    RandomSeed = 0,
                }));
        }

        [Theory]
        [InlineData("memo")]
        [InlineData(null)]
        public void Execute(string memo)
        {
            var state = GenerateAvatar(_initialState, out var recipientAvatarAddress, out var recipientAgentAddress);

            var avatarValues = _itemCurrencies.Select(currency => currency * 1).ToList();
            var agentValues = new List<FungibleAssetValue>();
            foreach (var currency in _wrappedFavCurrencies)
            {
                if (currency.Equals(_wrappedCrystalCurrency))
                {
                    agentValues.Add(1 * currency);
                }
                else
                {
                    avatarValues.Add(1 * currency);
                }
            }

            var fungibleAssetValues = avatarValues.Concat(agentValues).ToList();

            var action = new ClaimItems(
                new List<(Address, IReadOnlyList<FungibleAssetValue>)>
                {
                    (recipientAvatarAddress, fungibleAssetValues),
                },
                memo
            );
            var states = action.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = _signerAddress,
                BlockIndex = 0,
                RandomSeed = 0,
            });

            var avatarState = states.GetAvatarStateV2(recipientAvatarAddress);
            var mail = Assert.IsType<ClaimItemsMail>(avatarState.mailBox.Single());
            if (string.IsNullOrEmpty(memo))
            {
                Assert.Null(mail.Memo);
            }
            else
            {
                Assert.Equal(memo, mail.Memo);
            }

            Assert.Equal(0, mail.blockIndex);
            Assert.Equal(0, mail.requiredBlockIndex);

            var inventory = states.GetInventory(recipientAvatarAddress.Derive(SerializeKeys.LegacyInventoryKey));
            foreach (var i in Enumerable.Range(0, 3))
            {
                Assert.Equal(_itemCurrencies[i] * 4, states.GetBalance(_signerAddress, _itemCurrencies[i]));
                var itemId = _itemIds[i];
                Assert.Equal(
                    1,
                    inventory.Items.First(x => x.item.Id == itemId).count);
                var mailItems = mail.Items.Single(m => m.id == itemId);
                Assert.Equal(1, mailItems.count);
            }

            for (int i = 0; i < _wrappedFavCurrencies.Count; i++)
            {
                var wrappedCurrency = _wrappedFavCurrencies[i];
                Assert.Equal(wrappedCurrency * 4, states.GetBalance(_signerAddress, wrappedCurrency));
                var currency = Currencies.GetUnwrappedCurrency(wrappedCurrency);
                var recipientAddress = Currencies.SelectRecipientAddress(
                    currency,
                    recipientAgentAddress,
                    recipientAvatarAddress
                    );
                Assert.Equal(currency * 1, states.GetBalance(recipientAddress, currency));
                var mailFav = mail.FungibleAssetValues.Single(f => f.Currency.Equals(currency));
                Assert.Equal(currency * 1, mailFav);
            }
        }

        [Fact]
        public void Execute_WithMultipleRecipients()
        {
            var state = GenerateAvatar(_initialState, out var recipientAvatarAddress1, out _);
            state = GenerateAvatar(state, out var recipientAvatarAddress2, out _);

            var recipientAvatarAddresses = new List<Address>
            {
                recipientAvatarAddress1, recipientAvatarAddress2,
            };
            var fungibleAssetValues = _itemCurrencies.Select(currency => currency * 1).ToList();

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
                RandomSeed = 0,
            });

            Assert.Equal(states.GetBalance(_signerAddress, _itemCurrencies[0]), _itemCurrencies[0] * 3);
            Assert.Equal(states.GetBalance(_signerAddress, _itemCurrencies[1]), _itemCurrencies[1] * 3);
            Assert.Equal(states.GetBalance(_signerAddress, _itemCurrencies[2]), _itemCurrencies[2] * 4);

            var inventory1 = states.GetInventory(recipientAvatarAddress1.Derive(SerializeKeys.LegacyInventoryKey));
            Assert.Equal(1, inventory1.Items.First(x => x.item.Id == _itemIds[0]).count);
            Assert.Equal(1, inventory1.Items.First(x => x.item.Id == _itemIds[1]).count);

            var inventory2 = states.GetInventory(recipientAvatarAddress2.Derive(SerializeKeys.LegacyInventoryKey));
            Assert.Equal(1, inventory2.Items.First(x => x.item.Id == _itemIds[0]).count);
            Assert.Equal(1, inventory2.Items.First(x => x.item.Id == _itemIds[1]).count);
            Assert.Equal(1, inventory2.Items.First(x => x.item.Id == _itemIds[2]).count);
        }

        [Fact]
        public void Execute_WithNonFungibleItem()
        {
            const int nonFungibleitemId = 10232001;
            const int itemCount = 5;
            var currency = Currency.Legacy($"Item_T_{nonFungibleitemId}", 0, null);

            var state = GenerateAvatar(_initialState, out var recipientAvatarAddress1, out _);
            state = state.MintAsset(
                new ActionContext
                {
                    PreviousState = state,
                    Signer = _signerAddress,
                    BlockIndex = 0,
                    RandomSeed = 0,
                },
                _signerAddress,
                currency * itemCount);

            var action = new ClaimItems(new List<(Address, IReadOnlyList<FungibleAssetValue>)>
            {
                (recipientAvatarAddress1, new List<FungibleAssetValue> { currency * itemCount, }),
            });

            var states = action.Execute(new ActionContext
            {
                PreviousState = state,
                Signer = _signerAddress,
                BlockIndex = 0,
                RandomSeed = 0,
            });

            Assert.Equal(states.GetBalance(_signerAddress, currency), currency * 0);

            var inventory = states.GetInventory(recipientAvatarAddress1.Derive(SerializeKeys.LegacyInventoryKey));
            Assert.Equal(itemCount, inventory.Items.Count(x => x.item.Id == nonFungibleitemId));
        }

        [Fact]
        public void Execute_WithIncorrectClaimData()
        {
            var fungibleAssetValues = _itemCurrencies.Select(currency => currency * 1).ToList();

            var action = new ClaimItems(Enumerable.Repeat(0, 101)
                .Select(_ => (new PrivateKey().ToAddress(),
                    (IReadOnlyList<FungibleAssetValue>)fungibleAssetValues))
                .ToImmutableList());

            Assert.Throws<ArgumentOutOfRangeException>("ClaimData", () =>
                action.Execute(new ActionContext
                {
                    PreviousState = _initialState,
                    Signer = _signerAddress,
                    BlockIndex = 0,
                    RandomSeed = 0,
                }));
        }

        private IAccount GenerateAvatar(IAccount state, out Address avatarAddress, out Address agentAddress)
        {
            agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(agentAddress);
            avatarAddress = agentAddress.Derive("avatar");
            var rankingMapAddress = new PrivateKey().ToAddress();
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
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
                .SetState(agentAddress, agentState.Serialize())
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(
                    avatarAddress.Derive(SerializeKeys.LegacyWorldInformationKey),
                    avatarState.worldInformation.Serialize()
                )
                .SetState(
                    avatarAddress.Derive(SerializeKeys.LegacyQuestListKey),
                    avatarState.questList.Serialize()
                )
                .SetState(
                    avatarAddress.Derive(SerializeKeys.LegacyInventoryKey),
                    avatarState.inventory.Serialize());

            return state;
        }
    }
}
