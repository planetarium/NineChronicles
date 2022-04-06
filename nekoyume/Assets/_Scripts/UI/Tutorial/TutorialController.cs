using System.Collections.Generic;
using System.Linq;
using mixpanel;
using Nekoyume.L10n;
using UnityEngine;

namespace Nekoyume.UI
{
    public class TutorialController
    {
        private readonly Dictionary<TutorialTargetType, RectTransform> _targets =
            new Dictionary<TutorialTargetType, RectTransform>(new TutorialTargetTypeComparer());

        private readonly Dictionary<TutorialActionType, TutorialAction> _actions =
            new Dictionary<TutorialActionType, TutorialAction>(new TutorialActionTypeComparer());

        private readonly List<Preset> _preset = new List<Preset>();
        private readonly List<Scenario> _scenario = new List<Scenario>();

        private readonly Tutorial _tutorial;
        private readonly RectTransform _buttonRectTransform;

        private const string ScenarioPath = "Tutorial/Data/TutorialScenario";
        private const string PresetPath = "Tutorial/Data/TutorialPreset";
        private const string CheckPoint = "Tutorial_Check_Point";

        private readonly List<int> _mixpanelTargets = new List<int>() { 1, 2, 6, 11, 49 };

        public bool IsPlaying => _tutorial.IsActive();

        public bool IsCompleted
        {
            get
            {
                var worldInfo = Game.Game.instance.States.CurrentAvatarState.worldInformation;
                if (worldInfo is null) return false;
                var clearedStageId = worldInfo.TryGetLastClearedStageId(out var id) ? id : 1;
                if (GetCheckPoint(clearedStageId) != 0) return false;

                return !IsPlaying;
            }
        }

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
                    _targets.Add(target.type, target.rectTransform);
                }

                foreach (var action in widget.tutorialActions)
                {
                    var type = widget.GetType();
                    var methodInfo = type.GetMethod(action.ToString());
                    if (methodInfo != null)
                    {
                        _actions.Add(action, new TutorialAction(widget, methodInfo));
                    }
                }
            }

            _scenario.AddRange(GetData<TutorialScenario>(ScenarioPath).scenario);
            _preset.AddRange(GetData<TutorialPreset>(PresetPath).preset);
        }

        public void Run(int clearedStageId)
        {
            if (clearedStageId < GameConfig.RequireClearedStageLevel.CombinationEquipmentAction)
            {
                Play(1);
            }
            else
            {
                var checkPoint = GetCheckPoint(clearedStageId);
                if (checkPoint == 0)
                {
                    return;
                }

                Play(checkPoint);
            }
        }

        private void Play(int id)
        {
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
                var viewData = GetTutorialData(scenario.data);
                _tutorial.Play(viewData, scenario.data.presetId, () =>
                {
                    PlayAction(scenario.data.actionType);
                    Play(scenario.nextId);
                });
            }
            else
            {
                _tutorial.Stop(() =>
                {
                    _tutorial.gameObject.SetActive(false);
                    WidgetHandler.Instance.IsActiveTutorialMaskWidget = false;
                });
                HelpTooltip.HelpMe(100001, true);
            }
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
                    (DialogCommaType) preset.commaId,
                    script,
                    target)
            };
        }

        private static T GetData<T>(string path) where T : new()
        {
            var json = Resources.Load<TextAsset>(path).ToString();
            var data = JsonUtility.FromJson<T>(json);
            return data;
        }

        public static int GetCheckPoint(int clearedStageId)
        {
            if(PlayerPrefs.HasKey(CheckPoint))
            {
                return PlayerPrefs.GetInt(CheckPoint);
            }

            //If PlayerPrefs doesn't exist
            var value = 0;
            if (clearedStageId < GameConfig.RequireClearedStageLevel.CombinationEquipmentAction)
            {
                value = 1;
            }
            else if (clearedStageId == GameConfig.RequireClearedStageLevel.CombinationEquipmentAction)
            {
                value = 2;
            }
            return PlayerPrefs.GetInt(CheckPoint, value);
        }

        private static void SetCheckPoint(int id)
        {
            PlayerPrefs.SetInt(CheckPoint, id);
        }

        private void SendMixPanel(int id)
        {
            if (!_mixpanelTargets.Exists(x => x == id))
            {
                return;
            }

            var props = new Value
            {
                ["Id"] = id,
            };
            Analyzer.Instance.Track("Unity/Tutorial progress", props);
        }
    }
}
