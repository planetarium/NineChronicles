using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Action;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Factory;
using Nekoyume.Helper;
using Nekoyume.L10n;
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

    public class EnhancementResultPopup : PopupWidget
    {
        [Serializable]
        public class ResultItem
        {
            public SimpleItemView itemView;
            public TextMeshProUGUI itemNameText;
            public TextMeshProUGUI beforeGradeText;
            public TextMeshProUGUI afterGradeText;
            public TextMeshProUGUI cpText;
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
        private GameObject _titleFailSuccessObject;

        [SerializeField]
        private GameObject _titleSuccessObject;

        [SerializeField]
        private GameObject _titleGreatSuccessObject;

        [SerializeField]
        private ResultItem _resultItem;

        [SerializeField]
        private ItemOptionView _itemMainStatView;

        [SerializeField]
        private List<ItemOptionWithCountView> _itemStatOptionViews;

        [SerializeField]
        private List<ItemOptionView> _itemSkillOptionViews;

        [SerializeField]
        private float _delayTimeOfShowOptions;

        [SerializeField]
        private float _intervalTimeOfShowOptions;

        [SerializeField]
        private GameObject legacyFailText;

        [SerializeField]
        private GameObject gainCrystalObject;

        [SerializeField]
        private TMP_Text gainCrystalText;

        [SerializeField]
        private RectTransform crystalIconTransform;

#if UNITY_EDITOR
        [Space(10)]
        [Header("Editor Properties For Test")]
        [Space(10)]
        [SerializeField]
        private ItemEnhancement13.EnhancementResult _editorEnhancementResult;

        [SerializeField]
        private List<EditorStatOption> _editorStatOptions;

        [SerializeField]
        private List<EditorSkillOption> _editorSkillOptions;
#endif

        private static readonly int AnimatorHashGreatSuccess = Animator.StringToHash("GreatSuccess");
        private static readonly int AnimatorHashSuccess = Animator.StringToHash("Success");
        private static readonly int AnimatorHashFail = Animator.StringToHash("Fail");
        private static readonly int AnimatorHashLoop = Animator.StringToHash("Loop");
        private static readonly int AnimatorHashLoopFail = Animator.StringToHash("Loop_Fail");
        private static readonly int AnimatorHashClose = Animator.StringToHash("Close");

        private IDisposable _disposableOfSkip;
        private Coroutine _coroutineOfPlayOptionAnimation;

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
            var preEquipment = (Equipment)ItemFactory.CreateItemUsable(equipmentRow, Guid.NewGuid(), 0, preLevel);
            var equipment = (Equipment)ItemFactory.CreateItemUsable(equipmentRow, Guid.NewGuid(), 0, preLevel + 1);
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
                var skill = SkillFactory.GetV1(
                    skillRow,
                    skillOption.totalPower - skillOption.plusPower,
                    skillOption.totalChance - skillOption.plusChance);
                preEquipment.Skills.Add(skill);
                preEquipment.optionCountFromCombination++;

                skill = SkillFactory.GetV1(
                    skillRow,
                    skillOption.totalPower,
                    skillOption.totalChance);
                equipment.Skills.Add(skill);
                equipment.optionCountFromCombination++;
            }

            Show(_editorEnhancementResult, preEquipment, equipment);
        }
