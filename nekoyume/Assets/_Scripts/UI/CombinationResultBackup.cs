using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Nekoyume.Battle;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Nekoyume.UI
{
    using UniRx;

    public class CombinationResultBackup : Widget
    {
        [Serializable]
        public class ResultItem
        {
            public TextMeshProUGUI itemNameText;
            public SimpleCountableItemView itemView;
            public TextMeshProUGUI mainStatText;
            public TextMeshProUGUI cpText;
        }

        [Serializable]
        public class Option
        {
            public GameObject rootObject;
            public TextMeshProUGUI text;

            [CanBeNull]
            public GameObject secondStarObject;
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
        private ResultItem _resultItem;

        [SerializeField]
        private List<GameObject> _statOptionIconObjects;

        [SerializeField]
        private List<GameObject> _skillOptionIconObjects;

        [SerializeField]
        private List<Option> _statOptionTexts;

        [SerializeField]
        private List<Option> _skillOptionTexts;

        [SerializeField]
        private float _dueTimeOfIncreaseCPAnimation;

#if UNITY_EDITOR
        [Space(10)]
        [Header("Editor Properties For Test")]
        [Space(10)]
        [SerializeField]
        private List<EditorStatOption> _editorStatOptions;

        [SerializeField]
        private List<EditorSkillOption> _editorSkillOptions;
#endif

        private static readonly int AnimatorHashGreatSuccess = Animator.StringToHash("GreatSuccess");
        private static readonly int AnimatorHashSuccess = Animator.StringToHash("Success");
        private static readonly int AnimatorHashLoop = Animator.StringToHash("Loop");
        private static readonly int AnimatorHashClose = Animator.StringToHash("Close");

        private readonly List<decimal> _cpListForAnimationSteps = new List<decimal>();
        private IDisposable _disposableOfSkip;
        private IDisposable _disposableOfOpenOption;

        public override WidgetType WidgetType => WidgetType.Popup;

        protected override void OnDisable()
        {
            _disposableOfSkip?.Dispose();
            _disposableOfSkip = null;

            _disposableOfOpenOption?.Dispose();
            _disposableOfOpenOption = null;

            base.OnDisable();
        }

#if UNITY_EDITOR
        public void ShowWithEditorProperty()
        {
            ItemUsable itemUsable;
            var tableSheets = Game.Game.instance.TableSheets;
            if (Random.Range(0, 2) == 0)
            {
                var equipmentList = tableSheets.EquipmentItemSheet.OrderedList;
                var equipmentRow = equipmentList[Random.Range(0, equipmentList.Count)];
                var equipment = (Equipment) ItemFactory.CreateItemUsable(equipmentRow, Guid.NewGuid(), 0);
                foreach (var statOption in _editorStatOptions)
                {
                    equipment.StatsMap.AddStatAdditionalValue(statOption.statType, statOption.value);
                    equipment.optionCountFromCombination++;
                }

                var skillList = tableSheets.SkillSheet.OrderedList;
                foreach (var skillOption in _editorSkillOptions)
                {
                    var row = skillList[Random.Range(0, skillList.Count)];
                    var skill = SkillFactory.Get(row, skillOption.power, skillOption.chance);
                    equipment.Skills.Add(skill);
                    equipment.optionCountFromCombination++;
                }

                itemUsable = equipment;
            }
            else
            {
                var consumableList = tableSheets.ConsumableItemSheet.OrderedList;
                var consumableRow = consumableList[Random.Range(0, consumableList.Count)];
                var consumable = (Consumable) ItemFactory.CreateItemUsable(consumableRow, Guid.NewGuid(), 0);
                foreach (var statOption in _editorStatOptions)
                {
                    consumable.StatsMap.AddStatValue(statOption.statType, statOption.value);
                }

                itemUsable = consumable;
            }

            Show(itemUsable);
        }
#endif

        [Obsolete("Use `Show(ItemUsable itemUsable)` instead.")]
        public override void Show(bool ignoreShowAnimation = false)
        {
            // ignore.
        }

        public void Show(ItemUsable itemUsable)
        {
            if (itemUsable is null)
            {
                Debug.LogError($"{nameof(itemUsable)} is null");
                return;
            }

            _cpListForAnimationSteps.Clear();
            _resultItem.itemNameText.text = itemUsable.GetLocalizedName(useElementalIcon: false);
            _resultItem.itemView.SetData(new CountableItem(itemUsable, 1));
            _resultItem.mainStatText.text = string.Empty;
            _resultItem.cpText.text = string.Empty;

            var optionInfo = itemUsable is Equipment equipment
                ? new ItemOptionInfo(equipment)
                : new ItemOptionInfo(itemUsable);
            var statOptions = optionInfo.StatOptions;
            var statOptionsCount = statOptions.Count;
            for (var i = 0; i < _statOptionIconObjects.Count; i++)
            {
                _statOptionIconObjects[i].SetActive(i < statOptionsCount);
            }

            for (var i = 0; i < _statOptionTexts.Count; i++)
            {
                var statOptionText = _statOptionTexts[i];
                if (i >= statOptionsCount)
                {
                    statOptionText.rootObject.SetActive(false);
                    continue;
                }

                var (type, value, count) = statOptions[i];
                if (i == 0 && statOptionText.secondStarObject)
                {
                    statOptionText.secondStarObject.SetActive(count > 1);
                }

                statOptionText.text.text = $"{type.ToString()} +{value}";
                statOptionText.rootObject.SetActive(true);
            }

            var skillOptions = optionInfo.SkillOptions;
            var skillOptionsCount = skillOptions.Count;
            for (var i = 0; i < _skillOptionIconObjects.Count; i++)
            {
                _skillOptionIconObjects[i].SetActive(i < skillOptionsCount);
            }

            for (var i = 0; i < _skillOptionTexts.Count; i++)
            {
                var skillOptionText = _skillOptionTexts[i];
                if (i >= skillOptionsCount)
                {
                    skillOptionText.rootObject.SetActive(false);
                    continue;
                }

                var (skillName, power, chance) = skillOptions[i];
                skillOptionText.text.text = $"{skillName} {power} / {chance:P}";
                skillOptionText.rootObject.SetActive(true);
            }

            if (itemUsable is Equipment equipment2)
            {
                Show(equipment2);
            }
            else
            {
                _iconImage.overrideSprite = _consumableIconSprite;

                // NOTE: Ignore Show Animation
                base.Show(true);
                Animator.SetTrigger(AnimatorHashSuccess);
            }
        }

        private void Show(Equipment equipment, ItemOptionInfo itemOptionInfo)
        {
            _iconImage.overrideSprite = _equipmentIconSprite;

            var (mainStatType, mainStatValue) = itemOptionInfo.MainStat;
            _resultItem.mainStatText.text = $"{mainStatType.ToString()} {mainStatValue}";

            var statsCP = CPHelper.GetStatCP(mainStatType, mainStatValue);
            _cpListForAnimationSteps.Add(statsCP);
            _resultItem.cpText.text = CPHelper.DecimalToInt(statsCP).ToString();

            var statOptions = itemOptionInfo.StatOptions;
            foreach (var (type, value, _) in statOptions)
            {
                statsCP += CPHelper.GetStatCP(type, value);
                _cpListForAnimationSteps.Add(statsCP);
            }

            var skillOptions = itemOptionInfo.SkillOptions;
            for (var i = 0; i < skillOptions.Count; i++)
            {
                var multipliedCP = statsCP * CPHelper.GetSkillsMultiplier(i + 1);
                _cpListForAnimationSteps.Add(multipliedCP);
            }

            if (CPHelper.GetCP(equipment) !=
                CPHelper.DecimalToInt(_cpListForAnimationSteps[_cpListForAnimationSteps.Count - 1]))
            {
                Debug.LogError(
                    $"Wrong CP!!!! {CPHelper.GetCP(equipment)} != {_cpListForAnimationSteps[_cpListForAnimationSteps.Count - 1]}");
            }

            // NOTE: Ignore Show Animation
            base.Show(true);
            Animator.SetTrigger(itemOptionInfo.OptionCountFromCombination == 4
                ? AnimatorHashGreatSuccess
                : AnimatorHashSuccess);
        }

        #region Invoke from Animation

        public void OnAnimatorStateBeginning(string stateName)
        {
            Debug.Log("OnAnimatorStateBeginning: " + stateName);
            switch (stateName)
            {
                case "GreatSuccess":
                case "Success":
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

        public void OnOpenOption(int optionIndex)
        {
            if (optionIndex < 0 || optionIndex >= _cpListForAnimationSteps.Count - 1)
            {
                Debug.Log($"Argument out of range. {nameof(optionIndex)}({optionIndex})");
                return;
            }

            PlayCPAnimation(
                CPHelper.DecimalToInt(_cpListForAnimationSteps[optionIndex]),
                CPHelper.DecimalToInt(_cpListForAnimationSteps[optionIndex + 1]));
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
                hash == AnimatorHashSuccess)
            {
                Observable.NextFrame().Subscribe(_ =>
                    Animator.Play(AnimatorHashLoop, 0, 0));
            }

            if (_cpListForAnimationSteps.Any())
            {
                _resultItem.cpText.text = CPHelper
                    .DecimalToInt(_cpListForAnimationSteps[_cpListForAnimationSteps.Count - 1])
                    .ToString();
            }
        }

        private void PlayCPAnimation(int from, int to)
        {
            if (_disposableOfOpenOption != null)
            {
                _disposableOfOpenOption.Dispose();
            }

            var deltaCP = to - from;
            var deltaTime = 0f;
            _disposableOfOpenOption = Observable
                .EveryGameObjectUpdate()
                .Take(TimeSpan.FromSeconds(_dueTimeOfIncreaseCPAnimation))
                .DoOnCompleted(() => _disposableOfOpenOption = null)
                .Subscribe(_ =>
                {
                    deltaTime += Time.deltaTime;
                    var middleCP = math.min(to, (int) (from + deltaCP * (deltaTime / .3f)));
                    _resultItem.cpText.text = middleCP.ToString();
                });
        }

        protected override void Update()
        {
            base.Update();

            if (Input.GetKeyDown(KeyCode.A))
            {
                var from = Random.Range(999, 999999);
                var to = from + Random.Range(99, 99999);
                PlayCPAnimation(from, to);
            }
        }
    }
}
