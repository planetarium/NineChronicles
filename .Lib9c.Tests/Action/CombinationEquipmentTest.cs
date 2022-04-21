namespace Lib9c.Tests.Action
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Globalization;
    using System.Linq;
    using Bencodex.Types;
    using Libplanet;
    using Libplanet.Action;
    using Libplanet.Assets;
    using Libplanet.Crypto;
    using Nekoyume;
    using Nekoyume.Action;
    using Nekoyume.Extensions;
    using Nekoyume.Model;
    using Nekoyume.Model.Elemental;
    using Nekoyume.Model.Item;
    using Nekoyume.Model.Mail;
    using Nekoyume.Model.State;
    using Serilog;
    using Xunit;
    using Xunit.Abstractions;
    using static SerializeKeys;

    public class CombinationEquipmentTest
    {
        private readonly Address _agentAddress;
        private readonly Address _avatarAddress;
        private readonly Address _slotAddress;
        private readonly TableSheets _tableSheets;
        private readonly IRandom _random;
        private readonly IAccountStateDelta _initialState;
        private readonly AgentState _agentState;
        private readonly AvatarState _avatarState;

        public CombinationEquipmentTest(ITestOutputHelper outputHelper)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.TestOutput(outputHelper)
                .CreateLogger();

            _agentAddress = new PrivateKey().ToAddress();
            _avatarAddress = _agentAddress.Derive("avatar");
            _slotAddress = _avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    0
                )
            );
            var sheets = TableSheetsImporter.ImportSheets();
            _random = new TestRandom();
            _tableSheets = new TableSheets(sheets);

            _agentState = new AgentState(_agentAddress);
            _agentState.avatarAddresses[0] = _avatarAddress;

            var gameConfigState = new GameConfigState();

            _avatarState = new AvatarState(
                _avatarAddress,
                _agentAddress,
                1,
                _tableSheets.GetAvatarSheets(),
                gameConfigState,
                default
            );

            var gold = new GoldCurrencyState(new Currency("NCG", 2, minter: null));

            var combinationSlotState = new CombinationSlotState(
                _slotAddress,
                GameConfig.RequireClearedStageLevel.CombinationEquipmentAction);

            _initialState = new State()
                .SetState(_slotAddress, combinationSlotState.Serialize())
                .SetState(GoldCurrencyState.Address, gold.Serialize());

            foreach (var (key, value) in sheets)
            {
                _initialState =
                    _initialState.SetState(Addresses.TableSheet.Derive(key), value.Serialize());
            }
        }

        [Theory]
        // Tutorial recipe.
        [InlineData(false, false, true, true, false, 3, 0, true, 0, 1, null, true, false, false, null)]
        // Migration AvatarState.
        [InlineData(false, false, true, true, true, 3, 0, true, 0, 1, null, true, false, false, null)]
        // SubRecipe
        [InlineData(true, true, true, true, false, 11, 0, true, 0, 2, 1, true, false, false, null)]
        // Mimisbrunnr Equipment.
        [InlineData(true, true, true, true, false, 11, 0, true, 0, 2, 3, true, true, true, null)]
        // UnlockEquipmentRecipe not executed.
        [InlineData(false, true, true, true, false, 11, 0, true, 0, 2, 1, true, false, false, typeof(FailedLoadStateException))]
        // CRYSTAL not paid.
        [InlineData(true, false, true, true, false, 11, 0, true, 0, 2, 1, true, false, false, typeof(InvalidRecipeIdException))]
        // AgentState not exist.
        [InlineData(true, true, false, true, false, 3, 0, true, 0, 1, null, true, false, false, typeof(FailedLoadStateException))]
        // AvatarState not exist.
        [InlineData(true, true, true, false, false, 3, 0, true, 0, 1, null, true, false, false, typeof(FailedLoadStateException))]
        [InlineData(true, true, true, false, true, 3, 0, true, 0, 1, null, true, false, false, typeof(FailedLoadStateException))]
        // Tutorial not cleared.
        [InlineData(true, true, true, true, false, 1, 0, true, 0, 1, null, true, false, false, typeof(NotEnoughClearedStageLevelException))]
        // CombinationSlotState not exist.
        [InlineData(true, true, true, true, false, 3, 5, true, 0, 1, null, true, false, false, typeof(FailedLoadStateException))]
        // CombinationSlotState locked.
        [InlineData(true, true, true, true, false, 3, 0, false, 0, 1, null, true, false, false, typeof(CombinationSlotUnlockException))]
        // Stage not cleared.
        [InlineData(true, true, true, true, false, 3, 0, true, 0, 2, null, true, false, false, typeof(NotEnoughClearedStageLevelException))]
        // Not enough material.
        [InlineData(true, true, true, true, false, 3, 0, true, 0, 1, null, false, false, false, typeof(NotEnoughMaterialException))]
        // Insufficient NCG.
        [InlineData(true, true, true, true, false, 11, 0, true, 0, 2, 3, true, false, true, typeof(InsufficientBalanceException))]
        public void Execute(
            bool unlockIdsExist,
            bool crystalUnlock,
            bool agentExist,
            bool avatarExist,
            bool migrationRequired,
            int stageId,
            int slotIndex,
            bool slotUnlock,
            long blockIndex,
            int recipeId,
            int? subRecipeId,
            bool enoughMaterial,
            bool balanceExist,
            bool mimisbrunnr,
            Type exc
        )
        {
            IAccountStateDelta state = _initialState;
            if (unlockIdsExist)
            {
                var unlockIds = List.Empty.Add(1.Serialize());
                if (crystalUnlock)
                {
                    for (int i = 2; i < recipeId + 1; i++)
                    {
                        unlockIds = unlockIds.Add(i.Serialize());
                    }
                }

                state = state.SetState(_avatarAddress.Derive("recipe_ids"), unlockIds);
            }

            if (agentExist)
            {
                state = state.SetState(_agentAddress, _agentState.Serialize());

                if (avatarExist)
                {
                    _avatarState.worldInformation = new WorldInformation(
                        0,
                        _tableSheets.WorldSheet,
                        stageId);

                    if (enoughMaterial)
                    {
                        var row = _tableSheets.EquipmentItemRecipeSheet[recipeId];
                        var materialRow = _tableSheets.MaterialItemSheet[row.MaterialId];
                        var material = ItemFactory.CreateItem(materialRow, _random);
                        _avatarState.inventory.AddItem(material, row.MaterialCount);

                        if (subRecipeId.HasValue)
                        {
                            var subRow = _tableSheets.EquipmentItemSubRecipeSheetV2[subRecipeId.Value];

                            foreach (var materialInfo in subRow.Materials)
                            {
                                var subMaterial = ItemFactory.CreateItem(
                                    _tableSheets.MaterialItemSheet[materialInfo.Id], _random);
                                _avatarState.inventory.AddItem(subMaterial, materialInfo.Count);
                            }

                            if (balanceExist)
                            {
                                state = state.MintAsset(
                                    _agentAddress,
                                    subRow.RequiredGold * state.GetGoldCurrency());
                            }
                        }
                    }

                    if (migrationRequired)
                    {
                        state = state.SetState(_avatarAddress, _avatarState.Serialize());
                    }
                    else
                    {
                        var inventoryAddress = _avatarAddress.Derive(LegacyInventoryKey);
                        var worldInformationAddress =
                            _avatarAddress.Derive(LegacyWorldInformationKey);
                        var questListAddress = _avatarAddress.Derive(LegacyQuestListKey);

                        state = state
                            .SetState(_avatarAddress, _avatarState.SerializeV2())
                            .SetState(inventoryAddress, _avatarState.inventory.Serialize())
                            .SetState(
                                worldInformationAddress,
                                _avatarState.worldInformation.Serialize())
                            .SetState(questListAddress, _avatarState.questList.Serialize());
                    }

                    if (!slotUnlock)
                    {
                        // Lock slot.
                        state = state.SetState(
                            _slotAddress,
                            new CombinationSlotState(_slotAddress, stageId + 1).Serialize()
                        );
                    }
                }
            }

            var action = new CombinationEquipment
            {
                avatarAddress = _avatarAddress,
                slotIndex = slotIndex,
                recipeId = recipeId,
                subRecipeId = subRecipeId,
            };

            if (exc is null)
            {
                var nextState = action.Execute(new ActionContext
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    BlockIndex = blockIndex,
                    Random = _random,
                });

                var currency = nextState.GetGoldCurrency();
                Assert.Equal(0 * currency, nextState.GetBalance(_agentAddress, currency));

                var slotState = nextState.GetCombinationSlotState(_avatarAddress, 0);
                Assert.NotNull(slotState.Result);
                Assert.NotNull(slotState.Result.itemUsable);

                var equipment = (Equipment)slotState.Result.itemUsable;
                if (subRecipeId.HasValue)
                {
                    Assert.True(equipment.optionCountFromCombination > 0);

                    if (balanceExist)
                    {
                        Assert.Equal(450 * currency, nextState.GetBalance(Addresses.Blacksmith, currency));
                    }

                    Assert.Equal(mimisbrunnr, equipment.MadeWithMimisbrunnrRecipe);
                    Assert.Equal(
                        mimisbrunnr,
                        equipment.IsMadeWithMimisbrunnrRecipe(
                            _tableSheets.EquipmentItemRecipeSheet,
                            _tableSheets.EquipmentItemSubRecipeSheetV2,
                            _tableSheets.EquipmentItemOptionSheet
                        )
                    );

                    if (mimisbrunnr)
                    {
                        Assert.Equal(ElementalType.Fire, equipment.ElementalType);
                    }
                }
                else
                {
                    Assert.Equal(0, equipment.optionCountFromCombination);
                }

                var nextAvatarState = nextState.GetAvatarStateV2(_avatarAddress);
                var mail = nextAvatarState.mailBox.OfType<CombinationMail>().First();
                Assert.Equal(equipment, mail.attachment.itemUsable);
            }
            else
            {
                Assert.Throws(exc, () => action.Execute(new ActionContext
                {
                    PreviousStates = state,
                    Signer = _agentAddress,
                    BlockIndex = blockIndex,
                    Random = _random,
                }));
            }
        }

        [Fact]
        public void Rehearsal()
        {
            var action = new CombinationEquipment
            {
                avatarAddress = _avatarAddress,
                slotIndex = 0,
                recipeId = 1,
                subRecipeId = 255,
            };
            var slotAddress = _avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    0
                )
            );

            var updatedAddresses = new List<Address>
            {
                _agentAddress,
                _avatarAddress,
                slotAddress,
                _avatarAddress.Derive(LegacyInventoryKey),
                _avatarAddress.Derive(LegacyWorldInformationKey),
                _avatarAddress.Derive(LegacyQuestListKey),
                Addresses.Blacksmith,
            };

            var state = new State();

            var nextState = action.Execute(new ActionContext
            {
                PreviousStates = state,
                Signer = _agentAddress,
                BlockIndex = 0,
                Rehearsal = true,
            });

            Assert.Equal(updatedAddresses.ToImmutableHashSet(), nextState.UpdatedAddresses);
        }

        [Fact]
        public void AddAndUnlockOption()
        {
            var subRecipe = _tableSheets.EquipmentItemSubRecipeSheetV2.Last;
            Assert.NotNull(subRecipe);
            var equipment = (Necklace)ItemFactory.CreateItemUsable(
                _tableSheets.EquipmentItemSheet[10411000],
                Guid.NewGuid(),
                default);
            Assert.Equal(0, equipment.optionCountFromCombination);
            CombinationEquipment.AddAndUnlockOption(
                _agentState,
                equipment,
                _random,
                subRecipe,
                _tableSheets.EquipmentItemOptionSheet,
                _tableSheets.SkillSheet
            );
            Assert.True(equipment.optionCountFromCombination > 0);
        }
    }
}