#endif

        [Obsolete("Use `Show(ItemEnhanceMail mail)` instead.")]
        public override void Show(bool ignoreShowAnimation = false)
        {
            // ignore.
        }

        public void Show(ItemEnhanceMail mail)
        {
            if (!(mail.attachment is ItemEnhancement13.ResultModel result))
            {
                NcDebug.LogError("mail.attachment is not ItemEnhancement.ResultModel");
                return;
            }

            if (!(result.preItemUsable is Equipment preEquipment))
            {
                NcDebug.LogError("result.preItemUsable is not Equipment");
                return;
            }

            if (!(result.itemUsable is Equipment equipment))
            {
                NcDebug.LogError("result.itemUsable is not Equipment");
                return;
            }

            Show(result.enhancementResult, preEquipment, equipment, result.CRYSTAL.GetQuantityString());
        }

        public void Show(
            ItemEnhancement13.EnhancementResult enhancementResult,
            Equipment preEquipment,
            Equipment equipment,
            string crystal = "0")
        {
            if (preEquipment is null)
            {
                NcDebug.LogError("preEquipment is null");
                return;
            }

            if (equipment is null)
            {
                NcDebug.LogError("equipment is null");
                return;
            }

            var itemOptionInfoPre = new ItemOptionInfo(preEquipment);
            var itemOptionInfo = new ItemOptionInfo(equipment);

            _resultItem.itemView.SetData(new CountableItem(equipment, 1));
            _resultItem.beforeGradeText.text = $"+{equipment.level - 1}";
            _resultItem.afterGradeText.text = $"+{equipment.level}";
            _resultItem.itemNameText.text = equipment.GetLocalizedName(false, true);
            _resultItem.cpText.text = $"CP {itemOptionInfo.CP}";

            var (_, _, mainStatTotalValuePre) = itemOptionInfoPre.MainStat;
            var (mainStatType, _, mainStatTotalValue) = itemOptionInfo.MainStat;
            _itemMainStatView.UpdateViewAsTotalAndPlusStat(
                mainStatType,
                mainStatTotalValue,
                mainStatTotalValue - mainStatTotalValuePre);

            var statOptions = itemOptionInfo.StatOptions;
            var statOptionsCount = statOptions.Count;
            for (var i = 0; i < _itemStatOptionViews.Count; i++)
            {
                var optionView = _itemStatOptionViews[i];
                optionView.Hide(true);
                if (i >= statOptionsCount)
                {
                    optionView.UpdateToEmpty();
                    continue;
                }

                var (_, preValue, _) = itemOptionInfoPre.StatOptions[i];
                var (statType, value, count) = statOptions[i];
                optionView.UpdateAsTotalAndPlusStatWithCount(statType, value, value - preValue, count);
            }

            var skillOptions = itemOptionInfo.SkillOptions;
            var skillOptionsCount = skillOptions.Count;
            for (var i = 0; i < _itemSkillOptionViews.Count; i++)
            {
                var optionView = _itemSkillOptionViews[i];
                optionView.Hide(true);
                if (i >= skillOptionsCount)
                {
                    optionView.UpdateToEmpty();
                    continue;
                }

                var (_, prePower, preChance, preRatio, _) = itemOptionInfoPre.SkillOptions[i];
                var (skillRow, power, chance, ratio, type) = skillOptions[i];
                var powerText = SkillExtensions.EffectToString(skillRow.Id, skillRow.SkillType, power, ratio, type);
                var plusPowerText = SkillExtensions.EffectToString(
                    skillRow.Id,
                    skillRow.SkillType,
                    power - prePower,
                    ratio - preRatio,
                    type);
                optionView.UpdateAsTotalAndPlusSkill(
                    skillRow.GetLocalizedName(),
                    powerText,
                    chance,
                    power - prePower,
                    ratio - preRatio,
                    chance - preChance,
                    plusPowerText);
            }

            // NOTE: Ignore Show Animation
            base.Show(true);
            switch (enhancementResult)
            {
/*                case ItemEnhancement.EnhancementResult.GreatSuccess:
                    _titleSuccessObject.SetActive(false);
                    _titleGreatSuccessObject.SetActive(true);
                    _titleFailSuccessObject.SetActive(false);
                    Animator.SetTrigger(AnimatorHashGreatSuccess);
                    break;*/
                case ItemEnhancement13.EnhancementResult.Success:
                    _titleSuccessObject.SetActive(true);
                    _titleGreatSuccessObject.SetActive(false);
                    _titleFailSuccessObject.SetActive(false);
                    Animator.SetTrigger(AnimatorHashSuccess);
                    break;
/*                case ItemEnhancement.EnhancementResult.Fail:
                    _titleSuccessObject.SetActive(false);
                    _titleGreatSuccessObject.SetActive(false);
                    _titleFailSuccessObject.SetActive(true);
                    var gainCrystal = !crystal.Equals("0");
                    gainCrystalText.text = $"{crystal} {L10nManager.Localize("OBTAIN")}";
                    gainCrystalObject.SetActive(gainCrystal);
                    legacyFailText.SetActive(!gainCrystal);
                    Animator.SetTrigger(AnimatorHashFail);
                    break;*/
                default:
                    throw new ArgumentOutOfRangeException(nameof(enhancementResult), enhancementResult, null);
            }
        }

        #region Invoke from Animation

        public void OnAnimatorStateBeginning(string stateName)
        {
            switch (stateName)
            {
                case "Show":
                case "GreatSuccess":
                case "Success":
                case "Fail":
                    _disposableOfSkip ??= Observable.EveryUpdate()
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
            switch (stateName)
            {
                case "Close":
                    if (_titleFailSuccessObject.activeSelf)
                    {
                        StartCoroutine(
                            ItemMoveAnimationFactory.CoItemMoveAnimation(
                                ItemMoveAnimationFactory.AnimationItemType.Crystal,
                                crystalIconTransform.GetWorldPositionOfCenter(),
                                Find<HeaderMenuStatic>().Crystal.IconPosition +
                                GrindModule.CrystalMovePositionOffset,
                                1));
                    }

                    base.Close(true);
                    break;
            }
        }

        public void OnRequestPlaySFX(string sfxCode) =>
            AudioController.instance.PlaySfx(sfxCode);

        public void PlayOptionAnimation()
        {
            if (_coroutineOfPlayOptionAnimation != null)
            {
                StopCoroutine(_coroutineOfPlayOptionAnimation);
            }

            _coroutineOfPlayOptionAnimation = StartCoroutine(CoPlayOptionAnimation());
        }

        #endregion

        private void SkipAnimation()
        {
            if (_disposableOfSkip != null)
            {
                _disposableOfSkip.Dispose();
                _disposableOfSkip = null;
            }

            if (_coroutineOfPlayOptionAnimation != null)
            {
                StopCoroutine(_coroutineOfPlayOptionAnimation);
                _coroutineOfPlayOptionAnimation = null;
            }

            var animatorStateInfo = Animator.GetCurrentAnimatorStateInfo(0);
            if (animatorStateInfo.shortNameHash == AnimatorHashGreatSuccess ||
                animatorStateInfo.shortNameHash == AnimatorHashSuccess)
            {
                Animator.Play(AnimatorHashLoop, 0, 0);
            }
            else if (animatorStateInfo.shortNameHash == AnimatorHashFail)
            {
                Animator.Play(AnimatorHashLoopFail, 0, 0);
            }

            for (var i = 0; i < _itemStatOptionViews.Count; i++)
            {
                var optionView = _itemStatOptionViews[i];
                if (optionView.IsEmpty)
                {
                    continue;
                }

                optionView.Show(true);
            }

            for (var i = 0; i < _itemSkillOptionViews.Count; i++)
            {
                var optionView = _itemSkillOptionViews[i];
                if (optionView.IsEmpty)
                {
                    continue;
                }

                optionView.Show(true);
            }

            PressToContinue();
        }

        private IEnumerator CoPlayOptionAnimation()
        {
            yield return new WaitForSeconds(_delayTimeOfShowOptions);

            for (var i = 0; i < _itemStatOptionViews.Count; i++)
            {
                var optionView = _itemStatOptionViews[i];
                if (optionView.IsEmpty)
                {
                    continue;
                }

                yield return new WaitForSeconds(_intervalTimeOfShowOptions);
                optionView.Show();
            }

            for (var i = 0; i < _itemSkillOptionViews.Count; i++)
            {
                var optionView = _itemSkillOptionViews[i];
                if (optionView.IsEmpty)
                {
                    continue;
                }

                yield return new WaitForSeconds(_intervalTimeOfShowOptions);
                optionView.Show();
            }

            yield return null;

            _coroutineOfPlayOptionAnimation = null;

            if (_disposableOfSkip != null)
            {
                _disposableOfSkip.Dispose();
                _disposableOfSkip = null;
            }

            PressToContinue();
        }

        private void PressToContinue() => Observable.EveryUpdate()
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
    }
}
