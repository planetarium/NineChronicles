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
using Nekoyume.TableData;
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
        /// <param name="boostScheduleSheet">
        /// 액션이 실제로 본 BoostScheduleSheet(<see cref="Helper.BoostScheduleFrontHelper.GetSheetForAction"/>).
        /// 패치 블록에서는 <paramref name="sheets"/> 의 지금 시트와 다를 수 있다.
        /// </param>
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
            BoostScheduleSheet boostScheduleSheet,
            long shatterStrikeMaxDamage,
            out StageSimulator outSimulator,
            out AvatarState outAvatarForRendering)
        {
            outSimulator = null;
            outAvatarForRendering = null;
            var model = new BattleResultPopup.Model();
            var random = new ActionRenderHandler.LocalRandom(eval.RandomSeed);
            var stageRow = sheets.StageSheet[eval.Action.StageId];
            // 액션과 같은 스테이지·블록의 행. 뽑은 뒤 수량만 바꾸므로 전투 재생은 어긋나지 않고 보상 표시만 맞춘다.
            var itemRewardBoost = boostScheduleSheet?.FindActive(
                BoostScheduleSheet.Targets.StageItemReward, eval.Action.StageId, eval.BlockIndex);
            var favRewardBoost = boostScheduleSheet?.FindActive(
                BoostScheduleSheet.Targets.StageFavReward, eval.Action.StageId, eval.BlockIndex);
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
                    StageSimulator.GetWaveRewards(
                        random, stageRow, sheets.MaterialItemSheet, itemRewardBoost: itemRewardBoost),
                    collectionState.GetEffects(sheets.CollectionSheet),
                    sheets.BuffLimitSheet,
                    sheets.BuffLinkSheet,
                    true,
                    shatterStrikeMaxDamage,
                    favRewardBoost);
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
                        ItemFactory.CreateMaterial(sheets.MaterialItemSheet[id]),
                        count));
                }

                foreach (var fav in simulator.FungibleAssetRewards)
                {
                    var ticker = fav.Key;
                    var amount = fav.Value;
                    if (amount <= 0)
                    {
                        continue;
                    }

                    var currency = Lib9c.Currencies.GetMinterlessCurrency(ticker);
                    var fungibleAsset = currency * amount;
                    model.AddReward(new CountableItem(fungibleAsset, amount, true));
                }
            }

            return model;
        }
    }
}
