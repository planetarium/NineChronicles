namespace Lib9c.Tests.Action.Scenario
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
    using Nekoyume.Model.Item;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class ArenaScenarioTest
    {
        private readonly Address _rankingMapAddress;
        private readonly Currency _crystal;
        private readonly Currency _ncg;
        private TableSheets _tableSheets;
        private Dictionary<string, string> _sheets;
        private IAccountStateDelta _state;

        public ArenaScenarioTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _state = new Tests.Action.State();

            _sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(_sheets);
            foreach (var (key, value) in _sheets)
            {
                _state = _state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            _tableSheets = new TableSheets(_sheets);
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _crystal = Currency.Legacy("CRYSTAL", 18, null);
            _ncg = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(_ncg);
            _rankingMapAddress = new PrivateKey().ToAddress();
            var clearStageId = Math.Max(
                _tableSheets.StageSheet.First?.Id ?? 1,
                GameConfig.RequireClearedStageLevel.ActionsInRankingBoard);

            _state = _state
                .SetState(Addresses.GoldCurrency, goldCurrencyState.Serialize())
                .SetState(Addresses.GameConfig, new GameConfigState(_sheets[nameof(GameConfigSheet)]).Serialize());
        }

        public (List<Guid> Equipments, List<Guid> Costumes) GetDummyItems(AvatarState avatarState)
        {
            var items = Doomfist.GetAllParts(_tableSheets, avatarState.level);
            foreach (var equipment in items)
            {
                avatarState.inventory.AddItem(equipment);
            }

            var equipments = items.Select(e => e.NonFungibleId).ToList();

            var random = new TestRandom();
            var costumes = new List<Guid>();
            if (avatarState.level >= GameConfig.RequireCharacterLevel.CharacterFullCostumeSlot)
            {
                var costumeId = _tableSheets
                    .CostumeItemSheet
                    .Values
                    .First(r => r.ItemSubType == ItemSubType.FullCostume)
                    .Id;

                var costume = (Costume)ItemFactory.CreateItem(
                    _tableSheets.ItemSheet[costumeId], random);
                avatarState.inventory.AddItem(costume);
                costumes.Add(costume.ItemId);
            }

            return (equipments, costumes);
        }

        public IAccountStateDelta JoinArena(
            IRandom random,
            Address signer,
            Address avatarAddress,
            ArenaSheet.RoundData roundData)
        {
            var preCurrency = roundData.EntranceFee * _crystal;
            _state = _state.MintAsset(signer, preCurrency);

            var action = new JoinArena()
            {
                championshipId = roundData.ChampionshipId,
                round = roundData.Round,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                avatarAddress = avatarAddress,
            };

            _state = action.Execute(new ActionContext
            {
                PreviousStates = _state,
                Signer = signer,
                Random = random,
                Rehearsal = false,
                BlockIndex = roundData.StartBlockIndex,
            });
            return _state;
        }

        public IAccountStateDelta BattleArena(
            IRandom random,
            Address signer,
            Address myAvatarAddress,
            Address enemyAvatarAddress,
            ArenaSheet.RoundData roundData,
            int ticket,
            long blockIndex)
        {
            var action = new BattleArena6()
            {
                myAvatarAddress = myAvatarAddress,
                enemyAvatarAddress = enemyAvatarAddress,
                championshipId = roundData.ChampionshipId,
                round = roundData.Round,
                ticket = ticket,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
            };

            _state = action.Execute(new ActionContext
            {
                PreviousStates = _state,
                Signer = signer,
                Random = random,
                Rehearsal = false,
                BlockIndex = blockIndex,
            });
            return _state;
        }

        public (AgentState Agent, AvatarState Avatar) CreateAccount()
        {
            var clearStageId = Math.Max(
                _tableSheets.StageSheet.First?.Id ?? 1,
                GameConfig.RequireClearedStageLevel.ActionsInRankingBoard);

            var agentAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(agentAddress);

            var avatarAddress = agentAddress.Derive("avatar");
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                _tableSheets.GetAvatarSheets(),
                new GameConfigState(_sheets[nameof(GameConfigSheet)]),
                _rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    _tableSheets.WorldSheet,
                    clearStageId),
            };
            agentState.avatarAddresses.Add(0, avatarAddress);
            _state = _state
                .SetState(agentState.address, agentState.Serialize())
                .SetState(avatarState.address.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(avatarState.address.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                .SetState(avatarState.address.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                .SetState(avatarState.address, avatarState.Serialize());

            return (agentState, avatarState);
        }

        // It's a scenario that includes the purchase of tickets, so I'm deactivating it now
        // [Theory]
        // [InlineData(1, 10, 5, 3, 2)]
        // [InlineData(1, 10, 5, 3, 3)]
        // [InlineData(2, 10, 5, 10, 2)]
        // public void Execute(int seed, int user, int ticket, int repeatCount,  int arenaInterval)
        // {
        //     var arenaSheet = _state.GetSheet<ArenaSheet>();
        //
        //     var gameConfigState = SetArenaInterval(arenaInterval);
        //     _state = _state.SetState(GameConfigState.Address, gameConfigState.Serialize());
        //
        //     foreach (var value in arenaSheet.Values)
        //     {
        //         if (!arenaSheet.TryGetValue(value.Key, out var row))
        //         {
        //             throw new SheetRowNotFoundException(nameof(ArenaSheet), value.Key);
        //         }
        //
        //         var rand = new TestRandom(seed);
        //         var championshipData = row.Round.OrderByDescending(x => x.Round).First();
        //         // Log.Debug($"[RequiredMedalCount] {championshipData.RequiredMedalCount}");
        //         var seasonParticipants = new List<Address>();
        //         foreach (var data in row.Round)
        //         {
        //             var apAdr = ArenaParticipants.DeriveAddress(data.ChampionshipId, data.Round);
        //
        //             if (data.ArenaType.Equals(ArenaType.Championship))
        //             {
        //                 foreach (var innerData in row.Round)
        //                 {
        //                     if (innerData.ArenaType != ArenaType.Season)
        //                     {
        //                        continue;
        //                     }
        //
        //                     var innerApAdr =
        //                         ArenaParticipants.DeriveAddress(innerData.ChampionshipId, innerData.Round);
        //                     if (_state.TryGetArenaParticipants(innerApAdr, out var innerAp))
        //                     {
        //                         seasonParticipants.AddRange(innerAp.AvatarAddresses);
        //                     }
        //                 }
        //
        //                 seasonParticipants = seasonParticipants.Distinct().ToList();
        //                 foreach (var address in seasonParticipants)
        //                 {
        //                     var avatarState = _state.GetAvatarStateV2(address);
        //                     var medalCount = ArenaHelper.GetMedalTotalCount(row, avatarState);
        //                     if (medalCount >= data.RequiredMedalCount)
        //                     {
        //                         _state = JoinArena(rand, avatarState.agentAddress, avatarState.address, data);
        //                     }
        //                 }
        //             }
        //             else
        //             {
        //                 for (var i = 0; i < user; i++)
        //                 {
        //                     var (agentState, avatarState) = CreateAccount();
        //                     _state = JoinArena(rand, agentState.address, avatarState.address, data);
        //                 }
        //             }
        //
        //             if (_state.TryGetArenaParticipants(apAdr, out var afterAp))
        //             {
        //                 foreach (var adr in afterAp.AvatarAddresses)
        //                 {
        //                     var aiAdr = ArenaInformation.DeriveAddress(adr, data.ChampionshipId, data.Round);
        //                     var avatarState = _state.GetAvatarStateV2(adr);
        //                     var playCount = Math.Max(championshipData.RequiredMedalCount + 5, repeatCount);
        //
        //                     for (var i = 0; i < playCount; i++)
        //                     {
        //                         if (!_state.TryGetArenaInformation(aiAdr, out var ai))
        //                         {
        //                             throw new ArenaInformationNotFoundException($"ai : {aiAdr}");
        //                         }
        //
        //                         var buyTicket = Math.Max(0, ticket - ai.Ticket);
        //                         var currency = buyTicket * data.TicketPrice * _ncg;
        //                         _state = _state.MintAsset(avatarState.agentAddress, currency);
        //
        //                         var myScore = GetScore(adr, data);
        //
        //                         var targets = afterAp.AvatarAddresses
        //                             .Where(x => x != adr)
        //                             .OrderBy(x => Guid.NewGuid()).ToList();
        //
        //                         if (TryGetTarget(targets, data, myScore, out var targetAddress))
        //                         {
        //                             var addBlockIndex = Math.Min(data.StartBlockIndex + i, data.EndBlockIndex);
        //                             _state = BattleArena(
        //                                 rand,
        //                                 avatarState.agentAddress,
        //                                 adr,
        //                                 targetAddress,
        //                                 data,
        //                                 ticket,
        //                                 addBlockIndex);
        //                         }
        //                     }
        //                 }
        //             }
        //             else
        //             {
        //                 Log.Debug(
        //                     "Arena Participants is nobody : {ChampionshipId} / {Round}",
        //                     data.ChampionshipId,
        //                     data.Round);
        //             }
        //
        //             if (data.ArenaType == ArenaType.Championship && afterAp != null)
        //             {
        //                 foreach (var adr in afterAp.AvatarAddresses)
        //                 {
        //                     var aiAdr = ArenaInformation.DeriveAddress(adr, data.ChampionshipId, data.Round);
        //                     if (!_state.TryGetArenaInformation(aiAdr, out var ai))
        //                     {
        //                         throw new ArenaInformationNotFoundException($"ai : {aiAdr}");
        //                     }
        //
        //                     var sAdr = ArenaScore.DeriveAddress(adr, data.ChampionshipId, data.Round);
        //                     if (!_state.TryGetArenaScore(sAdr, out var score))
        //                     {
        //                         throw new ArenaScoreNotFoundException($"score : {score}");
        //                     }
        //
        //                     var avatarState = _state.GetAvatarStateV2(adr);
        //                     var medalCount = ArenaHelper.GetMedalTotalCount(row, avatarState);
        //                     // Log.Debug(ShowLog(adr, data, score, ai, medalCount));
        //                 }
        //             }
        //         }
        //
        //         var expectedUser = 0;
        //         foreach (var seasonParticipant in seasonParticipants)
        //         {
        //             var avatarState = _state.GetAvatarStateV2(seasonParticipant);
        //             var medalCount = ArenaHelper.GetMedalTotalCount(row, avatarState);
        //             if (medalCount >= championshipData.RequiredMedalCount)
        //             {
        //                 expectedUser++;
        //             }
        //         }
        //
        //         var chAdr =
        //             ArenaParticipants.DeriveAddress(championshipData.ChampionshipId, championshipData.Round);
        //         _state.TryGetArenaParticipants(chAdr, out var chAp);
        //         Assert.Equal(expectedUser, chAp.AvatarAddresses.Count);
        //     }
        // }
        private static string ShowLog(Address adr, ArenaSheet.RoundData data, ArenaScore score, ArenaInformation ai, int medalCount)
        {
            return $"[#{adr.ToHex().Substring(0, 6)}] arenaType({data.ArenaType}) / " +
                   $"score({score.Score}) / " +
                   $"Win({ai.Win}) / Lose({ai.Lose}) / " +
                   $"Ticket({ai.Ticket}) / TicketResetCount({ai.TicketResetCount}) / " +
                   $"MedalCount({medalCount})";
        }

        private bool TryGetTarget(
            List<Address> targets,
            ArenaSheet.RoundData data,
            int myScore,
            out Address targetAddress)
        {
            foreach (var target in targets)
            {
                var targetScore = GetScore(target, data);
                if (ArenaHelper.ValidateScoreDifference(ArenaHelper.ScoreLimits, data.ArenaType, myScore, targetScore))
                {
                    targetAddress = target;
                    return true;
                }
            }

            targetAddress = new PrivateKey().ToAddress();
            return false;
        }

        private int GetScore(Address avatarAddress, ArenaSheet.RoundData data)
        {
            var sAdr = ArenaScore.DeriveAddress(avatarAddress, data.ChampionshipId, data.Round);
            if (!_state.TryGetArenaScore(sAdr, out var score))
            {
                throw new ArenaScoreNotFoundException($"score : {score}");
            }

            return score.Score;
        }

        private GameConfigState SetArenaInterval(int interval)
        {
            var gameConfigState = _state.GetGameConfigState();
            var sheet = _tableSheets.GameConfigSheet;
            foreach (var value in sheet.Values)
            {
                if (value.Key.Equals("daily_arena_interval"))
                {
                    IReadOnlyList<string> field = new[]
                    {
                        value.Key,
                        interval.ToString(),
                    };
                    value.Set(field);
                }
            }

            gameConfigState.Set(sheet);
            return gameConfigState;
        }
    }
}
