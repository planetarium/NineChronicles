namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using Bencodex.Types;
    using Libplanet.Action.State;
    using Libplanet.Common;
    using Libplanet.Crypto;
    using Libplanet.Types.Assets;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Xunit;

    public class MintAssetsTest
    {
        private readonly Address _adminAddress;
        private readonly ISet<Address> _minters;
        private readonly Currency _ncgCurrency;
        private readonly IAccount _prevState;

        private readonly TableSheets _tableSheets;

        public MintAssetsTest()
        {
            _adminAddress = new PrivateKey().ToAddress();
            _ncgCurrency = Currency.Legacy("NCG", 2, null);
            _minters = new HashSet<Address>
            {
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
                new PrivateKey().ToAddress(),
            };
            _prevState = new Account(
                MockState.Empty
                    .SetState(AdminState.Address, new AdminState(_adminAddress, 100).Serialize())
                    .SetState(Addresses.AssetMinters, new List(_minters.Select(m => m.Serialize())))
            );

            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _prevState = _prevState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(sheets);
        }

        [Fact]
        public void PlainValue()
        {
            var r = new List<MintAssets.MintSpec>()
            {
                new (default, _ncgCurrency * 100, null),
                new (new Address("0x47d082a115c63e7b58b1532d20e631538eafadde"), _ncgCurrency * 1000, null),
            };
            var act = new MintAssets(r, null);
            var expected = Dictionary.Empty
                .Add("type_id", "mint_assets")
                .Add("values", List.Empty
                    .Add(Null.Value)
                    .Add(new List(default(Address).Bencoded, (_ncgCurrency * 100).Serialize(), default(Null)))
                    .Add(new List(new Address("0x47d082a115c63e7b58b1532d20e631538eafadde").Bencoded, (_ncgCurrency * 1000).Serialize(), default(Null))));
            Assert.Equal(
                expected,
                act.PlainValue
            );

            var act2 = new MintAssets(r, "memo");
            var expected2 = Dictionary.Empty
                .Add("type_id", "mint_assets")
                .Add("values", List.Empty
                    .Add((Text)"memo")
                    .Add(new List(default(Address).Bencoded, (_ncgCurrency * 100).Serialize(), default(Null)))
                    .Add(new List(new Address("0x47d082a115c63e7b58b1532d20e631538eafadde").Bencoded, (_ncgCurrency * 1000).Serialize(), default(Null))));
            Assert.Equal(
                expected2,
                act2.PlainValue
            );
        }

        [Fact]
        public void LoadPlainValue()
        {
            var pv = Dictionary.Empty
                .Add("type_id", "mint_assets")
                .Add("values", List.Empty
                    .Add(default(Null))
                    .Add(new List(default(Address).Bencoded, (_ncgCurrency * 100).Serialize(), default(Null)))
                    .Add(new List(new Address("0x47d082a115c63e7b58b1532d20e631538eafadde").Bencoded, (_ncgCurrency * 1000).Serialize(), default(Null))));
            var act = new MintAssets();
            act.LoadPlainValue(pv);

            var expected = new List<MintAssets.MintSpec>()
            {
                new (default, _ncgCurrency * 100, null),
                new (new Address("0x47d082a115c63e7b58b1532d20e631538eafadde"), _ncgCurrency * 1000, null),
            };
            Assert.Equal(expected, act.MintSpecs);
            Assert.Null(act.Memo);

            var pv2 = Dictionary.Empty
                .Add("type_id", "mint_assets")
                .Add("values", List.Empty
                    .Add((Text)"memo")
                    .Add(new List(default(Address).Bencoded, (_ncgCurrency * 100).Serialize(), default(Null)))
                    .Add(new List(new Address("0x47d082a115c63e7b58b1532d20e631538eafadde").Bencoded, (_ncgCurrency * 1000).Serialize(), default(Null))));
            var act2 = new MintAssets();
            act2.LoadPlainValue(pv2);
            Assert.Equal("memo", act2.Memo);
        }

        [Fact]
        public void Execute_With_FungibleAssetValue()
        {
            var action = new MintAssets(
                new List<MintAssets.MintSpec>()
                {
                    new (default, _ncgCurrency * 100, null),
                    new (new Address("0x47d082a115c63e7b58b1532d20e631538eafadde"), _ncgCurrency * 1000, null),
                },
                null
            );
            IAccount nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = _prevState,
                    Signer = _minters.First(),
                    Rehearsal = false,
                    BlockIndex = 1,
                }
            );

            Assert.Equal(
                _ncgCurrency * 100,
                nextState.GetBalance(default, _ncgCurrency)
            );
            Assert.Equal(
                _ncgCurrency * 1000,
                nextState.GetBalance(new Address("0x47d082a115c63e7b58b1532d20e631538eafadde"), _ncgCurrency)
            );
        }

        [Fact]
        public void Execute_With_FungibleItemValue()
        {
            IAccount prevState = GenerateAvatar(_prevState, out Address avatarAddress);
            HashDigest<SHA256> fungibleId = HashDigest<SHA256>.FromString(
                "7f5d25371e58c0f3d5a33511450f73c2e0fa4fac32a92e1cbe64d3bf2fef6328"
            );

            var action = new MintAssets(
                new List<MintAssets.MintSpec>()
                {
                    new (
                        avatarAddress,
                        null,
                        new FungibleItemValue(fungibleId, 42)
                    ),
                },
                "Execute_With_FungibleItemValue"
            );
            IAccount nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = prevState,
                    Signer = _minters.First(),
                    Rehearsal = false,
                    BlockIndex = 1,
                }
            );

            var inventory = nextState.GetInventory(avatarAddress.Derive(SerializeKeys.LegacyInventoryKey));
            Assert.Contains(inventory.Items, i => i.count == 42 && i.item is Material m && m.FungibleId.Equals(fungibleId));

            var avatarDict = Assert.IsType<Dictionary>(nextState.GetState(avatarAddress));
            var mailBox = new MailBox((List)avatarDict[SerializeKeys.MailBoxKey]);
            Assert.Single(mailBox);
            var mail = Assert.IsType<UnloadFromMyGaragesRecipientMail>(mailBox.First());
            Assert.Equal(new[] { (fungibleId, 42) }, mail.FungibleIdAndCounts);
            Assert.Equal(action.Memo, mail.Memo);
        }

        [Fact]
        public void Execute_With_Mixed()
        {
            IAccount prevState = GenerateAvatar(_prevState, out Address avatarAddress);
            HashDigest<SHA256> fungibleId = HashDigest<SHA256>.FromString(
                "7f5d25371e58c0f3d5a33511450f73c2e0fa4fac32a92e1cbe64d3bf2fef6328"
            );

            var action = new MintAssets(
                new List<MintAssets.MintSpec>()
                {
                    new (new Address("0x47d082a115c63e7b58b1532d20e631538eafadde"), _ncgCurrency * 1000, null),
                    new (
                        avatarAddress,
                        Currencies.StakeRune * 123,
                        null
                    ),
                    new (
                        avatarAddress,
                        null,
                        new FungibleItemValue(fungibleId, 42)
                    ),
                },
                "Execute_With_FungibleItemValue"
            );
            IAccount nextState = action.Execute(
                new ActionContext()
                {
                    PreviousState = prevState,
                    Signer = _minters.First(),
                    Rehearsal = false,
                    BlockIndex = 1,
                }
            );

            var inventory = nextState.GetInventory(avatarAddress.Derive(SerializeKeys.LegacyInventoryKey));
            Assert.Contains(inventory.Items, i => i.count == 42 && i.item is Material m && m.FungibleId.Equals(fungibleId));

            var avatarDict = Assert.IsType<Dictionary>(nextState.GetState(avatarAddress));
            var mailBox = new MailBox((List)avatarDict[SerializeKeys.MailBoxKey]);
            Assert.Single(mailBox);
            var mail = Assert.IsType<UnloadFromMyGaragesRecipientMail>(mailBox.First());
            Assert.Equal(new[] { (fungibleId, 42) }, mail.FungibleIdAndCounts);
            Assert.Equal(new[] { (avatarAddress, Currencies.StakeRune * 123) }, mail.FungibleAssetValues);
            Assert.Equal(action.Memo, mail.Memo);
        }

        [Fact]
        public void Execute_Throws_InvalidMinterException()
        {
            var action = new MintAssets(
                new List<MintAssets.MintSpec>()
                {
                    new (default, _ncgCurrency * 100, null),
                    new (new Address("0x47d082a115c63e7b58b1532d20e631538eafadde"), _ncgCurrency * 1000, null),
                },
                null
            );

            // Allows admin
            _ = action.Execute(
                new ActionContext()
                {
                    PreviousState = _prevState,
                    Signer = _adminAddress,
                    Rehearsal = false,
                    BlockIndex = 1,
                }
            );

            // Allows minters
            foreach (Address m in _minters)
            {
                _ = action.Execute(
                    new ActionContext()
                    {
                        PreviousState = _prevState,
                        Signer = m,
                        Rehearsal = false,
                        BlockIndex = 1,
                    }
                );
            }

            // Denies others
            Assert.Throws<InvalidMinterException>(() => action.Execute(
                new ActionContext()
                {
                    PreviousState = _prevState,
                    Signer = default,
                    Rehearsal = false,
                    BlockIndex = 1,
                }
            ));
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
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(
                    avatarAddress.Derive(SerializeKeys.LegacyInventoryKey),
                    avatarState.inventory.Serialize());

            return state;
        }
    }
}
