namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Crypto;
    using Nekoyume.Action;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;
    using static Nekoyume.Model.State.RedeemCodeState;

    public class RedeemCodeTest
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

        private readonly TableSheetsState _tableSheetsState;

        private readonly TableSheets _tableSheets;

        public RedeemCodeTest()
        {
            _tableSheetsState = TableSheetsImporter.ImportTableSheets();
            _tableSheets = TableSheets.FromTableSheetsState(_tableSheetsState);
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
                _tableSheets,
                gameConfigState
            );

            var goldState = new GoldCurrencyState(new Currency("NCG", minter: null));

            var initialState = new State()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(TableSheetsState.Address, _tableSheetsState.Serialize())
                .SetState(RedeemCodeState.Address, prevRedeemCodesState.Serialize())
                .SetState(GoldCurrencyState.Address, goldState.Serialize())
                .MintAsset(GoldCurrencyState.Address, goldState.Currency, 100000000);
            var redeemCode = new RedeemCode(
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
            });

            // Check target avatar & agent
            AvatarState nextAvatarState = nextState.GetAvatarState(_avatarAddress);
            // See also Data/TableCSV/RedeemRewardSheet.csv
            ItemSheet itemSheet = _tableSheets.ItemSheet;
            HashSet<int> expectedItems = new[] { 100000, 40100000 }.ToHashSet();
            Assert.Subset(nextAvatarState.inventory.Items.Select(i => i.item.Id).ToHashSet(), expectedItems);
            Assert.Equal(100, nextState.GetBalance(_agentAddress, goldState.Currency));

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
            var redeemCode = new RedeemCode(
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
