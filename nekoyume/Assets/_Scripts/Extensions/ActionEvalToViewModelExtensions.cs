using System;
using System.Collections.Generic;
using Lib9c.Renderers;
using Nekoyume.Action;
using Nekoyume.Battle;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI;
using Nekoyume.UI.Model;

namespace Nekoyume
{
    public static class ActionEvalToViewModelExtensions
    {
        /// <summary>
        /// Simulate HackAndSlash with action evaluation.
        /// </summary>
        /// <param name="eval"></param>
        /// <param name="avatarState"></param>
        /// <param name="allRuneState"></param>
        /// <param name="runeSlotState"></param>
        /// <param name="collectionState"></param>
        /// <param name="skillsOnWaveStart"></param>
        /// <param name="sheets"></param>
        /// <param name="outSimulator">First simulator or first winning simulator.</param>
        /// <param name="outAvatarForRendering">The pre-simulate state of the first winning avatar.
        /// If it is not null, <see cref="outSimulator"></see> must be first winning simulator.</param>
        /// <returns>Return summary of all Simulating as <see cref="BattleResultPopup.Model"/>.</returns>
        public static BattleResultPopup.Model GetHackAndSlashReward(
            this ActionEvaluation<HackAndSlash> eval,
            AvatarState avatarState,
            AllRuneState allRuneState,
            RuneSlotState runeSlotState,
            CollectionState collectionState,
            List<Model.Skill.Skill> skillsOnWaveStart,
            TableSheets sheets,
            out StageSimulator outSimulator,
            out AvatarState outAvatarForRendering)
        {
            outSimulator = null;
            outAvatarForRendering = null;
            var model = new BattleResultPopup.Model();
            var random = new ActionRenderHandler.LocalRandom(eval.RandomSeed);
            var stageRow = sheets.StageSheet[eval.Action.StageId];
            for (var i = 0; i < eval.Action.TotalPlayCount; i++)
            {
                var prevAvatarState = (AvatarState)avatarState.Clone();
                var prevExp = avatarState.exp;
                var simulator = new StageSimulator(
                    random,
                    avatarState,
                    i == 0 ? eval.Action.Foods : new List<Guid>(),
                    allRuneState,
                    runeSlotState,
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
                    StageSimulator.GetWaveRewards(random, stageRow, sheets.MaterialItemSheet),
                    collectionState.GetEffects(sheets.CollectionSheet),
                    sheets.DeBuffLimitSheet,
                    sheets.BuffLinkSheet,
                    logEvent: true,
                    States.Instance.GameConfigState.ShatterStrikeMaxDamage);
                simulator.Simulate();
                if (simulator.Log.IsClear)
                {
                    if (outAvatarForRendering is null)
                    {
                        outAvatarForRendering = prevAvatarState;
                        outSimulator = simulator;
                    }

                    simulator.Player.worldInformation.ClearStage(
                        eval.Action.WorldId,
                        eval.Action.StageId,
                        eval.BlockIndex,
                        sheets.WorldSheet,
                        sheets.WorldUnlockSheet
                    );
                }

                avatarState.Update(simulator);
                outSimulator ??= simulator;
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
