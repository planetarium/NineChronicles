namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Nekoyume.Model.State.RedeemCodeState;

    public class RedeemCode0Test
    {
        private readonly Address _agentAddress = new Address(new byte[]
        {
            0x10, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x02,
        });

        private readonly Address _avatarAddress = new Address(new byte[]
        {
            0x10, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x01,
        });

        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;

        public RedeemCode0Test()
        {
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);
        }

        [Fact]
        public void Execute()
        {
            var privateKey = new PrivateKey();
            PublicKey publicKey = privateKey.PublicKey;
            var prevRedeemCodesState = new RedeemCodeState(new Dictionary<PublicKey, Reward>()
            {
                [publicKey] = new Reward(1),
            });
            var gameConfigState = new GameConfigState();
            var agentState = new AgentState(_agentAddress);
            agentState.avatarAddresses[0] = _avatarAddress;
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                1,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            );

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var goldState = new GoldCurrencyState(Currency.Legacy("NCG", 2, null));
#pragma warning restore CS0618

            var initialState = new State()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(RedeemCodeState.Address, prevRedeemCodesState.Serialize())
                .SetState(GoldCurrencyState.Address, goldState.Serialize())
                .MintAsset(GoldCurrencyState.Address, goldState.Currency * 100000000);

            foreach (var (key, value) in _sheets)
            {
                initialState = initialState.SetState(
                    Addresses.TableSheet.Derive(key),
                    value.Serialize()
                );
            }

            var redeemCode = new RedeemCode0(
                ByteUtil.Hex(privateKey.ByteArray),
                _avatarAddress
            );

            IAccountStateDelta nextState = redeemCode.Execute(new ActionContext()
            {
                BlockIndex = 1,
                Miner = default,
                PreviousStates = initialState,
                Rehearsal = false,
                Signer = _agentAddress,
                Random = new TestRandom(),
            });

            // Check target avatar & agent
            AvatarState nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            // See also Data/TableCSV/RedeemRewardSheet.csv
            ItemSheet itemSheet = initialState.GetItemSheet();
            HashSet<int> expectedItems = new[] { 302006, 302004, 302001, 302002 }.ToHashSet();
            Assert.Subset(nextAvatarState.inventory.Items.Select(i => i.item.Id).ToHashSet(), expectedItems);

            // Check the code redeemed properly
            RedeemCodeState nextRedeemCodeState = nextState.GetRedeemCodeState();
            Assert.Throws<DuplicateRedeemException>(() =>
            {
                nextRedeemCodeState.Redeem(redeemCode.Code, redeemCode.AvatarAddress);
            });
        }

        [Fact]
        public void Rehearsal()
        {
            var redeemCode = new RedeemCode0(
                string.Empty,
                _avatarAddress
            );

            IAccountStateDelta nextState = redeemCode.Execute(new ActionContext()
            {
                BlockIndex = 1,
                Miner = default,
                PreviousStates = new State(),
                Rehearsal = true,
                Signer = _agentAddress,
            });

            Assert.Equal(
                nextState.UpdatedAddresses,
                new[] { _avatarAddress, _agentAddress, RedeemCodeState.Address, GoldCurrencyState.Address }.ToImmutableHashSet()
            );
        }
    }
}
