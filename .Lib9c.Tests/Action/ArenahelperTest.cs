namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Arena;
    using Nekoyume.Model;
    using Nekoyume.Model.Arena;
    using Nekoyume.Model.EnumType;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class ArenaHelperTest
    {
        private IAccountStateDelta _state;
        private Currency _crystal;
        private Address _agent1Address;
        private Address _avatar1Address;
        private AvatarState _avatar1;

        public ArenaHelperTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _state = new State();

            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            foreach (var (key, value) in sheets)
            {
                _state = _state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            tableSheets = new TableSheets(sheets);

#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _crystal = Currency.Legacy("CRYSTAL", 18, null);
            var ncg = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(ncg);

            var rankingMapAddress = new PrivateKey().ToAddress();
            var clearStageId = Math.Max(
                tableSheets.StageSheet.First?.Id ?? 1,
                GameConfig.RequireClearedStageLevel.ActionsInRankingBoard);

            // account 1
            var (agent1State, avatar1State) = GetAgentStateWithAvatarState(
                sheets,
                tableSheets,
                rankingMapAddress,
                clearStageId);

            _agent1Address = agent1State.address;
            _avatar1 = avatar1State;
            _avatar1Address = avatar1State.address;

            _state = _state
                .SetState(Addresses.GoldCurrency, goldCurrencyState.Serialize())
                .SetState(_agent1Address, agent1State.Serialize())
                .SetState(_avatar1Address.Derive(LegacyInventoryKey), _avatar1.inventory.Serialize())
                .SetState(_avatar1Address.Derive(LegacyWorldInformationKey), _avatar1.worldInformation.Serialize())
                .SetState(_avatar1Address.Derive(LegacyQuestListKey), _avatar1.questList.Serialize())
                .SetState(_avatar1Address, _avatar1.Serialize())
                .SetState(Addresses.GameConfig, new GameConfigState(sheets[nameof(GameConfigSheet)]).Serialize());

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();
        }

        public static (AgentState AgentState, AvatarState AvatarState) GetAgentStateWithAvatarState(
            IReadOnlyDictionary<string, string> sheets,
            TableSheets tableSheets,
            Address rankingMapAddress,
            int clearStageId)
        {
            var agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(agentAddress);

            var avatarAddress = agentAddress.Derive("avatar");
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(sheets[nameof(GameConfigSheet)]),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    tableSheets.WorldSheet,
                    clearStageId),
            };
            agentState.avatarAddresses.Add(0, avatarAddress);

            return (agentState, avatarState);
        }

        [Fact]
        public void ExecuteGetTicketPrice()
        {
            var arenaSheet = _state.GetSheet<ArenaSheet>();
            foreach (var row in arenaSheet)
            {
                foreach (var roundData in row.Round)
                {
                    var arenaInformationAdr =
                        ArenaInformation.DeriveAddress(_avatar1Address, roundData.ChampionshipId, roundData.Round);
                    if (_state.TryGetState(arenaInformationAdr, out List _))
                    {
                        throw new ArenaInformationAlreadyContainsException(
                            $"[{nameof(JoinArena)}] id({roundData.ChampionshipId}) / round({roundData.Round})");
                    }

                    var arenaInformation = new ArenaInformation(_avatar1Address, roundData.ChampionshipId, roundData.Round);
                    var max = roundData.MaxPurchaseCount;
                    for (var i = 0; i < max; i++)
                    {
                        arenaInformation.BuyTicket(roundData.MaxPurchaseCount);

                        var ticketPrice = 0;
                        var additionalTicketPrice = 0;
                        switch (roundData.ArenaType)
                        {
                            case ArenaType.OffSeason:
                                ticketPrice = 5;
                                additionalTicketPrice = 2;
                                break;
                            case ArenaType.Season:
                                ticketPrice = 50;
                                additionalTicketPrice = 20;
                                break;
                            case ArenaType.Championship:
                                ticketPrice = 100;
                                additionalTicketPrice = 40;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        if (roundData.ChampionshipId == 2 && roundData.Round == 8)
                        {
                            ticketPrice = 100;
                            additionalTicketPrice = 40;
                        }

                        var sum = ticketPrice + (additionalTicketPrice * arenaInformation.PurchasedTicketCount);
                        var major = sum / 100;
                        var miner = sum % 100;
                        var expectedPrice = new FungibleAssetValue(_state.GetGoldCurrency(), major, miner);
                        var price = ArenaHelper.GetTicketPrice(roundData, arenaInformation, _state.GetGoldCurrency());

                        Assert.Equal(expectedPrice, price);
                    }
                }
            }
        }
    }
}
