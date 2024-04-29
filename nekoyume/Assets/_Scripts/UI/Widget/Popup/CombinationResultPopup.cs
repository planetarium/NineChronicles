using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.UI.Module;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Nekoyume.UI
{
    using UniRx;

    public class CombinationResultPopup : PopupWidget
    {
        [Serializable]
        public struct ResultItem
        {
            public TextMeshProUGUI itemNameText;
            public VanillaItemView itemView;
            public TextMeshProUGUI mainStatText;
            public TextMeshProUGUI cpText;
        }

#if UNITY_EDITOR
        [Serializable]
        public enum EquipmentOrFood
        {
            Equipment,
            Food
        }

        [Serializable]
        public class EditorStatOption
        {
            public StatType statType;
            public int value;
        }

        [Serializable]
        public class EditorSkillOption
        {
            public int chance;
            public int power;
        }
#endif

        [SerializeField]
        private Image _iconImage;

        [SerializeField]
        private Sprite _equipmentIconSprite;

        [SerializeField]
        private Sprite _consumableIconSprite;

        [SerializeField]
        private GameObject _titleSuccessObject;

        [SerializeField]
        private GameObject _titleGreatSuccessObject;

        [SerializeField]
        private GameObject _titleFoodSuccessObject;

        [SerializeField]
        private ResultItem _resultItem;

        [SerializeField]
        private List<CoveredItemOptionView> _itemStatOptionViews;

        [SerializeField]
        private List<CoveredItemOptionView> _itemSkillOptionViews;

        [SerializeField]
        private List<ItemOptionIconView> _itemOptionIconViews;

        [SerializeField]
        private float _delayTimeOfShowOptions;

        [SerializeField]
        private float _intervalTimeOfDiscoverOptions;

        [SerializeField]
        private float _delayTimeOfIncreaseCPAnimation;

        [SerializeField]
        private float _dueTimeOfIncreaseCPAnimation;

#if UNITY_EDITOR
        [Space(10)]
        [Header("Editor Properties For Test")]
        [Space(10)]
        [SerializeField]
        private bool _isGreatSuccess;

        [SerializeField]
        private EquipmentOrFood _editorEquipmentOrFood;

        [SerializeField]
        private List<EditorStatOption> _editorStatOptions;

        [SerializeField]
        private List<EditorSkillOption> _editorSkillOptions;
#endif

        private static readonly int AnimatorHashGreatSuccess = Animator.StringToHash("GreatSuccess");
        private static readonly int AnimatorHashSuccess = Animator.StringToHash("Success");
        private static readonly int AnimatorHashLoop = Animator.StringToHash("Loop");
        private static readonly int AnimatorHashClose = Animator.StringToHash("Close");

        private ItemOptionInfo _itemOptionInfo;
        private readonly List<decimal> _cpListForAnimationSteps = new List<decimal>();
        private IDisposable _disposableOfSkip;
        private IDisposable _disposableOfCPAnimation;
        private Coroutine _coroutineOfPlayOptionAnimation;

        protected override void OnDisable()
        {
            _disposableOfSkip?.Dispose();
            _disposableOfSkip = null;

            _disposableOfCPAnimation?.Dispose();
            _disposableOfCPAnimation = null;

            base.OnDisable();
        }

#if UNITY_EDITOR
        public void ShowWithEditorProperty()
        {
            var tableSheets = Game.Game.instance.TableSheets;
            if (_editorEquipmentOrFood == EquipmentOrFood.Equipment)
            {
                var equipmentList = !_editorStatOptions.Any() || _editorStatOptions[0].statType == StatType.NONE
                    ? tableSheets.EquipmentItemSheet.OrderedList
                    : tableSheets.EquipmentItemSheet.OrderedList.Where(e =>
                        e.Stat.StatType == _editorStatOptions[0].statType).ToList();
                if (!equipmentList.Any())
                {
                    NcDebug.LogError($"{_editorStatOptions[0].statType} cannot be main stat type");
                    return;
                }

                var equipmentRow = equipmentList[Random.Range(0, equipmentList.Count)];
                var equipment = (Equipment)ItemFactory.CreateItemUsable(equipmentRow, Guid.NewGuid(), 0);
                foreach (var statOption in _editorStatOptions)
                {
                    equipment.StatsMap.AddStatAdditionalValue(statOption.statType, statOption.value);
                    equipment.optionCountFromCombination++;
                }

                var skillList = tableSheets.SkillSheet.OrderedList;
                foreach (var skillOption in _editorSkillOptions)
                {
                    var row = skillList[Random.Range(0, skillList.Count)];
                    var skill = SkillFactory.GetV1(row, skillOption.power, skillOption.chance);
                    equipment.Skills.Add(skill);
                    equipment.optionCountFromCombination++;
                }

                Show(equipment, _isGreatSuccess ? _editorStatOptions.Count + _editorSkillOptions.Count : 4);
            }
            else
            {
                var consumableList = !_editorStatOptions.Any() || _editorStatOptions[0].statType == StatType.NONE
                    ? tableSheets.ConsumableItemSheet.OrderedList
                    : tableSheets.ConsumableItemSheet.OrderedList.Where(e =>
                        e.Stats[0].StatType == _editorStatOptions[0].statType).ToList();
                if (!consumableList.Any())
                {
                    NcDebug.LogError($"{_editorStatOptions[0].statType} cannot be main stat type");
                    return;
                }

                var consumableRow = consumableList[Random.Range(0, consumableList.Count)];
                var consumable = (Consumable)ItemFactory.CreateItemUsable(consumableRow, Guid.NewGuid(), 0);
                foreach (var statOption in _editorStatOptions)
                {
                    consumable.StatsMap.AddStatValue(statOption.statType, statOption.value);
                }

                Show(consumable);
            }
        }
#endif

        [Obsolete("Use `Show(ItemUsable itemUsable)` instead.")]
        public override void Show(bool ignoreShowAnimation = false)
        {
            // ignore.
        }

        public void Show(ItemUsable itemUsable, int? subRecipeOptionCount = null)
        {
            if (itemUsable is null)
            {
                NcDebug.LogError($"{nameof(itemUsable)} is null");
                return;
            }

            _cpListForAnimationSteps.Clear();
            _resultItem.itemNameText.text = itemUsable.GetLocalizedName(false);
            _resultItem.itemView.SetData(itemUsable);
            _resultItem.mainStatText.text = string.Empty;
            _resultItem.cpText.text = string.Empty;

            _itemOptionInfo = itemUsable is Equipment equipment
                ? new ItemOptionInfo(equipment)
                : new ItemOptionInfo(itemUsable);

            var statOptions = _itemOptionInfo.StatOptions;
            var statOptionsCount = statOptions.Count;
            var statOptionsIconCount = statOptions.Sum(tuple => tuple.count);
            var skillOptions = _itemOptionInfo.SkillOptions;
            var skillOptionsCount = skillOptions.Count;
            for (var i = 0; i < _itemOptionIconViews.Count; i++)
            {
                if (i < statOptionsIconCount)
                {
                    _itemOptionIconViews[i].UpdateAsStat();
                    continue;
                }

                if (i < statOptionsIconCount + skillOptionsCount)
                {
                    _itemOptionIconViews[i].UpdateAsSkill();
                    continue;
                }

                _itemOptionIconViews[i].Hide(true);
            }

            for (var i = 0; i < _itemStatOptionViews.Count; i++)
            {
                var optionView = _itemStatOptionViews[i];
                optionView.Hide(true);
                if (i >= statOptionsCount)
                {
                    optionView.UpdateToEmpty();
                    continue;
                }

                var (type, value, count) = statOptions[i];
                optionView.UpdateAsStatWithCount(type, value, count);
            }

            for (var i = 0; i < _itemSkillOptionViews.Count; i++)
            {
                var optionView = _itemSkillOptionViews[i];
                optionView.Hide(true);
                if (i >= skillOptionsCount)
                {
                    optionView.UpdateToEmpty();
                    continue;
                }

                var (skillRow, power, chance, ratio, type) = skillOptions[i];
                var powerText = SkillExtensions.EffectToString(skillRow.Id, skillRow.SkillType, power, ratio, type);
                optionView.UpdateAsSkill(skillRow.GetLocalizedName(), powerText, chance);
            }

            if (itemUsable.ItemType == ItemType.Equipment)
            {
                PostShowAsEquipment(subRecipeOptionCount);
            }
            else
            {
                PostShowAsConsumable();
            }
        }

        private void PostShowAsConsumable()
        {
            _iconImage.overrideSprite = _consumableIconSprite;
            _titleSuccessObject.SetActive(false);
            _titleGreatSuccessObject.SetActive(false);
            _titleFoodSuccessObject.SetActive(true);

            // NOTE: Ignore Show Animation
            base.Show(true);
            Animator.SetTrigger(AnimatorHashSuccess);
        }

        private void PostShowAsEquipment(int? subRecipeOptionCount = null)
        {
            _iconImage.overrideSprite = _equipmentIconSprite;

            var (mainStatType, _, mainStatTotalValue) = _itemOptionInfo.MainStat;
            _resultItem.mainStatText.text = $"{mainStatType} {mainStatType.ValueToString(mainStatTotalValue)}";

            var statsCP = CPHelper.GetStatCP(mainStatType, mainStatTotalValue);
            _cpListForAnimationSteps.Add(statsCP);
            _resultItem.cpText.text = $"CP {CPHelper.DecimalToInt(statsCP)}";

            var statOptions = _itemOptionInfo.StatOptions;
            foreach (var (type, value, _) in statOptions)
            {
                // NOTE: Do not add a CP which is same type with mainStatType. Because statsCP already contains this amount.
                // But we should add statsCP to _cpListForAnimationSteps for animation.
                if (type != mainStatType)
                {
                    statsCP += CPHelper.GetStatCP(type, value);
                }

                _cpListForAnimationSteps.Add(statsCP);
            }

            var skillOptions = _itemOptionInfo.SkillOptions;
            for (var i = 0; i < skillOptions.Count; i++)
            {
                var multipliedCP = statsCP * CPHelper.GetSkillsMultiplier(i + 1);
                _cpListForAnimationSteps.Add(multipliedCP);
            }

            if (_itemOptionInfo.CP !=
                CPHelper.DecimalToInt(_cpListForAnimationSteps[_cpListForAnimationSteps.Count - 1]))
            {
                NcDebug.LogError(
                    $"Wrong CP!!!! {_itemOptionInfo.CP} != {_cpListForAnimationSteps[_cpListForAnimationSteps.Count - 1]}");
            }

            // NOTE: Ignore Show Animation
            base.Show(true);
            if (subRecipeOptionCount.HasValue &&
                _itemOptionInfo.OptionCountFromCombination == subRecipeOptionCount)
            {
                _titleSuccessObject.SetActive(false);
                _titleGreatSuccessObject.SetActive(true);
                _titleFoodSuccessObject.SetActive(false);
                Animator.SetTrigger(AnimatorHashGreatSuccess);
            }
            else
            {
                _titleSuccessObject.SetActive(true);
                _titleGreatSuccessObject.SetActive(false);
                _titleFoodSuccessObject.SetActive(false);
                Animator.SetTrigger(AnimatorHashSuccess);
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
                    base.Close(true);
                    break;
            }
        }

        public void OnRequestPlaySFX(string sfxCode) =>
            AudioController.instance.PlaySfx(sfxCode);

        public void ShowOptionIconsAll()
        {
            for (var i = 0; i < _itemOptionIconViews.Count; i++)
            {
                var view = _itemOptionIconViews[i];
                if (i < _itemOptionInfo.OptionCountFromCombination)
                {
                    view.Show();
                }
                else
                {
                    view.Hide();
                }
            }
        }

        public void HideOptionIcon(int index)
        {
            if (index < 0 || index >= _itemOptionIconViews.Count)
            {
                NcDebug.LogError($"Invalid argument: {nameof(index)}({index})");
            }

            if (index >= _itemOptionInfo.OptionCountFromCombination)
            {
                return;
            }

            _itemOptionIconViews[index].Hide();
        }

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

            if (_disposableOfCPAnimation != null)
            {
                _disposableOfCPAnimation.Dispose();
                _disposableOfCPAnimation = null;
            }

            if (_coroutineOfPlayOptionAnimation != null)
            {
                StopCoroutine(_coroutineOfPlayOptionAnimation);
                _coroutineOfPlayOptionAnimation = null;
            }

            Animator.Play(AnimatorHashLoop, 0, 0);

            for (var i = 0; i < _itemOptionIconViews.Count; i++)
            {
                _itemOptionIconViews[i].Hide(true);
            }

            for (var i = 0; i < _itemStatOptionViews.Count; i++)
            {
                var optionView = _itemStatOptionViews[i];
                if (optionView.IsEmpty)
                {
                    continue;
                }

                optionView.Discover(true);
            }

            for (var i = 0; i < _itemSkillOptionViews.Count; i++)
            {
                var optionView = _itemSkillOptionViews[i];
                if (optionView.IsEmpty)
                {
                    continue;
                }

                optionView.Discover(true);
            }

            if (_cpListForAnimationSteps.Any())
            {
                _resultItem.cpText.text =
                    $"CP {CPHelper.DecimalToInt(_cpListForAnimationSteps[_cpListForAnimationSteps.Count - 1])}";
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

                optionView.Show();
            }

            for (var i = 0; i < _itemSkillOptionViews.Count; i++)
            {
                var optionView = _itemSkillOptionViews[i];
                if (optionView.IsEmpty)
                {
                    continue;
                }

                optionView.Show();
            }

            var step = 0;
            for (var i = 0; i < _itemStatOptionViews.Count; i++)
            {
                var optionView = _itemStatOptionViews[i];
                if (optionView.IsEmpty)
                {
                    continue;
                }

                yield return new WaitForSeconds(_intervalTimeOfDiscoverOptions);
                optionView.Discover();
                yield return new WaitForSeconds(_delayTimeOfIncreaseCPAnimation);
                PlayCPAnimation(step++);
            }

            for (var i = 0; i < _itemSkillOptionViews.Count; i++)
            {
                var optionView = _itemSkillOptionViews[i];
                if (optionView.IsEmpty)
                {
                    continue;
                }

                yield return new WaitForSeconds(_intervalTimeOfDiscoverOptions);
                optionView.Discover();
                yield return new WaitForSeconds(_delayTimeOfIncreaseCPAnimation);
                PlayCPAnimation(step++);
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

        private void PlayCPAnimation(int stepIndex)
        {
            if (stepIndex < 0 || stepIndex >= _cpListForAnimationSteps.Count - 1)
            {
                NcDebug.Log($"Argument out of range. {nameof(stepIndex)}({stepIndex})");
                return;
            }

            var from = CPHelper.DecimalToInt(_cpListForAnimationSteps[stepIndex]);
            var to = CPHelper.DecimalToInt(_cpListForAnimationSteps[stepIndex + 1]);

            if (_disposableOfCPAnimation != null)
            {
                _disposableOfCPAnimation.Dispose();
            }

            var deltaCP = to - from;
            var deltaTime = 0f;
            _disposableOfCPAnimation = Observable
                .EveryGameObjectUpdate()
                .Take(TimeSpan.FromSeconds(_dueTimeOfIncreaseCPAnimation))
                .DoOnCompleted(() => _disposableOfCPAnimation = null)
                .Subscribe(_ =>
                {
                    deltaTime += Time.deltaTime;
                    var middleCP = math.min(to, (int)(from + deltaCP * (deltaTime / .3f)));
                    _resultItem.cpText.text = $"CP {middleCP}";
                });
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
