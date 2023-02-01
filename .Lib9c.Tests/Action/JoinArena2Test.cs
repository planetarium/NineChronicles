namespace Lib9c.Tests.Action
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

    public class JoinArena2Test
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;
        private readonly Address _signer;
        private readonly Address _signer2;
        private readonly Address _avatarAddress;
        private readonly Address _avatar2Address;
        private readonly IRandom _random;
        private readonly Currency _currency;
        private IAccountStateDelta _state;

        public JoinArena2Test(ITestOutputHelper outputHelper)
        {
            _random = new TestRandom();
            _sheets = TableSheetsImporter.ImportSheets();
            _tableSheets = new TableSheets(_sheets);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _state = new State();

            _signer = new PrivateKey().ToAddress();
            _avatarAddress = _signer.Derive("avatar");
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var rankingMapAddress = new PrivateKey().ToAddress();
            var agentState = new AgentState(_signer);
            var avatarState = new AvatarState(
                _avatarAddress,
                _signer,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    tableSheets.WorldSheet,
                    GameConfig.RequireClearedStageLevel.ActionsInRankingBoard),
            };
            agentState.avatarAddresses[0] = _avatarAddress;
            avatarState.level = GameConfig.RequireClearedStageLevel.ActionsInRankingBoard;

            _signer2 = new PrivateKey().ToAddress();
            _avatar2Address = _signer2.Derive("avatar");
            var agent2State = new AgentState(_signer2);

            var avatar2State = new AvatarState(
                _avatar2Address,
                _signer2,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress)
            {
                worldInformation = new WorldInformation(
                    0,
                    tableSheets.WorldSheet,
                    1),
            };
            agent2State.avatarAddresses[0] = _avatar2Address;
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            var currency = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(currency);
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _currency = Currency.Legacy("CRYSTAL", 18, null);
#pragma warning restore CS0618

            _state = _state
                .SetState(_signer, agentState.Serialize())
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize())
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(_signer2, agent2State.Serialize())
                .SetState(_avatar2Address.Derive(LegacyInventoryKey), avatar2State.inventory.Serialize())
                .SetState(_avatar2Address.Derive(LegacyWorldInformationKey), avatar2State.worldInformation.Serialize())
                .SetState(_avatar2Address.Derive(LegacyQuestListKey), avatar2State.questList.Serialize())
                .SetState(_avatar2Address, avatar2State.SerializeV2())
                .SetState(Addresses.GoldCurrency, goldCurrencyState.Serialize());

            foreach ((string key, string value) in sheets)
            {
                _state = _state
                    .SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
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

        public AvatarState GetAvatarState(AvatarState avatarState, out List<Guid> equipments, out List<Guid> costumes)
        {
            avatarState.level = 999;
            (equipments, costumes) = GetDummyItems(avatarState);

            _state = _state
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize());

            return avatarState;
        }

        public AvatarState AddMedal(AvatarState avatarState, ArenaSheet.Row row, int count = 1)
        {
            var materialSheet = _state.GetSheet<MaterialItemSheet>();
            foreach (var data in row.Round)
            {
                if (!data.ArenaType.Equals(ArenaType.Season))
                {
                    continue;
                }

                var itemId = ArenaHelper.GetMedalItemId(data.ChampionshipId, data.Round);
                var material = ItemFactory.CreateMaterial(materialSheet, itemId);
                avatarState.inventory.AddItem(material, count);
            }

            _state = _state
                .SetState(_avatarAddress, avatarState.SerializeV2())
                .SetState(_avatarAddress.Derive(LegacyInventoryKey), avatarState.inventory.Serialize())
                .SetState(_avatarAddress.Derive(LegacyWorldInformationKey), avatarState.worldInformation.Serialize())
                .SetState(_avatarAddress.Derive(LegacyQuestListKey), avatarState.questList.Serialize());

            return avatarState;
        }

        [Theory]
        [InlineData(0, 1, 1, "0")]
        [InlineData(4_479_999L, 1, 2, "998001")]
        [InlineData(4_480_001L, 1, 2, "998001")]
        [InlineData(100, 1, 8, "998001")]
        public void Execute(long blockIndex, int championshipId, int round, string balance)
        {
            var arenaSheet = _state.GetSheet<ArenaSheet>();
            if (!arenaSheet.TryGetValue(championshipId, out var row))
            {
                throw new SheetRowNotFoundException(
                    nameof(ArenaSheet), $"championship Id : {championshipId}");
            }

            var avatarState = _state.GetAvatarStateV2(_avatarAddress);
            avatarState = GetAvatarState(avatarState, out var equipments, out var costumes);
            avatarState = AddMedal(avatarState, row, 80);

            var state = _state.MintAsset(_signer, FungibleAssetValue.Parse(_currency, balance));

            var action = new JoinArena2()
            {
                championshipId = championshipId,
                round = round,
                costumes = costumes,
                equipments = equipments,
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatarAddress,
            };

            state = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _signer,
                Random = _random,
                Rehearsal = false,
                BlockIndex = blockIndex,
            });

            // ArenaParticipants
            var arenaParticipantsAdr = ArenaParticipants.DeriveAddress(championshipId, round);
            var serializedArenaParticipants = (List)state.GetState(arenaParticipantsAdr);
            var arenaParticipants = new ArenaParticipants(serializedArenaParticipants);

            Assert.Equal(arenaParticipantsAdr, arenaParticipants.Address);
            Assert.Equal(_avatarAddress, arenaParticipants.AvatarAddresses.First());

            // ArenaAvatarState
            var arenaAvatarStateAdr = ArenaAvatarState.DeriveAddress(_avatarAddress);
            var serializedArenaAvatarState = (List)state.GetState(arenaAvatarStateAdr);
            var arenaAvatarState = new ArenaAvatarState(serializedArenaAvatarState);

            foreach (var guid in arenaAvatarState.Equipments)
            {
                Assert.Contains(avatarState.inventory.Equipments, x => x.ItemId.Equals(guid));
            }

            foreach (var guid in arenaAvatarState.Costumes)
            {
                Assert.Contains(avatarState.inventory.Costumes, x => x.ItemId.Equals(guid));
            }

            Assert.Equal(arenaAvatarStateAdr, arenaAvatarState.Address);

            // ArenaScore
            var arenaScoreAdr = ArenaScore.DeriveAddress(_avatarAddress, championshipId, round);
            var serializedArenaScore = (List)state.GetState(arenaScoreAdr);
            var arenaScore = new ArenaScore(serializedArenaScore);

            Assert.Equal(arenaScoreAdr, arenaScore.Address);
            Assert.Equal(GameConfig.ArenaScoreDefault, arenaScore.Score);

            // ArenaInformation
            var arenaInformationAdr = ArenaInformation.DeriveAddress(_avatarAddress, championshipId, round);
            var serializedArenaInformation = (List)state.GetState(arenaInformationAdr);
            var arenaInformation = new ArenaInformation(serializedArenaInformation);

            Assert.Equal(arenaInformationAdr, arenaInformation.Address);
            Assert.Equal(0, arenaInformation.Win);
            Assert.Equal(0, arenaInformation.Lose);
            Assert.Equal(ArenaInformation.MaxTicketCount, arenaInformation.Ticket);

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException($"{nameof(JoinArena1)} : {row.ChampionshipId} / {round}");
            }

            Assert.Equal(0 * _currency, state.GetBalance(_signer, _currency));
        }

        [Theory]
        [InlineData(9999)]
        public void Execute_SheetRowNotFoundException(int championshipId)
        {
            var avatarState = _state.GetAvatarStateV2(_avatarAddress);
            avatarState = GetAvatarState(avatarState, out var equipments, out var costumes);
            var state = _state.SetState(_avatarAddress, avatarState.SerializeV2());

            var action = new JoinArena2()
            {
                championshipId = championshipId,
                round = 1,
                costumes = costumes,
                equipments = equipments,
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<SheetRowNotFoundException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _signer,
                Random = new TestRandom(),
            }));
        }

        [Theory]
        [InlineData(123)]
        public void Execute_RoundNotFoundByIdsException(int round)
        {
            var avatarState = _state.GetAvatarStateV2(_avatarAddress);
            avatarState = GetAvatarState(avatarState, out var equipments, out var costumes);
            var state = _state.SetState(_avatarAddress, avatarState.SerializeV2());

            var action = new JoinArena2()
            {
                championshipId = 1,
                round = round,
                costumes = costumes,
                equipments = equipments,
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<RoundNotFoundException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _signer,
                Random = new TestRandom(),
                BlockIndex = 1,
            }));
        }

        [Theory]
        [InlineData(8)]
        public void Execute_NotEnoughMedalException(int round)
        {
            var avatarState = _state.GetAvatarStateV2(_avatarAddress);
            GetAvatarState(avatarState, out var equipments, out var costumes);
            var preCurrency = 99800100000 * _currency;
            var state = _state.MintAsset(_signer, preCurrency);

            var action = new JoinArena2()
            {
                championshipId = 1,
                round = round,
                costumes = costumes,
                equipments = equipments,
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<NotEnoughMedalException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _signer,
                Random = new TestRandom(),
                BlockIndex = 100,
            }));
        }

        [Theory]
        [InlineData(6, 0)] // discounted_entrance_fee
        [InlineData(8, 100)] // entrance_fee
        public void Execute_NotEnoughFungibleAssetValueException(int round, long blockIndex)
        {
            var avatarState = _state.GetAvatarStateV2(_avatarAddress);
            GetAvatarState(avatarState, out var equipments, out var costumes);
            var state = _state.SetState(_avatarAddress, avatarState.SerializeV2());

            var action = new JoinArena2()
            {
                championshipId = 1,
                round = round,
                costumes = costumes,
                equipments = equipments,
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<NotEnoughFungibleAssetValueException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _signer,
                Random = new TestRandom(),
                BlockIndex = blockIndex,
            }));
        }

        [Fact]
        public void Execute_ArenaScoreAlreadyContainsException()
        {
            var avatarState = _state.GetAvatarStateV2(_avatarAddress);
            avatarState = GetAvatarState(avatarState, out var equipments, out var costumes);
            var state = _state.SetState(_avatarAddress, avatarState.SerializeV2());

            var action = new JoinArena2()
            {
                championshipId = 1,
                round = 1,
                costumes = costumes,
                equipments = equipments,
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatarAddress,
            };

            state = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _signer,
                Random = _random,
                Rehearsal = false,
                BlockIndex = 1,
            });

            Assert.Throws<ArenaScoreAlreadyContainsException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _signer,
                Random = new TestRandom(),
                BlockIndex = 2,
            }));
        }

        [Fact]
        public void Execute_ArenaScoreAlreadyContainsException2()
        {
            const int championshipId = 1;
            const int round = 1;

            var avatarState = _state.GetAvatarStateV2(_avatarAddress);
            avatarState = GetAvatarState(avatarState, out var equipments, out var costumes);
            var state = _state.SetState(_avatarAddress, avatarState.SerializeV2());

            var arenaScoreAdr = ArenaScore.DeriveAddress(_avatarAddress, championshipId, round);
            var arenaScore = new ArenaScore(_avatarAddress, championshipId, round);
            state = state.SetState(arenaScoreAdr, arenaScore.Serialize());

            var action = new JoinArena2()
            {
                championshipId = championshipId,
                round = round,
                costumes = costumes,
                equipments = equipments,
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<ArenaScoreAlreadyContainsException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _signer,
                Random = new TestRandom(),
                BlockIndex = 1,
            }));
        }

        [Fact]
        public void Execute_ArenaInformationAlreadyContainsException()
        {
            const int championshipId = 1;
            const int round = 1;

            var avatarState = _state.GetAvatarStateV2(_avatarAddress);
            avatarState = GetAvatarState(avatarState, out var equipments, out var costumes);
            var state = _state.SetState(_avatarAddress, avatarState.SerializeV2());

            var arenaInformationAdr = ArenaInformation.DeriveAddress(_avatarAddress, championshipId, round);
            var arenaInformation = new ArenaInformation(_avatarAddress, championshipId, round);
            state = state.SetState(arenaInformationAdr, arenaInformation.Serialize());

            var action = new JoinArena2()
            {
                championshipId = championshipId,
                round = round,
                costumes = costumes,
                equipments = equipments,
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatarAddress,
            };

            Assert.Throws<ArenaInformationAlreadyContainsException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = state,
                Signer = _signer,
                Random = new TestRandom(),
                BlockIndex = 1,
            }));
        }

        [Fact]
        public void Execute_NotEnoughClearedStageLevelException()
        {
            var action = new JoinArena2()
            {
                championshipId = 1,
                round = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
                avatarAddress = _avatar2Address,
            };

            Assert.Throws<NotEnoughClearedStageLevelException>(() => action.Execute(new ActionContext()
            {
                PreviousStates = _state,
                Signer = _signer2,
                Random = new TestRandom(),
            }));
        }
    }
}
