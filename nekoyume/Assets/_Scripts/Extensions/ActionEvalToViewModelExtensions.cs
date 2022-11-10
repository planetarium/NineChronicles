using System;
using System.Collections.Generic;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.UI;
using Nekoyume.UI.Model;

namespace Nekoyume
{
    public static class ActionEvalToViewModelExtensions
    {
        public static BattleResultPopup.Model GetHackAndSlashReward(
            this ActionBase.ActionEvaluation<HackAndSlash> eval,
            AvatarState avatarState,
            List<Model.Skill.Skill> skillsOnWaveStart,
            TableSheets sheets,
            out StageSimulator firstStageSimulator) =>
            eval.Action.GetHackAndSlashReward(
                eval.BlockIndex,
                eval.RandomSeed,
                avatarState,
                skillsOnWaveStart,
                sheets,
                out firstStageSimulator);

        public static BattleResultPopup.Model GetHackAndSlashReward(
            this HackAndSlash hackAndSlash,
            long blockIndex,
            int randomSeed,
            AvatarState avatarState,
            List<Model.Skill.Skill> skillsOnWaveStart,
            TableSheets sheets,
            out StageSimulator firstStageSimulator)
        {
            firstStageSimulator = null;
            var model = new BattleResultPopup.Model();
            var random = new ActionRenderHandler.LocalRandom(randomSeed);
            var stageRow = sheets.StageSheet[hackAndSlash.StageId];
            for (var i = 0; i < hackAndSlash.PlayCount; i++)
            {
                var prevExp = avatarState.exp;
                var simulator = new StageSimulator(
                    random,
                    avatarState,
                    i == 0 ? hackAndSlash.Foods : new List<Guid>(),
                    i == 0 ? skillsOnWaveStart : new List<Model.Skill.Skill>(),
                    hackAndSlash.WorldId,
                    hackAndSlash.StageId,
                    stageRow,
                    sheets.StageWaveSheet[hackAndSlash.StageId],
                    avatarState.worldInformation.IsStageCleared(hackAndSlash.StageId),
                    StageRewardExpHelper.GetExp(avatarState.level, hackAndSlash.StageId),
                    sheets.GetStageSimulatorSheets(),
                    sheets.EnemySkillSheet,
                    sheets.CostumeStatSheet,
                    StageSimulator.GetWaveRewards(random, stageRow, sheets.MaterialItemSheet));
                simulator.Simulate();

                if (simulator.Log.IsClear)
                {
                    simulator.Player.worldInformation.ClearStage(
                        hackAndSlash.WorldId,
                        hackAndSlash.StageId,
                        blockIndex,
                        sheets.WorldSheet,
                        sheets.WorldUnlockSheet
                    );
                }

                avatarState.Update(simulator);
                firstStageSimulator ??= simulator;
                model.Exp += simulator.Player.Exp.Current - prevExp;
                model.ClearedCountForEachWaves[simulator.Log.clearedWaveNumber]++;
                foreach (var (id, count) in simulator.ItemMap)
                {
                    model.AddReward(new CountableItem(
                        ItemFactory.CreateMaterial(TableSheets.Instance.MaterialItemSheet[id]),
                        count));
                }
            }

            return model;
        }
    }
}
