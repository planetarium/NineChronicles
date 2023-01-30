namespace Lib9c.Tests.Action
{
    using System;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Exceptions;
    using Nekoyume.Helper;
    using Nekoyume.Model.Rune;
    using Nekoyume.Model.State;
    using Nekoyume.TableData;
    using Nekoyume.TableData.Pet;
    using Xunit;
    using static Lib9c.SerializeKeys;

    public class PetEnhancement0Test
    {
        private Currency _goldCurrency;

        public PetEnhancement0Test()
        {
            _goldCurrency = Currency.Legacy("NCG", 2, null);
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(1, 2, 1)]
        [InlineData(1, 10, 1)]
        public void Execute(int petId, int targetLevel, int prevLevel = 0)
        {
            var agentAddress = new PrivateKey().ToAddress();
            var avatarAddress = new PrivateKey().ToAddress();
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var blockIndex = tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var rankingMapAddress = avatarAddress.Derive("ranking_map");
            var agentState = new AgentState(agentAddress);
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress
            );
            agentState.avatarAddresses.Add(0, avatarAddress);
            var state = new State()
                .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetState(agentAddress, agentState.SerializeV2())
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize());

            foreach (var (key, value) in sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var costSheet = state.GetSheet<PetCostSheet>();
            if (!costSheet.TryGetValue(petId, out var costRow))
            {
                throw new SheetRowNotFoundException(nameof(costSheet), petId);
            }

            if (!costRow.TryGetCost(targetLevel, out var cost))
            {
                throw new PetCostNotFoundException($"Can not find cost by TargetLevel({targetLevel}).");
            }

            var petSheet = state.GetSheet<PetSheet>();
            if (!petSheet.TryGetValue(petId, out var petRow))
            {
                throw new RuneNotFoundException($"[{nameof(Execute)}] ");
            }

            var petAddress = PetState.DeriveAddress(avatarAddress, petId);
            if (prevLevel > 0)
            {
                var petState = new PetState(petId);
                while (petState.Level < prevLevel)
                {
                    petState.LevelUp();
                }

                state = state.SetState(
                    petAddress,
                    petState.Serialize());
            }

            var ncgCurrency = state.GetGoldCurrency();
            var soulStoneCurrency = Currency.Legacy(petRow.SoulStoneTicker, 0, minters: null);

            var (ncgBal, soulStoneBal) = PetHelper.CalculateEnhancementCost(costSheet, petId, prevLevel, targetLevel);

            state = state.MintAsset(agentAddress, (ncgBal + 1) * ncgCurrency);
            state = state.MintAsset(avatarState.address, (soulStoneBal + 1) * soulStoneCurrency);

            var action = new PetEnhancement
            {
                AvatarAddress = avatarState.address,
                PetId = petId,
                TargetLevel = targetLevel,
            };
            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = state,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = agentAddress,
            };

            var nextState = action.Execute(ctx);
            if (!nextState.TryGetState(petAddress, out List rawPetState))
            {
                throw new Exception();
            }

            var nextNcgBal = nextState.GetBalance(agentAddress, ncgCurrency);
            var nextSoulStoneBal = nextState.GetBalance(avatarAddress, soulStoneCurrency);

            if (cost.NcgQuantity != 0)
            {
                Assert.Equal(1, nextNcgBal.MajorUnit);
            }

            if (cost.SoulStoneQuantity != 0)
            {
                Assert.Equal(1, nextSoulStoneBal.MajorUnit);
            }

            var nextPetState = new PetState(rawPetState);
            Assert.Equal(targetLevel, nextPetState.Level);
        }

        [Fact]
        public void ExecuteFailedLoadStateException()
        {
            var agentAddress = new PrivateKey().ToAddress();
            var avatarAddress = new PrivateKey().ToAddress();
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var blockIndex = tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var rankingMapAddress = avatarAddress.Derive("ranking_map");
            var agentState = new AgentState(agentAddress);
            var anotherAgent = new AgentState(new PrivateKey().ToAddress());
            var avatarState = new AvatarState(
                avatarAddress,
                anotherAgent.address,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress
            );
            var state = new State()
                .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetState(agentAddress, agentState.Serialize())
                .SetState(anotherAgent.address, anotherAgent.Serialize())
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize());

            foreach (var (key, value) in sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var action = new PetEnhancement
            {
                AvatarAddress = avatarState.address,
                PetId = 1,
                TargetLevel = 1,
            };

            Assert.Throws<FailedLoadStateException>(() =>
            {
                action.Execute(new ActionContext
                {
                    BlockIndex = blockIndex,
                    PreviousStates = state,
                    Random = new TestRandom(),
                    Rehearsal = false,
                    Signer = agentAddress,
                });
            });
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(1, 1)]
        [InlineData(1, 2)]
        public void ExecuteInvalidActionFieldException(int targetLevel, int prevLevel = 0)
        {
            const int petId = 1;
            var agentAddress = new PrivateKey().ToAddress();
            var avatarAddress = new PrivateKey().ToAddress();
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var blockIndex = tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var rankingMapAddress = avatarAddress.Derive("ranking_map");
            var agentState = new AgentState(agentAddress);
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress
            );
            agentState.avatarAddresses.Add(0, avatarAddress);
            var state = new State()
                .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetState(agentAddress, agentState.SerializeV2())
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize());

            foreach (var (key, value) in sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var petAddress = PetState.DeriveAddress(avatarAddress, petId);
            if (prevLevel > 0)
            {
                var petState = new PetState(petId);
                while (petState.Level < prevLevel)
                {
                    petState.LevelUp();
                }

                state = state.SetState(
                    petAddress,
                    petState.Serialize());
            }

            var action = new PetEnhancement
            {
                AvatarAddress = avatarState.address,
                PetId = petId,
                TargetLevel = targetLevel,
            };
            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = state,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = agentAddress,
            };

            Assert.Throws<InvalidActionFieldException>(() =>
            {
                action.Execute(ctx);
            });
        }

        [Theory]
        [InlineData(1, 0)]
        [InlineData(0, 1)]
        public void ExecuteSheetRowNotFoundException(int idToIncludeInPetSheet, int idToIncludeInPetCostSheet)
        {
            var agentAddress = new PrivateKey().ToAddress();
            var avatarAddress = new PrivateKey().ToAddress();
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var blockIndex = tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var rankingMapAddress = avatarAddress.Derive("ranking_map");
            var agentState = new AgentState(agentAddress);
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress
            );
            agentState.avatarAddresses.Add(0, avatarAddress);
            var state = new State()
                .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetState(agentAddress, agentState.SerializeV2())
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize());

            foreach (var (key, value) in sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var petSheet = new PetSheet();
            petSheet.Set(@$"id,grade,SoulStoneTicker,_petName
{idToIncludeInPetSheet},1,Soulstone_001,D:CC 블랙캣");
            state = state.SetState(Addresses.TableSheet.Derive(petSheet.Name), petSheet.Serialize());

            var petCostSheet = new PetCostSheet();
            petCostSheet.Set(@$"ID,_PET NAME,PetLevel,SoulStoneQuantity,NcgQuantity
{idToIncludeInPetCostSheet},D:CC 블랙캣,1,10,0");
            state = state.SetState(Addresses.TableSheet.Derive(petCostSheet.Name), petCostSheet.Serialize());

            var action = new PetEnhancement
            {
                AvatarAddress = avatarState.address,
                PetId = 1,
                TargetLevel = 1,
            };
            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = state,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = agentAddress,
            };

            Assert.Throws<SheetRowNotFoundException>(() =>
            {
                action.Execute(ctx);
            });
        }

        [Fact]
        public void ExecutePetCostNotFoundException()
        {
            const int petId = 1;
            var agentAddress = new PrivateKey().ToAddress();
            var avatarAddress = new PrivateKey().ToAddress();
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var blockIndex = tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var rankingMapAddress = avatarAddress.Derive("ranking_map");
            var agentState = new AgentState(agentAddress);
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress
            );
            agentState.avatarAddresses.Add(0, avatarAddress);
            var state = new State()
                .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetState(agentAddress, agentState.SerializeV2())
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize());

            foreach (var (key, value) in sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var action = new PetEnhancement
            {
                AvatarAddress = avatarState.address,
                PetId = petId,
                TargetLevel = int.MaxValue,
            };
            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = state,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = agentAddress,
            };

            Assert.Throws<PetCostNotFoundException>(() =>
            {
                action.Execute(ctx);
            });
        }

        [Theory]
        [InlineData(1, true, true)]
        [InlineData(10, true, false)]
        [InlineData(10, false, true)]
        [InlineData(10, true, true)]
        [InlineData(30, false, true)]
        [InlineData(30, true, false)]
        [InlineData(30, true, true)]
        public void ExecuteNotEnoughFungibleAssetValueException(int targetLevel, bool notEnoughNcg, bool notEnoughSoulStone)
        {
            const int petId = 1;
            var agentAddress = new PrivateKey().ToAddress();
            var avatarAddress = new PrivateKey().ToAddress();
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            var sheets = TableSheetsImporter.ImportSheets();
            var tableSheets = new TableSheets(sheets);
            var blockIndex = tableSheets.WorldBossListSheet.Values
                .OrderBy(x => x.StartedBlockIndex)
                .First()
                .StartedBlockIndex;

            var goldCurrencyState = new GoldCurrencyState(_goldCurrency);
            var rankingMapAddress = avatarAddress.Derive("ranking_map");
            var agentState = new AgentState(agentAddress);
            var avatarState = new AvatarState(
                avatarAddress,
                agentAddress,
                0,
                tableSheets.GetAvatarSheets(),
                new GameConfigState(),
                rankingMapAddress
            );
            agentState.avatarAddresses.Add(0, avatarAddress);
            var state = new State()
                .SetState(goldCurrencyState.address, goldCurrencyState.Serialize())
                .SetState(agentAddress, agentState.SerializeV2())
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize());

            foreach (var (key, value) in sheets)
            {
                state = state.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }

            var costSheet = state.GetSheet<PetCostSheet>();
            if (!costSheet.TryGetValue(petId, out var costRow))
            {
                throw new SheetRowNotFoundException(nameof(costSheet), petId);
            }

            if (!costRow.TryGetCost(targetLevel, out _))
            {
                throw new PetCostNotFoundException($"Can not find cost by TargetLevel({targetLevel}).");
            }

            var petSheet = state.GetSheet<PetSheet>();
            if (!petSheet.TryGetValue(petId, out var petRow))
            {
                throw new RuneNotFoundException($"[{nameof(Execute)}] ");
            }

            var ncgCurrency = state.GetGoldCurrency();
            var soulStoneCurrency = Currency.Legacy(petRow.SoulStoneTicker, 0, minters: null);

            var (ncgBal, soulStoneBal) = PetHelper.CalculateEnhancementCost(costSheet, petId, 0, targetLevel);

            state = state.MintAsset(agentAddress, (ncgBal + (notEnoughNcg ? -1 : 0)) * ncgCurrency);
            state = state.MintAsset(avatarState.address, (soulStoneBal + (notEnoughSoulStone ? -1 : 0)) * soulStoneCurrency);

            var action = new PetEnhancement
            {
                AvatarAddress = avatarState.address,
                PetId = petId,
                TargetLevel = targetLevel,
            };
            var ctx = new ActionContext
            {
                BlockIndex = blockIndex,
                PreviousStates = state,
                Random = new TestRandom(),
                Rehearsal = false,
                Signer = agentAddress,
            };

            Assert.Throws<NotEnoughFungibleAssetValueException>(() =>
            {
                action.Execute(ctx);
            });
        }
    }
}
