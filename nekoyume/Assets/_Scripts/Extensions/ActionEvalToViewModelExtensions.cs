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
            List<RuneState> runeStates,
            List<Model.Skill.Skill> skillsOnWaveStart,
            TableSheets sheets,
            out StageSimulator firstStageSimulator)
        {
            firstStageSimulator = null;
            var model = new BattleResultPopup.Model();
            var random = new ActionRenderHandler.LocalRandom(eval.RandomSeed);
            var stageRow = sheets.StageSheet[eval.Action.StageId];
            for (var i = 0; i < eval.Action.TotalPlayCount; i++)
            {
                var prevExp = avatarState.exp;
                var simulator = new StageSimulator(
                    random,
                    avatarState,
                    i == 0 ? eval.Action.Foods : new List<Guid>(),
                    runeStates,
                    i == 0 ? skillsOnWaveStart : new List<Model.Skill.Skill>(),
                    eval.Action.WorldId,
                    eval.Action.StageId,
                    stageRow,
                    sheets.StageWaveSheet[eval.Action.StageId],
                    avatarState.worldInformation.IsStageCleared(eval.Action.StageId),
                    StageRewardExpHelper.GetExp(avatarState.level, eval.Action.StageId),
                    sheets.GetStageSimulatorSheets(),
                    sheets.EnemySkillSheet,
                    sheets.CostumeStatSheet,
                    StageSimulatorV2.GetWaveRewards(random, stageRow, sheets.MaterialItemSheet));
                simulator.Simulate();

                if (simulator.Log.IsClear)
                {
                    simulator.Player.worldInformation.ClearStage(
                        eval.Action.WorldId,
                        eval.Action.StageId,
                        eval.BlockIndex,
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
