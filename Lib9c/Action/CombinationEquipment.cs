using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    /// <summary>
    /// Hard forked at https://github.com/planetarium/lib9c/pull/991
    /// Updated at https://github.com/planetarium/lib9c/pull/991
    /// </summary>
    [Serializable]
    [ActionType("combination_equipment11")]
    public class CombinationEquipment : GameAction
    {
        public static readonly Address BlacksmithAddress = ItemEnhancement.BlacksmithAddress;

        public const string AvatarAddressKey = "a";
        public Address avatarAddress;

        public const string SlotIndexKey = "s";
        public int slotIndex;

        public const string RecipeIdKey = "r";
        public int recipeId;

        public const string SubRecipeIdKey = "i";
        public int? subRecipeId;
        public const string PayByCrystalKey = "p";
        public bool payByCrystal;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>
            {
                [AvatarAddressKey] = avatarAddress.Serialize(),
                [SlotIndexKey] = slotIndex.Serialize(),
                [RecipeIdKey] = recipeId.Serialize(),
                [SubRecipeIdKey] = subRecipeId.Serialize(),
                [PayByCrystalKey] = payByCrystal.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue[AvatarAddressKey].ToAddress();
            slotIndex = plainValue[SlotIndexKey].ToInteger();
            recipeId = plainValue[RecipeIdKey].ToInteger();
            subRecipeId = plainValue[SubRecipeIdKey].ToNullableInteger();
            payByCrystal = plainValue[PayByCrystalKey].ToBoolean();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var slotAddress = avatarAddress.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CombinationSlotState.DeriveFormat,
                    slotIndex
                )
            );
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            if (context.Rehearsal)
            {
                return states
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(slotAddress, MarkChanged)
                    .SetState(context.Signer, MarkChanged)
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .MarkBalanceChanged(GoldCurrencyMock, context.Signer, BlacksmithAddress, Addresses.MaterialCost);
            }

            if (recipeId != 1)
            {
                var unlockedRecipeIdsAddress = avatarAddress.Derive("recipe_ids");
                if (!states.TryGetState(unlockedRecipeIdsAddress, out List rawIds))
                {
                    throw new FailedLoadStateException("can't find UnlockedRecipeList.");
                }

                List<int> unlockedIds = rawIds.ToList(StateExtensions.ToInteger);
                if (!unlockedIds.Contains(recipeId))
                {
                    throw new InvalidRecipeIdException($"unlock {recipeId} first.");
                }
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);
            if (!states.TryGetAgentAvatarStatesV2(context.Signer, avatarAddress, out var agentState,
                out var avatarState, out _))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            // Validate Required Cleared Tutorial Stage
            if (!avatarState.worldInformation.IsStageCleared(
                GameConfig.RequireClearedStageLevel.CombinationEquipmentAction))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(
                    addressesHex,
                    GameConfig.RequireClearedStageLevel.CombinationEquipmentAction,
                    current);
            }
            // ~Validate Required Cleared Tutorial Stage

            // Validate SlotIndex
            var slotState = states.GetCombinationSlotState(avatarAddress, slotIndex);
            if (slotState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the slot state is failed to load: # {slotIndex}");
            }

            if (!slotState.Validate(avatarState, context.BlockIndex))
            {
                throw new CombinationSlotUnlockException(
                    $"{addressesHex}Aborted as the slot state is invalid: {slotState} @ {slotIndex}");
            }
            // ~Validate SlotIndex

            // Validate Work
            var costActionPoint = 0;
            var costNCG = 0L;
            var endBlockIndex = context.BlockIndex;
            var requiredFungibleItems = new Dictionary<int, int>();
            var costCrystal = 0 * CrystalCalculator.CRYSTAL;

            Dictionary<Type, (Address, ISheet)> sheets = states.GetSheets(sheetTypes: new[]
            {
                typeof(EquipmentItemRecipeSheet),
                typeof(EquipmentItemSheet),
                typeof(MaterialItemSheet),
                typeof(EquipmentItemSubRecipeSheetV2),
                typeof(EquipmentItemOptionSheet),
                typeof(SkillSheet),
                typeof(CrystalMaterialCostSheet),
                typeof(CrystalFluctuationSheet),
            });

            // Validate RecipeId
            var equipmentItemRecipeSheet = sheets.GetSheet<EquipmentItemRecipeSheet>();
            if (!equipmentItemRecipeSheet.TryGetValue(recipeId, out var recipeRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(EquipmentItemRecipeSheet),
                    recipeId);
            }
            // ~Validate RecipeId

            // Validate Recipe Unlocked.
            if (!avatarState.worldInformation.IsStageCleared(recipeRow.UnlockStage))
            {
                avatarState.worldInformation.TryGetLastClearedStageId(out var current);
                throw new NotEnoughClearedStageLevelException(
                    addressesHex,
                    recipeRow.UnlockStage,
                    current);
            }
            // ~Validate Recipe Unlocked

            // Validate Recipe ResultEquipmentId
            var equipmentItemSheet = sheets.GetSheet<EquipmentItemSheet>();
            if (!equipmentItemSheet.TryGetValue(recipeRow.ResultEquipmentId, out var equipmentRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(equipmentItemSheet),
                    recipeRow.ResultEquipmentId);
            }
            // ~Validate Recipe ResultEquipmentId

            // Validate Recipe Material
            var materialItemSheet = sheets.GetSheet<MaterialItemSheet>();
            if (!materialItemSheet.TryGetValue(recipeRow.MaterialId, out var materialRow))
            {
                throw new SheetRowNotFoundException(
                    addressesHex,
                    nameof(MaterialItemSheet),
                    recipeRow.MaterialId);
            }

            if (requiredFungibleItems.ContainsKey(materialRow.Id))
            {
                requiredFungibleItems[materialRow.Id] += recipeRow.MaterialCount;
            }
            else
            {
                requiredFungibleItems[materialRow.Id] = recipeRow.MaterialCount;
            }
            // ~Validate Recipe Material

            // Validate SubRecipeId
            EquipmentItemSubRecipeSheetV2.Row subRecipeRow = null;
            if (subRecipeId.HasValue)
            {
                if (!recipeRow.SubRecipeIds.Contains(subRecipeId.Value))
                {
                    throw new SheetRowColumnException(
                        $"{addressesHex}Aborted as the sub recipe {subRecipeId.Value} was failed to load from the sheet."
                    );
                }

                var equipmentItemSubRecipeSheetV2 = sheets.GetSheet<EquipmentItemSubRecipeSheetV2>();
                if (!equipmentItemSubRecipeSheetV2.TryGetValue(subRecipeId.Value, out subRecipeRow))
                {
                    throw new SheetRowNotFoundException(
                        addressesHex,
                        nameof(EquipmentItemSubRecipeSheetV2),
                        subRecipeId.Value);
                }

                // Validate SubRecipe Material
                for (var i = subRecipeRow.Materials.Count; i > 0; i--)
                {
                    var materialInfo = subRecipeRow.Materials[i - 1];
                    if (!materialItemSheet.TryGetValue(materialInfo.Id, out materialRow))
                    {
                        throw new SheetRowNotFoundException(
                            addressesHex,
                            nameof(MaterialItemSheet),
                            materialInfo.Id);
                    }

                    if (requiredFungibleItems.ContainsKey(materialRow.Id))
                    {
                        requiredFungibleItems[materialRow.Id] += materialInfo.Count;
                    }
                    else
                    {
                        requiredFungibleItems[materialRow.Id] = materialInfo.Count;
                    }
                }
                // ~Validate SubRecipe Material

                costActionPoint += subRecipeRow.RequiredActionPoint;
                costNCG += subRecipeRow.RequiredGold;
                endBlockIndex += subRecipeRow.RequiredBlockIndex;
            }
            // ~Validate SubRecipeId

            costActionPoint += recipeRow.RequiredActionPoint;
            costNCG += recipeRow.RequiredGold;
            endBlockIndex += recipeRow.RequiredBlockIndex;
            // ~Validate Work

            // Remove Required Materials
            var inventory = avatarState.inventory;
            var crystalMaterialSheet = sheets.GetSheet<CrystalMaterialCostSheet>();
            foreach (var pair in requiredFungibleItems.OrderBy(pair => pair.Key))
            {
                var itemId = pair.Key;
                var requiredCount = pair.Value;
                if (materialItemSheet.TryGetValue(itemId, out materialRow))
                {
                    int itemCount = inventory.TryGetItem(itemId, out Inventory.Item item)
                        ? item.count
                        : 0;
                    if (itemCount < requiredCount && payByCrystal)
                    {
                        costCrystal += CrystalCalculator.CalculateMaterialCost(
                            itemId,
                            requiredCount - itemCount,
                            crystalMaterialSheet);
                        requiredCount = itemCount;
                    }

                    if (requiredCount > 0 && !inventory.RemoveFungibleItem(materialRow.ItemId, context.BlockIndex,
                            requiredCount))
                    {
                        throw new NotEnoughMaterialException(
                            $"{addressesHex}Aborted as the player has no enough material ({pair.Key} * {pair.Value})");
                    }
                }
                else
                {
                    throw new SheetRowNotFoundException(nameof(MaterialItemSheet), itemId);
                }
            }
            // ~Remove Required Materials
            if (costCrystal > 0 * CrystalCalculator.CRYSTAL)
            {
                var crystalFluctuationSheet = sheets.GetSheet<CrystalFluctuationSheet>();
                var row = crystalFluctuationSheet.Values
                    .First(r => r.Type == CrystalFluctuationSheet.ServiceType.Combination);
                var (dailyCostState, weeklyCostState, prevWeeklyCostState, beforePrevWeeklyCostState) = states.GetCrystalCostStates(context.BlockIndex, row.BlockInterval);
                costCrystal = CrystalCalculator.CalculateCombinationCost(costCrystal, row: row, prevWeeklyCostState: prevWeeklyCostState, beforePrevWeeklyCostState: beforePrevWeeklyCostState);
                // Update Daily Formula.
                dailyCostState.Count++;
                dailyCostState.CRYSTAL += costCrystal;
                // Update Weekly Formula.
                weeklyCostState.Count++;
                weeklyCostState.CRYSTAL += costCrystal;

                var crystalBalance = states.GetBalance(context.Signer, CrystalCalculator.CRYSTAL);
                if (costCrystal > crystalBalance)
                {
                    throw new NotEnoughFungibleAssetValueException($"required {costCrystal}, but balance is {crystalBalance}");
                }

                states = states
                    .SetState(dailyCostState.Address, dailyCostState.Serialize())
                    .SetState(weeklyCostState.Address, weeklyCostState.Serialize())
                    .TransferAsset(context.Signer, Addresses.MaterialCost, costCrystal);
            }

            // Subtract Required ActionPoint
            if (costActionPoint > 0)
            {
                if (avatarState.actionPoint < costActionPoint)
                {
                    throw new NotEnoughActionPointException(
                        $"{addressesHex}Aborted due to insufficient action point: {avatarState.actionPoint} < {costActionPoint}"
                    );
                }

                avatarState.actionPoint -= costActionPoint;
            }
            // ~Subtract Required ActionPoint

            // Transfer Required NCG
            if (costNCG > 0L)
            {
                states = states.TransferAsset(
                    context.Signer,
                    BlacksmithAddress,
                    states.GetGoldCurrency() * costNCG
                );
            }
            // ~Transfer Required NCG

            // Create Equipment
            var equipment = (Equipment) ItemFactory.CreateItemUsable(
                equipmentRow,
                context.Random.GenerateRandomGuid(),
                endBlockIndex,
                madeWithMimisbrunnrRecipe: recipeRow.IsMimisBrunnrSubRecipe(subRecipeId));

            if (!(subRecipeRow is null))
            {
                AddAndUnlockOption(
                    agentState,
                    equipment,
                    context.Random,
                    subRecipeRow,
                    sheets.GetSheet<EquipmentItemOptionSheet>(),
                    sheets.GetSheet<SkillSheet>()
                );
                endBlockIndex = equipment.RequiredBlockIndex;
            }
            // ~Create Equipment

            // Add or Update Equipment
            avatarState.blockIndex = context.BlockIndex;
            avatarState.updatedAt = context.BlockIndex;
            avatarState.questList.UpdateCombinationEquipmentQuest(recipeId);
            avatarState.UpdateFromCombination(equipment);
            avatarState.UpdateQuestRewards(materialItemSheet);
            // ~Add or Update Equipment

            // Update Slot
            var mailId = context.Random.GenerateRandomGuid();
            var attachmentResult = new CombinationConsumable5.ResultModel
            {
                id = mailId,
                actionPoint = costActionPoint,
                gold = costNCG,
                materials = requiredFungibleItems.ToDictionary(
                    e => ItemFactory.CreateMaterial(materialItemSheet, e.Key),
                    e => e.Value),
                itemUsable = equipment,
                recipeId = recipeId,
                subRecipeId = subRecipeId,
            };
            slotState.Update(attachmentResult, context.BlockIndex, endBlockIndex);
            // ~Update Slot

            // Create Mail
            var mail = new CombinationMail(
                attachmentResult,
                context.BlockIndex,
                mailId,
                endBlockIndex);
            avatarState.Update(mail);
            // ~Create Mail

            return states
                .SetState(avatarAddress, avatarState.SerializeV2())
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(slotAddress, slotState.Serialize())
                .SetState(context.Signer, agentState.Serialize());
        }

        public static void AddAndUnlockOption(
            AgentState agentState,
            Equipment equipment,
            IRandom random,
            EquipmentItemSubRecipeSheetV2.Row subRecipe,
            EquipmentItemOptionSheet optionSheet,
            SkillSheet skillSheet
        )
        {
            foreach (var optionInfo in subRecipe.Options
                .OrderByDescending(e => e.Ratio)
                .ThenBy(e => e.RequiredBlockIndex)
                .ThenBy(e => e.Id))
            {
                if (!optionSheet.TryGetValue(optionInfo.Id, out var optionRow))
                {
                    continue;
                }

                var value = random.Next(1, GameConfig.MaximumProbability + 1);
                if (value > optionInfo.Ratio)
                {
                    continue;
                }

                if (optionRow.StatType != StatType.NONE)
                {
                    var statMap = CombinationEquipment5.GetStat(optionRow, random);
                    equipment.StatsMap.AddStatAdditionalValue(statMap.StatType, statMap.Value);
                    equipment.Update(equipment.RequiredBlockIndex + optionInfo.RequiredBlockIndex);
                    equipment.optionCountFromCombination++;
                    agentState.unlockedOptions.Add(optionRow.Id);
                }
                else
                {
                    var skill = CombinationEquipment5.GetSkill(optionRow, skillSheet, random);
                    if (!(skill is null))
                    {
                        equipment.Skills.Add(skill);
                        equipment.Update(equipment.RequiredBlockIndex + optionInfo.RequiredBlockIndex);
                        equipment.optionCountFromCombination++;
                        agentState.unlockedOptions.Add(optionRow.Id);
                    }
                }
            }
        }
    }
}
