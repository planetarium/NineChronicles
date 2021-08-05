using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nekoyume.UI
{
    using UniRx;

    public class EnhancementResult : Widget
    {
        [Serializable]
        public class ResultItem
        {
            public SimpleCountableItemView itemView;
            public TextMeshProUGUI beforeGradeText;
            public TextMeshProUGUI afterGradeText;
            public TextMeshProUGUI itemNameText;
            public TextMeshProUGUI cpText;
        }

        [Serializable]
        public class Option
        {
            public GameObject rootObject;
            public TextMeshProUGUI totalText;
            public TextMeshProUGUI plusText;

            [CanBeNull]
            public GameObject secondStarObject;
        }

#if UNITY_EDITOR
        [Serializable]
        public class EditorStatOption
        {
            public StatType statType;
            public int totalValue;
            public int plusValue;
        }

        [Serializable]
        public class EditorSkillOption
        {
            public int totalChance;
            public int plusChance;
            public int totalPower;
            public int plusPower;
        }
#endif

        [SerializeField]
        private ResultItem _resultItem;

        [SerializeField]
        private Option _mainStat;

        [SerializeField]
        private List<Option> _optionTexts;

#if UNITY_EDITOR
        [Space(10)]
        [Header("Editor Properties For Test")]
        [Space(10)]
        [SerializeField]
        private ItemEnhancement.EnhancementResult _editorEnhancementResult;

        [SerializeField]
        private List<EditorStatOption> _editorStatOptions;

        [SerializeField]
        private List<EditorSkillOption> _editorSkillOptions;
#endif

        private static readonly int AnimatorHashGreatSuccess = Animator.StringToHash("GreatSuccess");
        private static readonly int AnimatorHashSuccess = Animator.StringToHash("Success");
        private static readonly int AnimatorHashFail = Animator.StringToHash("Fail");
        private static readonly int AnimatorHashLoop = Animator.StringToHash("Loop");
        private static readonly int AnimatorHashClose = Animator.StringToHash("Close");

        private IDisposable _disposableOfSkip;

        public override WidgetType WidgetType => WidgetType.Popup;

        protected override void OnDisable()
        {
            _disposableOfSkip?.Dispose();
            _disposableOfSkip = null;

            base.OnDisable();
        }

#if UNITY_EDITOR
        public void ShowWithEditorProperty()
        {
            var tableSheets = Game.Game.instance.TableSheets;
            var equipmentList = tableSheets.EquipmentItemSheet.OrderedList;
            var equipmentRow = equipmentList[Random.Range(0, equipmentList.Count)];
            var preLevel = Random.Range(0, 10);
            var preEquipment = (Equipment) ItemFactory.CreateItemUsable(equipmentRow, Guid.NewGuid(), 0, preLevel);
            var equipment = _editorEnhancementResult == ItemEnhancement.EnhancementResult.Fail
                ? (Equipment) ItemFactory.CreateItemUsable(equipmentRow, Guid.NewGuid(), 0, preLevel)
                : (Equipment) ItemFactory.CreateItemUsable(equipmentRow, Guid.NewGuid(), 0, preLevel + 1);
            foreach (var statOption in _editorStatOptions)
            {
                preEquipment.StatsMap.AddStatAdditionalValue(
                    statOption.statType,
                    statOption.totalValue - statOption.plusValue);
                preEquipment.optionCountFromCombination++;

                equipment.StatsMap.AddStatAdditionalValue(statOption.statType, statOption.totalValue);
                equipment.optionCountFromCombination++;
            }

            var skillList = tableSheets.SkillSheet.OrderedList;
            foreach (var skillOption in _editorSkillOptions)
            {
                var skillRow = skillList[Random.Range(0, skillList.Count)];
                var skill = SkillFactory.Get(
                    skillRow,
                    skillOption.totalPower - skillOption.plusPower,
                    skillOption.totalChance - skillOption.plusChance);
                preEquipment.Skills.Add(skill);
                preEquipment.optionCountFromCombination++;

                skill = SkillFactory.Get(
                    skillRow,
                    skillOption.totalPower,
                    skillOption.totalChance);
                equipment.Skills.Add(skill);
                equipment.optionCountFromCombination++;
            }

            Show(_editorEnhancementResult, preEquipment, equipment);
        }
#endif

        [Obsolete("Use `Show(Equipment equipment)` instead.")]
        public override void Show(bool ignoreShowAnimation = false)
        {
            // ignore.
        }

        public void Show(ItemEnhanceMail mail)
        {
            if (!(mail.attachment is ItemEnhancement.ResultModel result))
            {
                Debug.LogError("mail.attachment is not ItemEnhancement.ResultModel");
                return;
            }

            if (!(result.preItemUsable is Equipment preEquipment))
            {
                Debug.LogError("result.preItemUsable is not Equipment");
                return;
            }

            if (!(result.itemUsable is Equipment equipment))
            {
                Debug.LogError("result.itemUsable is not Equipment");
                return;
            }

            Show(ItemEnhancement.EnhancementResult.GreatSuccess, equipment, preEquipment);
        }

        public void Show(
            ItemEnhancement.EnhancementResult enhancementResult,
            Equipment preEquipment,
            Equipment equipment)
        {
            if (preEquipment is null)
            {
                Debug.LogError("preEquipment is null");
                return;
            }

            if (equipment is null)
            {
                Debug.LogError("equipment is null");
                return;
            }

            // NOTE: Ignore Show Animation
            base.Show(true);

            _resultItem.itemView.SetData(new CountableItem(equipment, 1));
            _resultItem.beforeGradeText.text = $"+{equipment.level - 1}";
            _resultItem.afterGradeText.text = $"+{equipment.level}";
            _resultItem.itemNameText.text = equipment.GetLocalizedName(
                useElementalIcon: false,
                ignoreLevel: true);
            _resultItem.cpText.text = equipment.GetCPText();

            var optionCount = equipment.optionCountFromCombination;
            var mainStatTotal = equipment.StatsMap.GetStat(equipment.UniqueStatType, true);
            _mainStat.totalText.text =
                $"{equipment.UniqueStatType.ToString()} {mainStatTotal}";
            _mainStat.plusText.text =
                $"(+{mainStatTotal - preEquipment.StatsMap.GetStat(preEquipment.UniqueStatType, true)})";

            var preAdditionalStats = preEquipment.StatsMap.GetAdditionalStats(true)
                .ToArray();
            var additionalStats = equipment.StatsMap.GetAdditionalStats(true)
                .ToArray();
            var additionalStatsLength = additionalStats.Length;

            var preSkills = preEquipment.Skills;
            var skills = equipment.Skills;
            var skillsCount = skills.Count;
            for (var i = 0; i < _optionTexts.Count; i++)
            {
                var optionText = _optionTexts[i];
                if (i < additionalStatsLength)
                {
                    if (i == 0 && optionText.secondStarObject != null)
                    {
                        optionText.secondStarObject.SetActive(
                            additionalStatsLength < optionCount - skillsCount);
                    }

                    var (statType, additionalValue) = additionalStats[i];
                    optionText.totalText.text = $"{statType.ToString()} {additionalValue}";

                    var (_, preAdditionalValue) = preAdditionalStats.First(tuple => tuple.statType == statType);
                    optionText.plusText.text = $"(+{additionalValue - preAdditionalValue})";
                    optionText.rootObject.SetActive(true);
                }
                else if (i < additionalStatsLength + skillsCount)
                {
                    var skill = skills[i - additionalStatsLength];
                    optionText.totalText.text = $"{skill.SkillRow.GetLocalizedName()} {skill.Power} / {skill.Chance}%";

                    var preSkill = preSkills[i - additionalStatsLength];
                    optionText.plusText.text =
                        $"(+{skill.Power - preSkill.Power} / +{skill.Chance - preSkill.Chance}%)";
                    optionText.rootObject.SetActive(true);
                }
                else
                {
                    optionText.rootObject.SetActive(false);
                }
            }

            switch (enhancementResult)
            {
                case ItemEnhancement.EnhancementResult.GreatSuccess:
                    Animator.SetTrigger(AnimatorHashGreatSuccess);
                    break;
                case ItemEnhancement.EnhancementResult.Success:
                    Animator.SetTrigger(AnimatorHashSuccess);
                    break;
                case ItemEnhancement.EnhancementResult.Fail:
                    Animator.SetTrigger(AnimatorHashFail);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(enhancementResult), enhancementResult, null);
            }
        }

        #region Invoke from Animation

        public void OnAnimatorStateBeginning(string stateName)
        {
            Debug.Log("OnAnimatorStateBeginning: " + stateName);
            switch (stateName)
            {
                case "GreatSuccess":
                case "Success":
                case "Fail":
                    if (_disposableOfSkip != null)
                    {
                        _disposableOfSkip.Dispose();
                    }

                    _disposableOfSkip = Observable.EveryUpdate()
                        .Where(_ => Input.GetMouseButtonDown(0) ||
                                    Input.GetKeyDown(KeyCode.Return) ||
                                    Input.GetKeyDown(KeyCode.KeypadEnter) ||
                                    Input.GetKeyDown(KeyCode.Escape))
                        .Take(1)
                        .DoOnCompleted(() => _disposableOfSkip = null)
                        .Subscribe(_ =>
                        {
                            AudioController.PlayClick();
                            SkipAnimation();
                        });
                    break;
            }
        }

        public void OnAnimatorStateEnd(string stateName)
        {
            Debug.Log("OnAnimatorStateEnd: " + stateName);
            switch (stateName)
            {
                case "GreatSuccess":
                case "Success":
                case "Fail":
                    Observable.EveryUpdate()
                        .Where(_ => Input.GetMouseButtonDown(0) ||
                                    Input.GetKeyDown(KeyCode.Return) ||
                                    Input.GetKeyDown(KeyCode.KeypadEnter) ||
                                    Input.GetKeyDown(KeyCode.Escape))
                        .First()
                        .Subscribe(_ =>
                        {
                            AudioController.PlayClick();
                            Animator.SetTrigger(AnimatorHashClose);
                        });
                    break;
                case "Close":
                    base.Close(true);
                    break;
            }
        }

        public void OnRequestPlaySFX(string sfxCode)
        {
            AudioController.instance.PlaySfx(sfxCode);
        }

        #endregion

        private void SkipAnimation()
        {
            var hash = Animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            Animator.Play(hash, 0, 1f);
            if (hash == AnimatorHashGreatSuccess ||
                hash == AnimatorHashSuccess ||
                hash == AnimatorHashFail)
            {
                Observable.NextFrame().Subscribe(_ =>
                    Animator.Play(AnimatorHashLoop, 0, 0));
            }
        }
    }
}
