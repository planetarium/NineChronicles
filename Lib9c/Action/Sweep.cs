using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Bencodex.Types;
using Libplanet;
using Libplanet.Action;
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Model;
using Nekoyume.Model.Item;
using Nekoyume.Model.Quest;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Action
{
    [Serializable]
    [ActionType("sweep")]
    public class Sweep : GameAction
    {
        public const int usableApStoneCount = 10;

        public Address avatarAddress;
        public int apStoneCount = 0;
        public int worldId;
        public int stageId;

        protected override IImmutableDictionary<string, IValue> PlainValueInternal =>
            new Dictionary<string, IValue>()
            {
                ["avatarAddress"] = avatarAddress.Serialize(),
                ["apStoneCount"] = apStoneCount.Serialize(),
                ["worldId"] = worldId.Serialize(),
                ["stageId"] = stageId.Serialize(),
            }.ToImmutableDictionary();

        protected override void LoadPlainValueInternal(
            IImmutableDictionary<string, IValue> plainValue)
        {
            avatarAddress = plainValue["avatarAddress"].ToAddress();
            apStoneCount = plainValue["apStoneCount"].ToInteger();
            worldId = plainValue["worldId"].ToInteger();
            stageId = plainValue["stageId"].ToInteger();
        }

        public override IAccountStateDelta Execute(IActionContext context)
        {
            var states = context.PreviousStates;
            var inventoryAddress = avatarAddress.Derive(LegacyInventoryKey);
            var worldInformationAddress = avatarAddress.Derive(LegacyWorldInformationKey);
            var questListAddress = avatarAddress.Derive(LegacyQuestListKey);
            if (context.Rehearsal)
            {
                return states
                    .SetState(inventoryAddress, MarkChanged)
                    .SetState(worldInformationAddress, MarkChanged)
                    .SetState(questListAddress, MarkChanged)
                    .SetState(avatarAddress, MarkChanged)
                    .SetState(context.Signer, MarkChanged);
            }

            var addressesHex = GetSignerAndOtherAddressesHex(context, avatarAddress);

            if (!states.TryGetAvatarStateV2(context.Signer, avatarAddress, out var avatarState,
                    out _))
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the avatar state of the signer was failed to load.");
            }

            var sheets = states.GetSheets(
                containQuestSheet: false,
                containStageSimulatorSheets: false,
                sheetTypes: new[]
                {
                    typeof(WorldSheet),
                    typeof(StageSheet),
                    typeof(MaterialItemSheet),
                    typeof(StageWaveSheet),
                    typeof(CharacterLevelSheet),
                });

            var worldSheet = sheets.GetSheet<WorldSheet>();
            if (!worldSheet.TryGetValue(worldId, out var worldRow, false))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(WorldSheet), worldId);
            }

            if (stageId < worldRow.StageBegin ||
                stageId > worldRow.StageEnd)
            {
                throw new SheetRowColumnException(
                    $"{addressesHex}{worldId} world is not contains {worldRow.Id} stage: " +
                    $"{worldRow.StageBegin}-{worldRow.StageEnd}");
            }

            if (!sheets.GetSheet<StageSheet>().TryGetValue(stageId, out var stageRow))
            {
                throw new SheetRowNotFoundException(addressesHex, nameof(StageSheet), stageId);
            }

            var worldInformation = avatarState.worldInformation;
            if (!worldInformation.TryGetWorld(worldId, out var world))
            {
                worldInformation.AddAndUnlockNewWorld(worldRow, context.BlockIndex, worldSheet);
            }

            if (!world.IsStageCleared && stageId > world.StageClearedId)
            {
                throw new InvalidStageException(
                    $"{addressesHex}Aborted as the stage ({worldId}/{stageId}) is not cleared; " +
                    $"cleared stage: {world.StageClearedId}"
                );
            }

            if (worldId == GameConfig.MimisbrunnrWorldId)
            {
                throw new InvalidWorldException(
                    $"{addressesHex} [{worldId}] can't execute sweep action.");
            }

            var materialItemSheet = sheets.GetSheet<MaterialItemSheet>();

            // check ap
            if (apStoneCount > 0)
            {
                if (apStoneCount > usableApStoneCount)
                {
                    throw new UsageLimitExceedException();
                }

                // use apStone
                var row = materialItemSheet.Values.First(r => r.ItemSubType == ItemSubType.ApStone);
                if (!avatarState.inventory.RemoveFungibleItem(row.ItemId, context.BlockIndex, count: apStoneCount))
                {
                    throw new NotEnoughMaterialException(
                        $"{addressesHex}Aborted as the player has no enough material ({row.Id})");
                }
            }
            else
            {
                if (avatarState.actionPoint < stageRow.CostAP)
                {
                    throw new NotEnoughActionPointException(
                        $"{addressesHex}Aborted due to insufficient action point: " +
                        $"{avatarState.actionPoint} < required cost : {stageRow.CostAP})"
                    );
                }
            }

            var gameConfigState = states.GetGameConfigState();
            if (gameConfigState is null)
            {
                throw new FailedLoadStateException(
                    $"{addressesHex}Aborted as the game config state was failed to load.");
            }

            // apply
            var itemPlayCount = gameConfigState.ActionPointMax / stageRow.CostAP * apStoneCount;
            var apPlayCount = avatarState.actionPoint / stageRow.CostAP;
            var playCount = apPlayCount + itemPlayCount;

            var stageWaveSheet = sheets.GetSheet<StageWaveSheet>();
            UpdateMonsterMap(avatarState, stageWaveSheet);

            var rewardItems = GetRewardItems(context.Random, playCount, stageRow, materialItemSheet);
            UpdateInventory(avatarState, rewardItems);

            var levelSheet = sheets.GetSheet<CharacterLevelSheet>();
            var (level, exp) = GetLevelAndExp(levelSheet, avatarState, stageId, playCount);
            UpdateExp(avatarState, level, exp);

            return states
                .SetState(inventoryAddress, avatarState.inventory.Serialize())
                .SetState(worldInformationAddress, avatarState.worldInformation.Serialize())
                .SetState(questListAddress, avatarState.questList.Serialize())
                .SetState(avatarAddress, avatarState.SerializeV2());
        }

        private void UpdateMonsterMap(AvatarState avatarState, StageWaveSheet stageWaveSheet)
        {
            var monsterMap = new CollectionMap();
            if (stageWaveSheet.TryGetValue(stageId, out var stageWaveRow))
            {
                foreach (var monster in stageWaveRow.Waves.SelectMany(wave => wave.Monsters))
                {
                    monsterMap.Add(new KeyValuePair<int, int>(monster.CharacterId, monster.Count));
                }
            }

            avatarState.questList.UpdateMonsterQuest(monsterMap);
        }

        private static void UpdateInventory(AvatarState avatarState, List<ItemBase> rewards)
        {
            var itemMap = new CollectionMap();
            foreach (var reward in rewards)
            {
                itemMap.Add(avatarState.inventory.AddItem(reward));
            }
            avatarState.questList.UpdateCollectQuest(itemMap);
        }

        private void UpdateExp(AvatarState avatarState, int level, long exp)
        {
            var levelUpCount = level - avatarState.level;
            var eventMap = new CollectionMap
                { new KeyValuePair<int, int>((int)QuestEventType.Level, levelUpCount) };
            avatarState.level = level;
            avatarState.exp = exp;
            avatarState.questList.UpdateCompletedQuest(eventMap);
        }

        public static List<ItemBase> GetRewardItems(IRandom random, int playCount,
            StageSheet.Row stageRow,
            MaterialItemSheet materialItemSheet)
        {
            var rewardItems = new List<ItemBase>();
            var maxCount = random.Next(stageRow.DropItemMin, stageRow.DropItemMax + 1);
            for (var i = 0; i < playCount; i++)
            {
                var selector = StageSimulator.SetItemSelector(stageRow, random);
                var rewards = Simulator.SetRewardV2(selector, maxCount, random,
                    materialItemSheet);
                rewardItems.AddRange(rewards);
            }

            rewardItems = rewardItems.OrderBy(x => x.Id).ToList();
            return rewardItems;
        }

        public static (int, long) GetLevelAndExp(CharacterLevelSheet characterLevelSheet,
            AvatarState avatarState, int stageId, int repeatCount)
        {
            var remainCount = repeatCount;
            var currentLevel = avatarState.level;
            var currentExp = avatarState.exp;
            while (remainCount > 0)
            {
                characterLevelSheet.TryGetValue(currentLevel, out var row, true);
                var maxExp = row.Exp + row.ExpNeed;
                var remainExp = maxExp - currentExp;
                var stageExp = StageRewardExpHelper.GetExp(currentLevel, stageId);
                var requiredCount = (int)Math.Ceiling(remainExp / (double)stageExp);
                if (remainCount - requiredCount > 0) // level up
                {
                    currentExp += stageExp * requiredCount;
                    remainCount -= requiredCount;
                    currentLevel += 1;
                }
                else
                {
                    currentExp += stageExp * remainCount;
                    break;
                }
            }

            return (currentLevel, currentExp);
        }
    }
}
