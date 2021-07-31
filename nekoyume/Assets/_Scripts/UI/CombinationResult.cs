using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class CombinationResult : Widget
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
            public float value;
        }

        [Serializable]
        public class EditorSkillOption
        {
            public string skillName;
            public float chance;
            public float power;
        }
#endif

        [SerializeField]
        private Image _iconSprite;

        [SerializeField]
        private ResultItem _resultItem;

        [SerializeField]
        private List<GameObject> _optionStarObjects;

        [SerializeField]
        private List<Option> _optionTexts;

        [SerializeField]
        private Button _skipButton;

#if UNITY_EDITOR
        [Space(10)]
        [Header("Editor Properties For Test")]
        [Space(10)]
        [SerializeField]
        private EquipmentOrFood _equipmentOrFood;

        [SerializeField]
        private EditorStatOption _editorMainStat;

        [SerializeField]
        private List<EditorStatOption> _editorStatOptions;

        [SerializeField]
        private List<EditorSkillOption> _editorSkillOptions;
#endif

        private static readonly int AnimatorHashGreatSuccess = Animator.StringToHash("GreatSuccess");
        private static readonly int AnimatorHashSuccess = Animator.StringToHash("Success");
        private static readonly int AnimatorHashLoop = Animator.StringToHash("Loop");
        private static readonly int AnimatorHashClose = Animator.StringToHash("Close");

        public override WidgetType WidgetType => WidgetType.Popup;

        protected override void Awake()
        {
            base.Awake();

            _skipButton.OnClickAsObservable()
                .ThrottleFirst(TimeSpan.FromSeconds(1d))
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    SkipAnimation();
                }).AddTo(gameObject);
        }

#if UNITY_EDITOR
        protected override void OnEnable()
        {
            base.OnEnable();

            // var itemUsable = _equipmentOrFood == EquipmentOrFood.Equipment
            //     ? (ItemUsable) new Equipment(new EquipmentItemSheet.Row(), Guid.NewGuid(), 0)
            //     : new Consumable(new ConsumableItemSheet.Row(), Guid.NewGuid(), 0);
        }
#endif

        [Obsolete("Use `Show(ItemUsable itemUsable)` instead.")]
        public override void Show(bool ignoreShowAnimation = false)
        {
            // ignore.
        }

        public void Show(ItemUsable itemUsable)
        {
            // NOTE: Ignore Show Animation
            base.Show(true);

            _resultItem.itemNameText.text = itemUsable.GetLocalizedName();
            _resultItem.itemView.SetData(new CountableItem(itemUsable, 1));

            if (itemUsable is Equipment equipment)
            {
                _resultItem.mainStatText.text = equipment.StatsMap.GetStat(equipment.UniqueStatType, true).ToString();
                _resultItem.cpText.text = itemUsable.GetCPText();

                var optionCount = equipment.optionCountFromCombination;
                for (var i = 0; i < _optionStarObjects.Count; i++)
                {
                    _optionStarObjects[i].SetActive(i < optionCount);
                }

                var additionalStats = equipment.StatsMap.GetAdditionalStats(true)
                    .ToArray();
                var additionalStatsLength = additionalStats.Length;
                var skills = equipment.Skills;
                var skillsCount = skills.Count;
                var optionTextsIndex = 0;
                while (optionTextsIndex < _optionTexts.Count)
                {
                    var optionText = _optionTexts[optionTextsIndex];
                    if (optionTextsIndex < additionalStatsLength)
                    {
                        if (optionTextsIndex == 0 && optionText.secondStarObject != null)
                        {
                            optionText.secondStarObject.SetActive(additionalStatsLength < optionCount);
                        }

                        var (statType, additionalValue) = additionalStats[optionTextsIndex];
                        optionText.text.text = $"{statType.GetLocalizedString()} +{additionalValue}";
                        optionText.rootObject.SetActive(true);
                    }
                    else if (optionTextsIndex < additionalStatsLength + skillsCount)
                    {
                        var skill = skills[optionTextsIndex - additionalStatsLength];
                        optionText.text.text = $"{skill.SkillRow.GetLocalizedName()} {skill.Power} / {skill.Chance:P}";
                        optionText.rootObject.SetActive(true);
                    }
                    else
                    {
                        optionText.rootObject.SetActive(false);
                    }

                    optionTextsIndex++;
                }

                Animator.SetTrigger(optionCount == 4
                    ? AnimatorHashGreatSuccess
                    : AnimatorHashSuccess);
            }
            else if (itemUsable is Consumable consumable)
            {
                _resultItem.mainStatText.text = string.Empty;
                _resultItem.cpText.text = string.Empty;

                var stats = consumable.StatsMap.GetStats(true)
                    .ToArray();
                var statsLength = stats.Length;
                for (var i = 0; i < _optionStarObjects.Count; i++)
                {
                    _optionStarObjects[i].SetActive(i < statsLength);
                }

                var optionTextsIndex = 0;
                while (optionTextsIndex < _optionTexts.Count)
                {
                    var optionText = _optionTexts[optionTextsIndex];
                    if (optionText.secondStarObject != null)
                    {
                        optionText.secondStarObject.SetActive(false);
                    }

                    if (optionTextsIndex < statsLength)
                    {
                        var (statType, additionalValue) = stats[optionTextsIndex];
                        optionText.text.text = $"{statType.GetLocalizedString()} +{additionalValue}";
                        optionText.rootObject.SetActive(true);
                    }
                    else
                    {
                        optionText.rootObject.SetActive(false);
                    }

                    optionTextsIndex++;
                }

                Animator.SetTrigger(AnimatorHashSuccess);
            }
        }

        public void OnAnimatorStateBeginning(string stateName)
        {
            Debug.Log("OnAnimatorStateBeginning: " + stateName);
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
        }
    }
}
