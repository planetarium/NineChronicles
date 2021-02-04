using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Nekoyume.L10n;
using Nekoyume.State;
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

        private const string ScenarioPath = "Tutorial/Data/TutorialScenario";
        private const string PresetPath = "Tutorial/Data/TutorialPreset";

        public TutorialController(IEnumerable<Widget> widgets)
        {
            foreach (var widget in widgets)
            {
                if (widget is Tutorial tutorial)
                {
                    _tutorial = tutorial;
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

        private T GetData<T>(string path) where T : new()
        {
            var data = Resources.Load<TextAsset>(path)?.text;
            return !string.IsNullOrEmpty(data) ? JsonSerializer.Deserialize<T>(data) : new T();
        }

        public void Play(int id)
        {
            if (!_tutorial.isActiveAndEnabled)
            {
                _tutorial.Show();
            }

            var scenario = _scenario.FirstOrDefault(x => x.id == id);
            if (scenario != null)
            {
                var viewData = GetTutorialData(scenario.data);
                _tutorial.Play(viewData, scenario.data.presetId, () =>
                {
                    PlayAction(scenario.data.actionType);
                    Play(scenario.nextId);
                });
                SaveTutorialProgress(id);
            }
            else
            {
                _tutorial.Stop(() => _tutorial.gameObject.SetActive(false));
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
            var script = L10nManager.TryLocalize(scriptKey, out var s) ? s : scriptKey;

            return new List<ITutorialData>()
            {
                new GuideBackgroundData(
                    preset.isExistFadeInBackground,
                    preset.isEnableMask,
                    target),
                new GuideArrowData(
                    data.guideType,
                    target,
                    data.targetPositionOffset,
                    data.targetSizeOffset,
                    preset.isSkipArrowAnimation),
                new GuideDialogData(
                    data.emojiType,
                    (DialogCommaType) preset.commaId,
                    script,
                    target,
                    _tutorial.NextButton)
            };
        }

        public int GetTutorialProgress()
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            var prefsKey = $"TUTORIAL_PROGRESS_{avatarAddress}";
            var progress = PlayerPrefs.GetInt(prefsKey, 0);
            return progress;
        }

        private void SaveTutorialProgress(int id)
        {
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            var prefsKey = $"TUTORIAL_PROGRESS_{avatarAddress}";
            PlayerPrefs.SetInt(prefsKey, id);
            Debug.LogWarning($"Saved tutorial progress : {id}");
        }
    }
}
