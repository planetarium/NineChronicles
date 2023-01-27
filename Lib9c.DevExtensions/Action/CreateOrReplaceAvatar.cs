using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Bencodex.Types;
using Lib9c.DevExtensions.Action.Interface;
using Libplanet.Action;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.Quest;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Lib9c.DevExtensions.Action
{
    [Serializable]
    [ActionType("create_or_replace_avatar")]
    public class CreateOrReplaceAvatar : GameAction, ICreateOrReplaceAvatar
    {
        public int AvatarIndex { get; private set; }
        public string Name { get; private set; }
        public int Hair { get; private set; }
        public int Lens { get; private set; }
        public int Ear { get; private set; }
        public int Tail { get; private set; }
        public int Level { get; private set; }
        public (int itemId, int enhancement)[] Equipments { get; private set; }

        protected override IImmutableDictionary<string, IValue> PlainValueInternal
        {
            get
            {
                var list = List.Empty
                    .Add(AvatarIndex.Serialize())
                    .Add(Name.Serialize())
                    .Add(Hair.Serialize())
                    .Add(Lens.Serialize())
                    .Add(Ear.Serialize())
                    .Add(Tail.Serialize())
                    .Add(Level.Serialize());

                var equipmentList = List.Empty;
                if (Equipments != null &&
                    Equipments.Length > 0)
                {
                    for (var i = 0; i < Equipments.Length; i++)
                    {
                        var (itemId, enhancement) = Equipments[i];
                        equipmentList = equipmentList.Add(List.Empty
                            .Add(itemId.Serialize())
                            .Add(enhancement.Serialize()));
                    }
                }

                list = list.Add(equipmentList);

                return new Dictionary<string, IValue>
                {
                    ["l"] = list.Serialize(),
                }.ToImmutableDictionary();
            }
        }

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            var list = (List)plainValue["l"];
            AvatarIndex = list[0].ToInteger();
            Name = list[1].ToDotnetString();
            Hair = list[2].ToInteger();
            Lens = list[3].ToInteger();
            Ear = list[4].ToInteger();
            Tail = list[5].ToInteger();
            Level = list[6].ToInteger();
            var equipments = (List)list[7];
            Equipments = new (int itemId, int enhancement)[equipments.Count];
            for (var i = 0; i < equipments.Count; i++)
            {
                var equipment = (List)equipments[i];
                Equipments[i] = (
                    equipment[0].ToInteger(),
                    equipment[1].ToInteger());
            }
        }

        public CreateOrReplaceAvatar() : this(0)
        {
        }

        public CreateOrReplaceAvatar(
            int avatarIndex,
            string name = "Avatar",
            int hair = 0,
            int lens = 0,
            int ear = 0,
            int tail = 0,
            int level = 1,
            (int itemId, int enhancement)[] equipments = null)
        {
            if (avatarIndex < 0 ||
                avatarIndex >= GameConfig.SlotCount)
            {
                throw new ArgumentException(
                    $"Invalid avatarIndex: ({avatarIndex})." +
                    $" It must be between 0 and {GameConfig.SlotCount - 1}.",
                    nameof(avatarIndex));
            }

            if (!Regex.IsMatch(name, GameConfig.AvatarNickNamePattern))
            {
                throw new ArgumentException(
                    $"Invalid nickname: \"{name}\"." +
                    " Nickname must be between 2 and 20" +
                    " characters long and can only contain alphabets and numbers.",
                    nameof(name));
            }

            if (hair < 0)
            {
                throw new ArgumentException(
                    $"Invalid hair: ({hair})." +
                    " It must be greater than or equal to 0.",
                    nameof(hair));
            }

            if (lens < 0)
            {
                throw new ArgumentException(
                    $"Invalid lens: ({lens})." +
                    " It must be greater than or equal to 0.",
                    nameof(lens));
            }

            if (ear < 0)
            {
                throw new ArgumentException(
                    $"Invalid ear: ({ear})." +
                    " It must be greater than or equal to 0.",
                    nameof(ear));
            }

            if (tail < 0)
            {
                throw new ArgumentException(
                    $"Invalid tail: ({tail})." +
                    " It must be greater than or equal to 0.",
                    nameof(tail));
            }

            if (level < 1)
            {
                throw new ArgumentException(
                    $"Invalid level: ({level})." +
                    " It must be greater than or equal to 1.",
                    nameof(level));
            }

            if (equipments != null)
            {
                foreach (var tuple in equipments)
                {
                    var (itemId, enhancement) = tuple;
                    if (itemId < 0)
                    {
                        throw new ArgumentException(
                            $"Invalid itemId: ({itemId})." +
                            " It must be greater than or equal to 0.",
                            nameof(itemId));
                    }

                    if (enhancement < 0)
                    {
                        throw new ArgumentException(
                            $"Invalid enhancement: ({enhancement})." +
                            " It must be greater than or equal to 0.",
                            nameof(enhancement));
                    }
                }
            }

            AvatarIndex = avatarIndex;
            Name = name;
            Hair = hair;
            Lens = lens;
            Ear = ear;
            Tail = tail;
            Level = level;
            Equipments = equipments ?? Array.Empty<(int itemId, int enhancement)>();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            if (context.Rehearsal)
            {
                return context.PreviousStates;
            }

            var states = context.PreviousStates;
            var agentAddr = context.Signer;
            var avatarAddr = context.Signer.Derive(
                string.Format(
                    CultureInfo.InvariantCulture,
                    CreateAvatar.DeriveFormat,
                    AvatarIndex
                )
            );

            // Set AgentState.
            var agent = states.GetState(agentAddr) is Dictionary agentDict
                ? new AgentState(agentDict)
                : new AgentState(agentAddr);
            if (!agent.avatarAddresses.ContainsKey(AvatarIndex))
            {
                agent.avatarAddresses[AvatarIndex] = avatarAddr;
                states = states.SetState(agentAddr, agent.Serialize());
            }
            // ~Set AgentState.

            var sheets = states.GetSheets(
                containAvatarSheets: true,
                containQuestSheet: true,
                sheetTypes: new[]
                {
                    typeof(WorldSheet),
                    typeof(EquipmentItemRecipeSheet),
                    typeof(EquipmentItemSubRecipeSheet),
                    typeof(EquipmentItemSheet),
                    typeof(EnhancementCostSheetV2),
                    typeof(EquipmentItemRecipeSheet),
                    typeof(EquipmentItemSubRecipeSheetV2),
                    typeof(EquipmentItemOptionSheet),
                    typeof(SkillSheet),
                });
            var gameConfig = states.GetGameConfigState();

            // Set AvatarState.
            var avatar = new AvatarState(
                avatarAddr,
                agentAddr,
                context.BlockIndex,
                sheets.GetAvatarSheets(),
                gameConfig,
                default,
                Name)
            {
                level = Level,
                hair = Hair,
                lens = Lens,
                ear = Ear,
                tail = Tail,
            };
            states = states.SetState(avatarAddr, avatar.Serialize());
            // ~Set AvatarState.

            // Set WorldInformation.
            var worldInfoAddr = avatarAddr.Derive(LegacyWorldInformationKey);
            var worldInfo = new WorldInformation(
                context.BlockIndex,
                sheets.GetSheet<WorldSheet>(),
                false);
            states = states.SetState(worldInfoAddr, worldInfo.Serialize());
            // ~Set WorldInformation.

            // Set QuestList.
            var questListAddr = avatarAddr.Derive(LegacyQuestListKey);
            var questList = new QuestList(
                sheets.GetQuestSheet(),
                sheets.GetSheet<QuestRewardSheet>(),
                sheets.GetSheet<QuestItemRewardSheet>(),
                sheets.GetSheet<EquipmentItemRecipeSheet>(),
                sheets.GetSheet<EquipmentItemSubRecipeSheet>());
            states = states.SetState(questListAddr, questList.Serialize());
            // ~Set QuestList.

            // Set Inventory.
            var inventoryAddr = avatarAddr.Derive(LegacyInventoryKey);
            var inventory = new Inventory();
            var equipmentItemSheet = sheets.GetSheet<EquipmentItemSheet>();
            var enhancementCostSheetV2 = sheets.GetSheet<EnhancementCostSheetV2>();
            var recipeSheet = sheets.GetSheet<EquipmentItemRecipeSheet>();
            var subRecipeSheetV2 = sheets.GetSheet<EquipmentItemSubRecipeSheetV2>();
            var optionSheet = sheets.GetSheet<EquipmentItemOptionSheet>();
            var skillSheet = sheets.GetSheet<SkillSheet>();

            // FIXME: Separate the logic of equipment creation from the action.
            for (var i = 0; i < Equipments.Length; i++)
            {
                var (itemId, enhancement) = Equipments[i];
                if (!equipmentItemSheet.TryGetValue(itemId, out var itemRow, true))
                {
                    continue;
                }

                // NOTE: Do not use `level` argument at here.
                var equipment = (Equipment)ItemFactory.CreateItemUsable(
                    itemRow,
                    context.Random.GenerateRandomGuid(),
                    context.BlockIndex,
                    0);
                var recipe = recipeSheet.OrderedList!
                    .First(e => e.ResultEquipmentId == itemId);
                var subRecipe = subRecipeSheetV2[recipe.SubRecipeIds[1]];
                CombinationEquipment.AddAndUnlockOption(
                    agent,
                    equipment,
                    context.Random,
                    subRecipe,
                    optionSheet,
                    skillSheet);
                var additionalOptionStats = equipment.StatsMap.GetAdditionalStats().ToArray();
                foreach (var statMapEx in additionalOptionStats)
                {
                    equipment.StatsMap.SetStatAdditionalValue(statMapEx.StatType, 0);
                }

                equipment.Skills.Clear();
                equipment.BuffSkills.Clear();

                var options = subRecipe.Options
                    .Select(e => optionSheet[e.Id])
                    .ToArray();
                foreach (var option in options)
                {
                    if (option.StatType == StatType.NONE)
                    {
                        var skillRow = skillSheet[option.SkillId];
                        var skill = SkillFactory.Get(
                            skillRow,
                            option.SkillDamageMax,
                            option.SkillChanceMax);
                        equipment.Skills.Add(skill);

                        continue;
                    }

                    equipment.StatsMap.AddStatAdditionalValue(option.StatType, option.StatMax);
                }

                if (enhancement > 0 &&
                    ItemEnhancement.TryGetRow(
                        equipment,
                        enhancementCostSheetV2,
                        out var row))
                {
                    for (var j = 0; j < enhancement; j++)
                    {
                        equipment.LevelUpV2(context.Random, row, true);
                    }
                }

                inventory.AddItem(equipment);
            }

            states = states.SetState(inventoryAddr, inventory.Serialize());
            // ~Set Inventory.

            // Set CombinationSlot.
            for (var i = 0; i < AvatarState.CombinationSlotCapacity; i++)
            {
                var slotAddr = avatarAddr.Derive(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        CombinationSlotState.DeriveFormat,
                        i
                    )
                );
                var slot = new CombinationSlotState(
                    slotAddr,
                    GameConfig.RequireClearedStageLevel.CombinationEquipmentAction);
                states = states.SetState(slotAddr, slot.Serialize());
            }
            // ~Set CombinationSlot.

            return states;
        }
    }
}
