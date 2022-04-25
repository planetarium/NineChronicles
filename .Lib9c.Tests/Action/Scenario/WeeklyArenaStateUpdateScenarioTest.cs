namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Model;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Xunit;

    public class WeeklyArenaStateUpdateScenarioTest
    {
        private readonly Address _agent1Address;
        private readonly Address _avatar1Address;
        private readonly Address _avatar2Address;
        private readonly Address _avatar3Address;
        private readonly Address _weeklyArenaAddress;
        private readonly IAccountStateDelta _initialState;

        public WeeklyArenaStateUpdateScenarioTest()
        {
            _initialState = new Tests.Action.State();

            var sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in sheets)
            {
                _initialState = _initialState.SetState(
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            var tableSheets = new TableSheets(sheets);

            var rankingMapAddress = new PrivateKey().ToAddress();

            _agent1Address = new PrivateKey().ToAddress();
            _avatar1Address = new PrivateKey().ToAddress();

            var agentState = new AgentState(_agent1Address);
            var avatarState = new AvatarState(
                _avatar1Address,
                _agent1Address,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(sheets[nameof(GameConfigSheet)]),
                rankingMapAddress
            )
            {
                worldInformation = new WorldInformation(
                    0,
                    tableSheets.WorldSheet,
                    Math.Max(
                        tableSheets.StageSheet.First?.Id ?? 1,
                        GameConfig.RequireClearedStageLevel.ActionsInRankingBoard)),
                level = 100,
            };
            agentState.avatarAddresses.Add(0, _avatar1Address);

            var agent2Address = new PrivateKey().ToAddress();
            _avatar2Address = new PrivateKey().ToAddress();

            var agent2State = new AgentState(agent2Address);
            var avatar2State = new AvatarState(
                _avatar2Address,
                agent2Address,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(sheets[nameof(GameConfigSheet)]),
                rankingMapAddress
            )
            {
                worldInformation = new WorldInformation(
                    0,
                    tableSheets.WorldSheet,
                    Math.Max(
                        tableSheets.StageSheet.First?.Id ?? 1,
                        GameConfig.RequireClearedStageLevel.ActionsInRankingBoard)),
                level = 100,
            };
            agent2State.avatarAddresses.Add(0, _avatar2Address);

            var agent3Address = new PrivateKey().ToAddress();
            _avatar3Address = new PrivateKey().ToAddress();

            var agent3State = new AgentState(agent3Address);
            var avatar3State = new AvatarState(
                _avatar3Address,
                agent2Address,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(sheets[nameof(GameConfigSheet)]),
                rankingMapAddress
            )
            {
                worldInformation = new WorldInformation(
                    0,
                    tableSheets.WorldSheet,
                    Math.Max(
                        tableSheets.StageSheet.First?.Id ?? 1,
                        GameConfig.RequireClearedStageLevel.ActionsInRankingBoard)),
                level = 100,
            };
            agent3State.avatarAddresses.Add(0, _avatar3Address);

            var prevWeeklyArenaState = new WeeklyArenaState(RankingBattle11.UpdateTargetWeeklyArenaIndex - 2);
            var weeklyArenaState = new WeeklyArenaState(RankingBattle11.UpdateTargetWeeklyArenaIndex - 1);
            weeklyArenaState.SetV2(avatarState, tableSheets.CharacterSheet, tableSheets.CostumeStatSheet);
            weeklyArenaState[_avatar1Address].Activate();
            weeklyArenaState.SetV2(avatar2State, tableSheets.CharacterSheet, tableSheets.CostumeStatSheet);
            weeklyArenaState[_avatar2Address].Activate();
            _weeklyArenaAddress = WeeklyArenaState.DeriveAddress(RankingBattle11.UpdateTargetWeeklyArenaIndex);

            var gold = new GoldCurrencyState(new Currency("NCG", 2, minter: null));

            _initialState = _initialState
                .SetState(_agent1Address, agentState.Serialize())
                .SetState(_avatar1Address, avatarState.Serialize())
                .SetState(agent2Address, agent2State.Serialize())
                .SetState(_avatar2Address, avatar2State.Serialize())
                .SetState(agent3Address, agent2State.Serialize())
                .SetState(_avatar3Address, avatar2State.Serialize())
                .SetState(Addresses.GameConfig, new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize())
                .SetState(prevWeeklyArenaState.address, prevWeeklyArenaState.Serialize())
                .SetState(weeklyArenaState.address, weeklyArenaState.Serialize())
                .SetState(_weeklyArenaAddress, new WeeklyArenaState(RankingBattle11.UpdateTargetWeeklyArenaIndex).Serialize())
                .SetState(GoldCurrencyState.Address, gold.Serialize())
                .SetState(Addresses.GoldDistribution, GoldDistributionTest.Fixture.Select(v => v.Serialize()).Serialize())
                .MintAsset(GoldCurrencyState.Address, gold.Currency * 100000000000);
        }

        [Fact]
        public void TargetBlock()
        {
            var rb = new RankingBattle11
            {
                avatarAddress = _avatar1Address,
                enemyAddress = _avatar3Address,
                weeklyArenaAddress = _weeklyArenaAddress,
                costumeIds = new List<Guid>(),
                equipmentIds = new List<Guid>(),
            };

            var arenaInfoAddress = _weeklyArenaAddress.Derive(_avatar1Address.ToByteArray());
            var arenaInfo2Address = _weeklyArenaAddress.Derive(_avatar2Address.ToByteArray());
            var arenaInfo3Address = _weeklyArenaAddress.Derive(_avatar3Address.ToByteArray());
            var listAddress = _weeklyArenaAddress.Derive("address_list");

            Assert.False(_initialState.TryGetState(arenaInfoAddress, out Dictionary _));
            Assert.False(_initialState.TryGetState(arenaInfo2Address, out Dictionary _));
            Assert.False(_initialState.TryGetState(arenaInfo3Address, out Dictionary _));
            Assert.False(_initialState.TryGetState(listAddress, out List _));

            var testRandom = new TestRandom();
            var blockIndex = RankingBattle11.UpdateTargetBlockIndex;
            var nextState = rb.Execute(new ActionContext
            {
                PreviousStates = _initialState,
                Signer = _agent1Address,
                Random = testRandom,
                Rehearsal = false,
                BlockIndex = blockIndex,
            });

            Assert.True(nextState.TryGetState(arenaInfoAddress, out Dictionary rawInfo));
            Assert.True(nextState.TryGetState(arenaInfo3Address, out Dictionary _));
            Assert.True(nextState.TryGetState(listAddress, out List rawList));

            var info = new ArenaInfo(rawInfo);
            var addressList = rawList.ToList(StateExtensions.ToAddress);

            Assert.Equal(4, info.DailyChallengeCount);
            Assert.Contains(_avatar1Address, addressList);
            Assert.DoesNotContain(_avatar2Address, addressList);
            Assert.Contains(_avatar3Address, addressList);

            var rg = new RewardGold();

            var updatedState = rg.Execute(new ActionContext
            {
                PreviousStates = nextState,
                Signer = _agent1Address,
                Random = testRandom,
                Rehearsal = false,
                BlockIndex = blockIndex,
            });

            Assert.True(updatedState.TryGetState(arenaInfoAddress, out Dictionary updatedRawInfo));
            Assert.True(updatedState.TryGetState(arenaInfo2Address, out Dictionary _));
            Assert.True(updatedState.TryGetState(arenaInfo3Address, out Dictionary _));
            Assert.True(updatedState.TryGetState(listAddress, out List updatedRawList));

            var updatedInfo = new ArenaInfo(updatedRawInfo);
            var updatedAddressList = updatedRawList.ToList(StateExtensions.ToAddress);

            Assert.Equal(5, updatedInfo.DailyChallengeCount);
            Assert.Contains(_avatar1Address, updatedAddressList);
            Assert.Contains(_avatar2Address, updatedAddressList);
            Assert.Contains(_avatar3Address, updatedAddressList);
        }
    }
}
