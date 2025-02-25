using System.Collections;
using System.Collections.Generic;
using System.Linq;
using mixpanel;
using Nekoyume.Blockchain;
using Nekoyume.Game;
using Nekoyume.Game.LiveAsset;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume.UI
{
    public class TutorialController
    {
        private readonly Dictionary<TutorialTargetType, RectTransform> _targets = new(new TutorialTargetTypeComparer());

        private readonly Dictionary<TutorialActionType, TutorialAction> _actions = new(new TutorialActionTypeComparer());

        private readonly List<Preset> _preset = new();
        private readonly List<Scenario> _scenario = new();

        private readonly Tutorial _tutorial;
        private readonly RectTransform _buttonRectTransform;

        private const string ScenarioPath = "Tutorial/Data/TutorialScenario";
        private const string PresetPath = "Tutorial/Data/TutorialPreset";
        private const int CreateAvatarRewardTutorialId = 50000;
        private static string CheckPointKey => $"Tutorial_Check_Point_{Game.Game.instance.States.CurrentAvatarKey}";

        // 튜토리얼이 실행될 스테이지를 넣는 배열입니다. 배열 요소에 해당하는 스테이지를 클리어하고나면 실행됩니다.
        public static readonly int[] TutorialStageArray = { 5, 7, 10, 15, 23, 35, 40, 45, 49, 151, 301, };

        public bool IsPlaying => _tutorial.IsActive();

        public int LastPlayedTutorialId { get; private set; }
        private Coroutine _rewardScreenCoroutine;

        public TutorialController(IEnumerable<Widget> widgets)
        {
            foreach (var widget in widgets)
            {
                if (widget is Tutorial tutorial)
                {
                    _tutorial = tutorial;
                    _buttonRectTransform = tutorial.NextButton.GetComponent<RectTransform>();
                    continue;
                }

                foreach (var target in widget.tutorialTargets.Where(target => target != null))
                {
                    if (_targets.ContainsKey(target.type))
                    {
                        NcDebug.LogError($"Duplication Tutorial Targets AlreadyRegisterd : {_targets[target.type].gameObject.name}  TryRegisterd : {target.rectTransform.gameObject.name}");
                        continue;
                    }

                    _targets.Add(target.type, target.rectTransform);
                }

                foreach (var action in widget.tutorialActions)
                {
                    var type = widget.GetType();
                    var methodInfo = type.GetMethod(action.ToString());
                    if (methodInfo != null)
                    {
                        if (_actions.ContainsKey(action))
                        {
                            NcDebug.LogError($"Duplication Tutorial {action} Action AlreadyRegisterd : {_actions[action].ActionWidget.name}  TryRegisterd : {widget.name}");
                            continue;
                        }

                        _actions.Add(action, new TutorialAction(widget, methodInfo));
                    }
                }
            }

            _scenario.AddRange(GetData<TutorialScenarioScriptableObject>(ScenarioPath).tutorialScenario.scenario);
            _preset.AddRange(GetData<TutorialPresetScriptableObject>(PresetPath).tutorialPreset.preset);
        }

        public void Run(int clearedStageId)
        {
            var checkPoint = GetCheckPoint(clearedStageId);
            if (checkPoint > 0)
            {
                Play(checkPoint);
            }
        }

        public void Play(int id)
        {
            NcDebug.Log($"[TutorialController] Play({id})");
            if (!_tutorial.isActiveAndEnabled)
            {
                _tutorial.Show(true);
                WidgetHandler.Instance.IsActiveTutorialMaskWidget = true;
            }

            var scenario = _scenario.FirstOrDefault(x => x.id == id);
            if (scenario != null)
            {
                SendMixPanel(id);
                SetCheckPoint(scenario.checkPointId);
                LastPlayedTutorialId = id;
                var viewData = GetTutorialData(scenario.data);
                _tutorial.Play(viewData, scenario.data.presetId, scenario.data.guideSprite, () =>
                {
                    PlayAction(scenario.data.actionType);
                    Play(scenario.nextId);
                });

                if (id == CreateAvatarRewardTutorialId && _rewardScreenCoroutine == null)
                {
                    _rewardScreenCoroutine = _tutorial.StartCoroutine(CoShowTutorialRewardScreen());
                }
            }
            else
            {
                _tutorial.Stop(() =>
                {
                    _tutorial.gameObject.SetActive(false);
                    WidgetHandler.Instance.IsActiveTutorialMaskWidget = false;
                });
            }
        }

        public void Skip(int tutorialId)
        {
            var id = tutorialId;
            while (id != 0)
            {
                var nowScenario = _scenario.FirstOrDefault(scenario => scenario.id == id);
                if (nowScenario is null)
                {
                    break;
                }

                id = nowScenario.nextId;
                if (id == 0)
                {
                    var checkPointId = nowScenario.checkPointId;
                    SetCheckPoint(checkPointId);
                    break;
                }
            }

            _tutorial.Stop(() =>
            {
                _tutorial.gameObject.SetActive(false);
                WidgetHandler.Instance.IsActiveTutorialMaskWidget = false;
            });
        }

        // force-set tutorial target. not recommend.
        public void SetTutorialTarget(TutorialTarget target)
        {
            _targets[target.type] = target.rectTransform;
        }

        private void PlayAction(TutorialActionType actionType)
        {
            if (_actions.ContainsKey(actionType))
            {
                _actions[actionType].ActionMethodInfo
                    ?.Invoke(_actions[actionType].ActionWidget, null);
            }
        }

        private List<ITutorialData> GetTutorialData(ScenarioData data)
        {
            var preset = _preset.First(x => x.id == data.presetId);
            var target = _targets.ContainsKey(data.targetType) ? _targets[data.targetType] : null;
            var scriptKey = data.scriptKey;
            var script = L10nManager.Localize(scriptKey);

            return new List<ITutorialData>()
            {
                new GuideBackgroundData(
                    preset.isExistFadeInBackground,
                    preset.isEnableMask,
                    target,
                    _buttonRectTransform,
                    data.fullScreenButton,
                    data.buttonRaycastPadding,
                    data.targetPositionOffset,
                    data.targetSizeOffset),
                new GuideArrowData(
                    data.noArrow ? GuideType.Stop : data.guideType,
                    target,
                    data.targetPositionOffset,
                    data.targetSizeOffset,
                    data.arrowPositionOffset,
                    data.arrowAdditionalDelay,
                    preset.isSkipArrowAnimation),
                new GuideDialogData(
                    data.emojiType,
                    (DialogCommaType)preset.commaId,
                    data.dialogPositionType,
                    script,
                    target)
            };
        }

        private static T GetData<T>(string path) where T : ScriptableObject
        {
            return Resources.Load<T>(path);
        }

        private static int GetCheckPoint(int clearedStageId)
        {
            /*
             * check point = 0 : not playing tutorial
             * check point > 0 : already playing tutorial
             * check point < 0 : ended tutorial per stage, not playing tutorial
             *
             * when playing tutorial, check point = tutorial id
             * when ended tutorial, check point = stage id * -1 (in TutorialScenario, "checkPointId")
             */

            var checkPoint = PlayerPrefs.GetInt(CheckPointKey, 0);
            if (checkPoint > 0)
            {
                if (checkPoint is 100000 or 101000)
                {
                    // if check point is in patrol reward tutorial, play patrol reward tutorial for event condition
                    return !LiveAssetManager.instance.EventRewardPopupData.HasEvent
                        ? 100000
                        : 101000;
                }

                return checkPoint;
            }

            // format example
            void Check(int stageIdForTutorial) // ex) 5, 10
            {
                // clearedStageId == stageIdForTutorial => 튜토리얼이 실행되어야 하는 스테이지
                // 튜토리얼이 종료된 후 checkPoint = -stageIdForTutorial 연산을 함
                // checkPoint != -stageIdForTutorial => 해당 스테이지의 튜토리얼이 종료된적 없음
                if (clearedStageId == stageIdForTutorial && checkPoint != -stageIdForTutorial)
                {
                    checkPoint = stageIdForTutorial * 10000;
                }
            }

            if (TutorialStageArray.Any(stageId => stageId == clearedStageId))
            {
                if (Game.LiveAsset.GameConfig.IsKoreanBuild && clearedStageId == 7)
                {
                    // Skip tutorial 7 (portal reward) in K version
                }
                else if (clearedStageId == 10 &&
                    LiveAssetManager.instance.EventRewardPopupData.HasEvent)
                {
                    // if there is event reward data, play other patrol reward tutorial for event
                    checkPoint = 101000;
                }
                else
                {
                    Check(clearedStageId);
                }
            }
            // If PlayerPrefs doesn't exist
            else if (clearedStageId == 0 && checkPoint != -1)
            {
                checkPoint = 1;
            }

            return checkPoint;
        }

        private static void SetCheckPoint(int id)
        {
            PlayerPrefs.SetInt(CheckPointKey, id);
        }

        private void SendMixPanel(int id)
        {
            // tutorial start point (id <= 2 : before stage 5)
            // in playing stage3 tutorial, id 30000 is played. (duplicate tutorial)
            if (TutorialStageArray.All(x => x * 10000 != id) || id <= 2 || id != 30000)
            {
                return;
            }

            var props = new Dictionary<string, Value>()
            {
                ["Id"] = id
            };
            Analyzer.Instance.Track("Unity/Tutorial progress", props);
        }

        private IEnumerator CoShowTutorialRewardScreen()
        {
            yield return new WaitUntil(() => Widget.Find<LobbyMenu>().IsShown);

            var mailRewards = new List<MailReward>();
            foreach (var row in TableSheets.Instance.CreateAvatarItemSheet.Values)
            {
                var itemId = row.ItemId;
                var count = row.Count;
                var itemRow = TableSheets.Instance.ItemSheet[itemId];
                if (itemRow is MaterialItemSheet.Row materialRow)
                {
                    var item = ItemFactory.CreateMaterial(materialRow);
                    mailRewards.Add(new MailReward(item, count));
                }
                else
                {
                    for (var i = 0; i < count; i++)
                    {
                        // the Aura item will appear in summon tutorial
                        if (itemRow.ItemSubType != ItemSubType.Aura)
                        {
                            var item = ItemFactory.CreateItem(itemRow, new ActionRenderHandler.LocalRandom(0));
                            mailRewards.Add(new MailReward(item, 1));
                        }
                    }
                }
            }

            mailRewards.AddRange(
                TableSheets.Instance.CreateAvatarFavSheet.Values
                    .Select(row =>
                        new MailReward(row.Currency * row.Quantity, row.Quantity)));
            Widget.Find<RewardScreen>().Show(mailRewards, "UI_TUTORIAL_5_CLAIM_REWARD");
        }

        public void RegisterWidget(Widget widget)
        {
            foreach (var target in widget.tutorialTargets.Where(target => target != null))
            {
                if (_targets.ContainsKey(target.type))
                {
                    NcDebug.LogError($"Duplication Tutorial Targets AlreadyRegisterd : {_targets[target.type].gameObject.name}  TryRegisterd : {target.rectTransform.gameObject.name}");
                    continue;
                }

                _targets.Add(target.type, target.rectTransform);
            }

            foreach (var action in widget.tutorialActions)
            {
                var type = widget.GetType();
                var methodInfo = type.GetMethod(action.ToString());
                if (methodInfo != null)
                {
                    if (_actions.ContainsKey(action))
                    {
                        NcDebug.LogError($"Duplication Tutorial {action} Action AlreadyRegisterd : {_actions[action].ActionWidget.name}  TryRegisterd : {widget.name}");
                        continue;
                    }

                    _actions.Add(action, new TutorialAction(widget, methodInfo));
                }
            }
        }

        public void UnregisterWidget(Widget widget)
        {
            foreach (var target in widget.tutorialTargets.Where(target => target != null))
            {
                _targets.Remove(target.type);
            }

            foreach (var action in widget.tutorialActions)
            {
                var type = widget.GetType();
                var methodInfo = type.GetMethod(action.ToString());
                if (methodInfo != null)
                {
                    _actions.Remove(action);
                }
            }
        }
    }
}
