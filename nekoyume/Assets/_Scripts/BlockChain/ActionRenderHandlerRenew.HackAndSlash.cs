using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using mixpanel;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI;
using UniRx;
using UnityEngine;
using ObservableExtensions = UniRx.ObservableExtensions;

namespace Nekoyume.BlockChain
{
    public partial class ActionRenderHandlerRenew
    {
        private void OnRenderHackAndSlash(ActionBase.ActionEvaluation<ActionBase> eval)
        {
            switch (eval.Action)
            {
                case HackAndSlash hackAndSlash:
                    ResponseHackAndSlash(eval, hackAndSlash);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(eval));
            }
        }

        private void ResponseHackAndSlash(
            ActionBase.ActionEvaluation<ActionBase> eval,
            HackAndSlash hackAndSlash)
        {
            if (eval.Exception is null)
            {
                if (!ActionManager.IsLastBattleActionId(hackAndSlash.Id))
                {
                    return;
                }

                _disposableForBattleEnd?.Dispose();
                _disposableForBattleEnd =
                    ObservableExtensions.Subscribe(Game.Game.instance.Stage.onEnterToStageEnd
                            .First(), _ =>
                        {
                            var task = UniTask.Run(() =>
                            {
                                UpdateCurrentAvatarStateAsync(eval).Forget();
                                UpdateCrystalRandomSkillState(eval);
                                var avatarState = States.Instance.CurrentAvatarState;
                                RenderQuest(hackAndSlash.AvatarAddress,
                                    avatarState.questList.completedQuestIds);
                                _disposableForBattleEnd = null;
                                Game.Game.instance.Stage.IsAvatarStateUpdatedAfterBattle = true;
                            });
                            task.ToObservable()
                                .First()
                                // ReSharper disable once ConvertClosureToMethodGroup
                                .DoOnError(e => Debug.LogException(e));
                        });

                var tableSheets = TableSheets.Instance;
                var skillsOnWaveStart = new List<Model.Skill.Skill>();
                if (hackAndSlash.StageBuffId.HasValue)
                {
                    var skill = CrystalRandomSkillState.GetSkill(
                        hackAndSlash.StageBuffId.Value,
                        tableSheets.CrystalRandomBuffSheet,
                        tableSheets.SkillSheet);
                    skillsOnWaveStart.Add(skill);
                }

                var resultModel = hackAndSlash.GetHackAndSlashReward(
                    eval.BlockIndex,
                    eval.RandomSeed,
                    States.Instance.CurrentAvatarState,
                    skillsOnWaveStart,
                    tableSheets,
                    out var simulator);
                var log = simulator.Log;
                Game.Game.instance.Stage.PlayCount = hackAndSlash.PlayCount;
                Game.Game.instance.Stage.StageType = StageType.HackAndSlash;
                if (hackAndSlash.PlayCount > 1)
                {
                    Widget.Find<BattleResultPopup>().ModelForMultiHackAndSlash = resultModel;
                }

                if (hackAndSlash.StageBuffId.HasValue)
                {
                    Analyzer.Instance.Track("Unity/Use Crystal Bonus Skill",
                        new Dictionary<string, Value>()
                        {
                            ["RandomSkillId"] = hackAndSlash.StageBuffId,
                            ["IsCleared"] = simulator.Log.IsClear,
                            ["AvatarAddress"] =
                                States.Instance.CurrentAvatarState.address.ToString(),
                            ["AgentAddress"] = States.Instance.AgentState.address.ToString(),
                        });
                }

                if (Widget.Find<LoadingScreen>().IsActive())
                {
                    if (Widget.Find<BattlePreparation>().IsActive())
                    {
                        Widget.Find<BattlePreparation>().GoToStage(log);
                    }
                    else if (Widget.Find<Menu>().IsActive())
                    {
                        Widget.Find<Menu>().GoToStage(log);
                    }
                }
                else if (Widget.Find<StageLoadingEffect>().IsActive() &&
                         Widget.Find<BattleResultPopup>().IsActive())
                {
                    Widget.Find<BattleResultPopup>().NextStage(log);
                }
            }
            else
            {
                var showLoadingScreen = false;
                if (Widget.Find<StageLoadingEffect>().IsActive())
                {
                    Widget.Find<StageLoadingEffect>().Close();
                }

                if (Widget.Find<BattleResultPopup>().IsActive())
                {
                    showLoadingScreen = true;
                    Widget.Find<BattleResultPopup>().Close();
                }

                Game.Game.BackToMainAsync(eval.Exception.InnerException, showLoadingScreen)
                    .Forget();
            }
        }
    }
}
