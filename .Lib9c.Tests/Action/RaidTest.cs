namespace Lib9c.Tests.Action
{
    using System.Collections.Generic;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Helper;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class RaidTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private IAccountStateDelta _state;
        private TableSheets _tableSheets;

        public RaidTest()
        {
            _tableSheets = new TableSheets(TableSheetsImporter.ImportSheets());
            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_agentAddress);
            var avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                default
            );
            _state = new State()
                .SetState(_agentAddress, agentState.Serialize())
                .SetState(_avatarAddress, avatarState.Serialize())
                .SetState(Addresses.GetSheetAddress<WorldBossListSheet>(), _tableSheets.WorldBossListSheet.Serialize())
                .MintAsset(_agentAddress, 300 * CrystalCalculator.CRYSTAL);
        }

        [Fact]
        public void Execute()
        {
            var action = new Raid
            {
                AvatarAddress = _avatarAddress,
                RaidId = 1,
            };

            var avatarState = _state.GetAvatarState(_avatarAddress);
            for (int i = 0; i < 50; i++)
            {
                avatarState.worldInformation.ClearStage(1, i + 1, 0, _tableSheets.WorldSheet, _tableSheets.WorldUnlockSheet);
            }

            var state = _state.SetState(_avatarAddress, avatarState.Serialize());

            var nextState = action.Execute(new ActionContext
            {
                BlockIndex = 1,
                PreviousStates = state,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = _agentAddress,
            });

            Currency crystal = CrystalCalculator.CRYSTAL;
            Address bossAddress = Addresses.GetWorldBossAddress(1);
            Assert.Equal(0 * crystal, nextState.GetBalance(_agentAddress, crystal));
            Assert.Equal(300 * crystal, nextState.GetBalance(bossAddress, crystal));

            Assert.True(nextState.TryGetState(Addresses.GetRaiderAddress(_avatarAddress, 1), out List rawRaider));
            var raiderState = new RaiderState(rawRaider);
            Assert.Equal(10_000, raiderState.HighScore);
            Assert.Equal(10_000, raiderState.TotalScore);
            Assert.Equal(2, raiderState.RemainChallengeCount);
            Assert.Equal(1, raiderState.TotalChallengeCount);

            Assert.True(nextState.TryGetState(Addresses.GetRaidersAddress(1), out List rawRaiders));
            List<Address> raiders = rawRaiders.ToList(StateExtensions.ToAddress);
            Assert.Single(raiders);
            Assert.Contains(_avatarAddress, raiders);

            Assert.True(nextState.TryGetState(bossAddress, out List rawBoss));
            var bossState = new WorldBossState(rawBoss);
            Assert.Equal(10_000, bossState.CurrentHP);
            Assert.Equal(1, bossState.Level);
        }
    }
}
