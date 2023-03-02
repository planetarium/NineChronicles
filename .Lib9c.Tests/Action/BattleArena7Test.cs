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
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static Lib9c.SerializeKeys;

    public class BattleArena7Test
    {
        private readonly Dictionary<string, string> _sheets;
        private readonly TableSheets _tableSheets;

        private readonly Address _agent1Address;
        private readonly Address _agent2Address;
        private readonly Address _agent3Address;
        private readonly Address _agent4Address;
        private readonly Address _avatar1Address;
        private readonly Address _avatar2Address;
        private readonly Address _avatar3Address;
        private readonly Address _avatar4Address;
        private readonly Currency _crystal;
        private readonly Currency _ncg;
        private IAccountStateDelta _initialStates;

        public BattleArena7Test(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _initialStates = new State();

            _sheets = TableSheetsImporter.ImportSheets();
            foreach (var (key, value) in _sheets)
            {
                _initialStates = _initialStates.SetState(
                    Addresses.TableSheet.Derive(key),
                    value.Serialize());
            }

            _tableSheets = new TableSheets(_sheets);
#pragma warning disable CS0618
            // Use of obsolete method Currency.Legacy(): https://github.com/planetarium/lib9c/discussions/1319
            _crystal = Currency.Legacy("CRYSTAL", 18, null);
            _ncg = Currency.Legacy("NCG", 2, null);
#pragma warning restore CS0618
            var goldCurrencyState = new GoldCurrencyState(_ncg);

            var rankingMapAddress = new PrivateKey().ToAddress();
            var clearStageId = Math.Max(
                _tableSheets.StageSheet.First?.Id ?? 1,
                GameConfig.RequireClearedStageLevel.ActionsInRankingBoard);

            // account 1
            var (agent1State, avatar1State) = GetAgentStateWithAvatarState(
                _sheets,
                _tableSheets,
                rankingMapAddress,
                clearStageId);

            _agent1Address = agent1State.address;
            _avatar1Address = avatar1State.address;

            // account 2
            var (agent2State, avatar2State) = GetAgentStateWithAvatarState(
                _sheets,
                _tableSheets,
                rankingMapAddress,
                clearStageId);
            _agent2Address = agent2State.address;
            _avatar2Address = avatar2State.address;

            // account 3
            var (agent3State, avatar3State) = GetAgentStateWithAvatarState(
                _sheets,
                _tableSheets,
                rankingMapAddress,
                1);
            _agent3Address = agent3State.address;
            _avatar3Address = avatar3State.address;

            // account 4
            var (agent4State, avatar4State) = GetAgentStateWithAvatarState(
                _sheets,
                _tableSheets,
                rankingMapAddress,
                1);

            _agent4Address = agent4State.address;
            _avatar4Address = avatar4State.address;

            _initialStates = _initialStates
                .SetState(Addresses.GoldCurrency, goldCurrencyState.Serialize())
                .SetState(_agent1Address, agent1State.Serialize())
                .SetState(
                    _avatar1Address.Derive(LegacyInventoryKey),
                    avatar1State.inventory.Serialize())
                .SetState(
                    _avatar1Address.Derive(LegacyWorldInformationKey),
                    avatar1State.worldInformation.Serialize())
                .SetState(
                    _avatar1Address.Derive(LegacyQuestListKey),
                    avatar1State.questList.Serialize())
                .SetState(_avatar1Address, avatar1State.SerializeV2())
                .SetState(_agent2Address, agent2State.Serialize())
                .SetState(_avatar2Address, avatar2State.Serialize())
                .SetState(_agent3Address, agent3State.Serialize())
                .SetState(_avatar3Address, avatar3State.Serialize())
                .SetState(_agent4Address, agent4State.Serialize())
                .SetState(
                    _avatar4Address.Derive(LegacyInventoryKey),
                    avatar4State.inventory.Serialize())
                .SetState(
                    _avatar4Address.Derive(LegacyWorldInformationKey),
                    avatar4State.worldInformation.Serialize())
                .SetState(
                    _avatar4Address.Derive(LegacyQuestListKey),
                    avatar4State.questList.Serialize())
                .SetState(_avatar4Address, avatar4State.SerializeV2())
                .SetState(
                    Addresses.GameConfig,
                    new GameConfigState(_sheets[nameof(GameConfigSheet)]).Serialize());

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();
        }

        [Theory]
        [InlineData(4, 1, 1, false, 1, 5, 3)]
        [InlineData(4, 1, 1, false, 1, 5, 4)]
        [InlineData(4, 1, 1, false, 5, 5, 3)]
        [InlineData(4, 1, 1, true, 1, 5, 3)]
        [InlineData(4, 1, 1, true, 3, 5, 3)]
        [InlineData(1, 1, 2, false, 1, 5, 3)]
        [InlineData(1, 1, 2, true, 1, 5, 3)]
        public void Execute_Success(
            long nextBlockIndex,
            int championshipId,
            int round,
            bool isPurchased,
            int ticket,
            int arenaInterval,
            int randomSeed)
        {
            Execute(
                nextBlockIndex,
                championshipId,
                round,
                isPurchased,
                ticket,
                arenaInterval,
                randomSeed,
                _agent1Address,
                _avatar1Address,
                _agent2Address,
                _avatar2Address);
        }

        [Fact]
        public void Execute_Backward_Compatibility_Success()
        {
            Execute(
                1,
                1,
                2,
                default,
                1,
                2,
                default,
                _agent2Address,
                _avatar2Address,
                _agent1Address,
                _avatar1Address);
        }

        [Fact]
        public void Execute_InvalidAddressException()
        {
            var action = new BattleArena7
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar1Address,
                championshipId = 1,
                round = 1,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            Assert.Throws<InvalidAddressException>(() => action.Execute(new ActionContext
            {
                PreviousStates = _initialStates,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_FailedLoadStateException()
        {
            var action = new BattleArena7
            {
                myAvatarAddress = _avatar2Address,
                enemyAvatarAddress = _avatar1Address,
                championshipId = 1,
                round = 1,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            Assert.Throws<FailedLoadStateException>(() => action.Execute(new ActionContext
            {
                PreviousStates = _initialStates,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_NotEnoughClearedStageLevelException()
        {
            var action = new BattleArena7
            {
                myAvatarAddress = _avatar4Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = 1,
                round = 1,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            Assert.Throws<NotEnoughClearedStageLevelException>(() =>
                action.Execute(new ActionContext
                {
                    PreviousStates = _initialStates,
                    Signer = _agent4Address,
                    Random = new TestRandom(),
                    BlockIndex = 1,
                }));
        }

        [Fact]
        public void Execute_SheetRowNotFoundException()
        {
            var action = new BattleArena7
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = 9999999,
                round = 1,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            Assert.Throws<SheetRowNotFoundException>(() => action.Execute(new ActionContext
            {
                PreviousStates = _initialStates,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_ThisArenaIsClosedException()
        {
            var action = new BattleArena7
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = 1,
                round = 1,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            Assert.Throws<ThisArenaIsClosedException>(() => action.Execute(new ActionContext
            {
                PreviousStates = _initialStates,
                Signer = _agent1Address,
                Random = new TestRandom(),
                BlockIndex = 4480001,
            }));
        }

        [Fact]
        public void Execute_ArenaParticipantsNotFoundException()
        {
            var action = new BattleArena7
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = 1,
                round = 1,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            Assert.Throws<ArenaParticipantsNotFoundException>(() => action.Execute(new ActionContext
            {
                PreviousStates = _initialStates,
                Signer = _agent1Address,
                Random = new TestRandom(),
                BlockIndex = 1,
            }));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_AddressNotFoundInArenaParticipantsException(bool excludeMe)
        {
            const int championshipId = 1;
            const int round = 1;
            var previousStates = _initialStates;
            Assert.True(previousStates.GetSheet<ArenaSheet>().TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena7)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousStates = excludeMe
                ? JoinArena(
                    previousStates,
                    _agent2Address,
                    _avatar2Address,
                    roundData.StartBlockIndex,
                    championshipId,
                    round,
                    random)
                : JoinArena(
                    previousStates,
                    _agent1Address,
                    _avatar1Address,
                    roundData.StartBlockIndex,
                    championshipId,
                    round,
                    random);

            var action = new BattleArena7
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            Assert.Throws<AddressNotFoundInArenaParticipantsException>(() =>
                action.Execute(new ActionContext
                {
                    PreviousStates = previousStates,
                    Signer = _agent1Address,
                    Random = new TestRandom(),
                    BlockIndex = 1,
                }));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Execute_ValidateScoreDifferenceException(bool isSigner)
        {
            const int championshipId = 1;
            const int round = 2;
            var previousStates = _initialStates;
            Assert.True(previousStates.GetSheet<ArenaSheet>().TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena7)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousStates = JoinArena(
                previousStates,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousStates = JoinArena(
                previousStates,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaScoreAdr = ArenaScore.DeriveAddress(
                isSigner
                    ? _avatar1Address
                    : _avatar2Address, roundData.ChampionshipId,
                roundData.Round);
            previousStates.TryGetArenaScore(arenaScoreAdr, out var arenaScore);
            arenaScore.AddScore(900);
            previousStates = previousStates.SetState(arenaScoreAdr, arenaScore.Serialize());

            var action = new BattleArena7
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            var blockIndex = roundData.StartBlockIndex + 1;
            Assert.Throws<ValidateScoreDifferenceException>(() => action.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = previousStates,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_InsufficientBalanceException()
        {
            const int championshipId = 1;
            const int round = 2;
            var previousStates = _initialStates;
            Assert.True(previousStates.GetSheet<ArenaSheet>().TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena7)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousStates = JoinArena(
                previousStates,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousStates = JoinArena(
                previousStates,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!previousStates.TryGetArenaInformation(arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            beforeInfo.UseTicket(beforeInfo.Ticket);
            previousStates = previousStates.SetState(arenaInfoAdr, beforeInfo.Serialize());

            var action = new BattleArena7
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            var blockIndex = roundData.StartBlockIndex + 1;
            Assert.Throws<InsufficientBalanceException>(() => action.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = previousStates,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_ExceedPlayCountException()
        {
            const int championshipId = 1;
            const int round = 2;
            var previousStates = _initialStates;
            Assert.True(previousStates.GetSheet<ArenaSheet>().TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena7)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousStates = JoinArena(
                previousStates,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousStates = JoinArena(
                previousStates,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!previousStates.TryGetArenaInformation(arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            var action = new BattleArena7
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 2,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            var blockIndex = roundData.StartBlockIndex + 1;
            Assert.Throws<ExceedPlayCountException>(() => action.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = previousStates,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_ExceedTicketPurchaseLimitException()
        {
            const int championshipId = 1;
            const int round = 2;
            var previousStates = _initialStates;
            Assert.True(previousStates.GetSheet<ArenaSheet>().TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena7)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousStates = JoinArena(
                previousStates,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousStates = JoinArena(
                previousStates,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!previousStates.TryGetArenaInformation(arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            beforeInfo.UseTicket(ArenaInformation.MaxTicketCount);
            var max = roundData.MaxPurchaseCount;
            for (var i = 0; i < max; i++)
            {
                beforeInfo.BuyTicket(roundData.MaxPurchaseCount);
            }

            previousStates = previousStates.SetState(arenaInfoAdr, beforeInfo.Serialize());
            var price = ArenaHelper.GetTicketPrice(
                roundData,
                beforeInfo,
                previousStates.GetGoldCurrency());
            previousStates = previousStates.MintAsset(_agent1Address, price);

            var action = new BattleArena7
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            var blockIndex = roundData.StartBlockIndex + 1;
            Assert.Throws<ExceedTicketPurchaseLimitException>(() => action.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = previousStates,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_ExceedTicketPurchaseLimitDuringIntervalException()
        {
            const int championshipId = 1;
            const int round = 2;
            var previousStates = _initialStates;
            Assert.True(previousStates.GetSheet<ArenaSheet>().TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena7)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousStates = JoinArena(
                previousStates,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousStates = JoinArena(
                previousStates,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!previousStates.TryGetArenaInformation(arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            beforeInfo.UseTicket(ArenaInformation.MaxTicketCount);
            var max = roundData.MaxPurchaseCountWithInterval;
            for (var i = 0; i < max; i++)
            {
                beforeInfo.BuyTicket(roundData.MaxPurchaseCount);
            }

            var purchasedCountDuringInterval = arenaInfoAdr.Derive(BattleArena7.PurchasedCountKey);
            previousStates = previousStates
                .SetState(arenaInfoAdr, beforeInfo.Serialize())
                .SetState(
                    purchasedCountDuringInterval,
                    new Integer(beforeInfo.PurchasedTicketCount));
            var price = ArenaHelper.GetTicketPrice(
                roundData,
                beforeInfo,
                previousStates.GetGoldCurrency());
            previousStates = previousStates.MintAsset(_agent1Address, price);

            var action = new BattleArena7
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            var blockIndex = roundData.StartBlockIndex + 1;
            Assert.Throws<ExceedTicketPurchaseLimitDuringIntervalException>(() => action.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = previousStates,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        [Fact]
        public void Execute_CoolDownBlockException()
        {
            const int championshipId = 1;
            const int round = 2;
            var previousStates = _initialStates;
            Assert.True(previousStates.GetSheet<ArenaSheet>().TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena7)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom();
            previousStates = JoinArena(
                previousStates,
                _agent1Address,
                _avatar1Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousStates = JoinArena(
                previousStates,
                _agent2Address,
                _avatar2Address,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(_avatar1Address, championshipId, round);
            if (!previousStates.TryGetArenaInformation(arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            beforeInfo.UseTicket(ArenaInformation.MaxTicketCount);
            var max = roundData.MaxPurchaseCountWithInterval;
            previousStates = previousStates.SetState(arenaInfoAdr, beforeInfo.Serialize());
            for (var i = 0; i < max; i++)
            {
                var price = ArenaHelper.GetTicketPrice(
                    roundData,
                    beforeInfo,
                    previousStates.GetGoldCurrency());
                previousStates = previousStates.MintAsset(_agent1Address, price);
                beforeInfo.BuyTicket(roundData.MaxPurchaseCount);
            }

            var action = new BattleArena7
            {
                myAvatarAddress = _avatar1Address,
                enemyAvatarAddress = _avatar2Address,
                championshipId = championshipId,
                round = round,
                ticket = 1,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            var blockIndex = roundData.StartBlockIndex + 1;

            var nextStates = action.Execute(new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = previousStates,
                Signer = _agent1Address,
                Random = new TestRandom(),
            });

            Assert.Throws<CoolDownBlockException>(() => action.Execute(new ActionContext
            {
                BlockIndex = blockIndex + 1,
                PreviousStates = nextStates,
                Signer = _agent1Address,
                Random = new TestRandom(),
            }));
        }

        private static (AgentState AgentState, AvatarState AvatarState) GetAgentStateWithAvatarState(
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

        private void Execute(
            long nextBlockIndex,
            int championshipId,
            int round,
            bool isPurchased,
            int ticket,
            int arenaInterval,
            int randomSeed,
            Address myAgentAddress,
            Address myAvatarAddress,
            Address enemyAgentAddress,
            Address enemyAvatarAddress)
        {
            var previousStates = _initialStates;
            Assert.True(_initialStates.GetSheet<ArenaSheet>().TryGetValue(
                championshipId,
                out var row));

            if (!row.TryGetRound(round, out var roundData))
            {
                throw new RoundNotFoundException(
                    $"[{nameof(BattleArena7)}] ChampionshipId({row.ChampionshipId}) - round({round})");
            }

            var random = new TestRandom(randomSeed);
            previousStates = JoinArena(
                previousStates,
                myAgentAddress,
                myAvatarAddress,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);
            previousStates = JoinArena(
                previousStates,
                enemyAgentAddress,
                enemyAvatarAddress,
                roundData.StartBlockIndex,
                championshipId,
                round,
                random);

            var arenaInfoAdr =
                ArenaInformation.DeriveAddress(myAvatarAddress, championshipId, round);
            if (!previousStates.TryGetArenaInformation(arenaInfoAdr, out var beforeInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            if (isPurchased)
            {
                beforeInfo.UseTicket(beforeInfo.Ticket);
                previousStates = previousStates.SetState(arenaInfoAdr, beforeInfo.Serialize());
                for (var i = 0; i < ticket; i++)
                {
                    var price = ArenaHelper.GetTicketPrice(
                        roundData,
                        beforeInfo,
                        previousStates.GetGoldCurrency());
                    previousStates = previousStates.MintAsset(myAgentAddress, price);
                    beforeInfo.BuyTicket(roundData.MaxPurchaseCount);
                }
            }

            var action = new BattleArena7
            {
                myAvatarAddress = myAvatarAddress,
                enemyAvatarAddress = enemyAvatarAddress,
                championshipId = championshipId,
                round = round,
                ticket = ticket,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                runeInfos = new List<RuneSlotInfo>(),
            };

            var myScoreAdr = ArenaScore.DeriveAddress(
                myAvatarAddress,
                championshipId,
                round);
            var enemyScoreAdr = ArenaScore.DeriveAddress(
                enemyAvatarAddress,
                championshipId,
                round);
            if (!previousStates.TryGetArenaScore(myScoreAdr, out var beforeMyScore))
            {
                throw new ArenaScoreNotFoundException($"myScoreAdr : {myScoreAdr}");
            }

            if (!previousStates.TryGetArenaScore(enemyScoreAdr, out var beforeEnemyScore))
            {
                throw new ArenaScoreNotFoundException($"enemyScoreAdr : {enemyScoreAdr}");
            }

            Assert.True(previousStates.TryGetAvatarStateV2(
                myAgentAddress,
                myAvatarAddress,
                out var previousMyAvatarState,
                out _));
            Assert.Empty(previousMyAvatarState.inventory.Materials);

            var gameConfigState = SetArenaInterval(arenaInterval);
            previousStates = previousStates.SetState(GameConfigState.Address, gameConfigState.Serialize());

            var blockIndex = roundData.StartBlockIndex + nextBlockIndex;
            var nextStates = action.Execute(new ActionContext
            {
                PreviousStates = previousStates,
                Signer = myAgentAddress,
                Random = random,
                Rehearsal = false,
                BlockIndex = blockIndex,
            });

            if (!nextStates.TryGetArenaScore(myScoreAdr, out var myAfterScore))
            {
                throw new ArenaScoreNotFoundException($"myScoreAdr : {myScoreAdr}");
            }

            if (!nextStates.TryGetArenaScore(enemyScoreAdr, out var enemyAfterScore))
            {
                throw new ArenaScoreNotFoundException($"enemyScoreAdr : {enemyScoreAdr}");
            }

            if (!nextStates.TryGetArenaInformation(arenaInfoAdr, out var afterInfo))
            {
                throw new ArenaInformationNotFoundException($"arenaInfoAdr : {arenaInfoAdr}");
            }

            var (myWinScore, myDefeatScore, enemyWinScore) =
                ArenaHelper.GetScoresV1(beforeMyScore.Score, beforeEnemyScore.Score);

            var addMyScore = afterInfo.Win * myWinScore + afterInfo.Lose * myDefeatScore;
            var addEnemyScore = afterInfo.Win * enemyWinScore;
            var expectedMyScore = Math.Max(
                beforeMyScore.Score + addMyScore,
                ArenaScore.ArenaScoreDefault);
            var expectedEnemyScore = Math.Max(
                beforeEnemyScore.Score + addEnemyScore,
                ArenaScore.ArenaScoreDefault);

            Assert.Equal(expectedMyScore, myAfterScore.Score);
            Assert.Equal(expectedEnemyScore, enemyAfterScore.Score);
            Assert.Equal(
                isPurchased
                    ? 0
                    : ArenaInformation.MaxTicketCount,
                beforeInfo.Ticket);
            Assert.Equal(0, beforeInfo.Win);
            Assert.Equal(0, beforeInfo.Lose);

            var useTicket = Math.Min(ticket, beforeInfo.Ticket);
            Assert.Equal(beforeInfo.Ticket - useTicket, afterInfo.Ticket);
            Assert.Equal(ticket, afterInfo.Win + afterInfo.Lose);

            var balance = nextStates.GetBalance(
                myAgentAddress,
                nextStates.GetGoldCurrency());
            if (isPurchased)
            {
                Assert.Equal(ticket, afterInfo.PurchasedTicketCount);
            }

            Assert.Equal(0, balance.RawValue);

            var avatarState = nextStates.GetAvatarStateV2(myAvatarAddress);
            var medalCount = 0;
            if (roundData.ArenaType != ArenaType.OffSeason)
            {
                var medalId = ArenaHelper.GetMedalItemId(championshipId, round);
                avatarState.inventory.TryGetItem(medalId, out var medal);
                if (afterInfo.Win > 0)
                {
                    Assert.Equal(afterInfo.Win, medal.count);
                }
                else
                {
                    Assert.Null(medal);
                }

                medalCount = medal?.count ?? 0;
            }

            var materialCount = avatarState.inventory.Materials.Count();
            var high = (ArenaHelper.GetRewardCount(beforeMyScore.Score) * ticket) + medalCount;
            Assert.InRange(materialCount, 0, high);
        }

        private IAccountStateDelta JoinArena(
            IAccountStateDelta states,
            Address signer,
            Address avatarAddress,
            long blockIndex,
            int championshipId,
            int round,
            IRandom random)
        {
            var preCurrency = 1000 * _crystal;
            states = states.MintAsset(signer, preCurrency);

            var action = new JoinArena1
            {
                championshipId = championshipId,
                round = round,
                costumes = new List<Guid>(),
                equipments = new List<Guid>(),
                avatarAddress = avatarAddress,
            };

            states = action.Execute(new ActionContext
            {
                PreviousStates = states,
                Signer = signer,
                Random = random,
                Rehearsal = false,
                BlockIndex = blockIndex,
            });
            return states;
        }

        private GameConfigState SetArenaInterval(int interval)
        {
            var gameConfigState = _initialStates.GetGameConfigState();
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
