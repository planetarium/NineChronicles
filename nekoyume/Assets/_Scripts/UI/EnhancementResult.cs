using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

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

        [SerializeField]
        private ResultItem _resultItem;

        [SerializeField]
        private List<Option> _optionTexts;

#if UNITY_EDITOR
        [Space(10)]
        [Header("Editor Properties For Test")]
        [Space(10)]
        [SerializeField]
        private List<CombinationResult.EditorStatOption> _editorStatOptions;
#endif

        private static readonly int AnimatorHashGreatSuccess = Animator.StringToHash("GreatSuccess");
        private static readonly int AnimatorHashSuccess = Animator.StringToHash("Success");
        private static readonly int AnimatorHashFail = Animator.StringToHash("Fail");
        private static readonly int AnimatorHashLoop = Animator.StringToHash("Loop");
        private static readonly int AnimatorHashClose = Animator.StringToHash("Close");

        public override WidgetType WidgetType => WidgetType.Popup;

#if UNITY_EDITOR
        protected override void OnEnable()
        {
            base.OnEnable();
        }
#endif

        [Obsolete("Use `Show(Equipment equipment)` instead.")]
        public override void Show(bool ignoreShowAnimation = false)
        {
            // ignore.
        }

        public void Show(Equipment equipment) // or ItemEnhanceMail
        {
            // NOTE: Ignore Show Animation
            base.Show(true);

            _resultItem.itemView.SetData(new CountableItem(equipment, 1));
            _resultItem.beforeGradeText.text = $"+{equipment.level - 1}";
            _resultItem.afterGradeText.text = $"+{equipment.level}";
            _resultItem.itemNameText.text = equipment.GetLocalizedName(ignoreLevel: true);
            _resultItem.cpText.text = equipment.GetCPText();

            var optionCount = equipment.optionCountFromCombination;
            var additionalStats = equipment.StatsMap.GetAdditionalStats(true)
                .ToArray();
            var additionalStatsLength = additionalStats.Length;
            var skills = equipment.Skills;
            var skillsCount = skills.Count;
            for (var i = 0; i < _optionTexts.Count; i++)
            {
                var optionText = _optionTexts[i];
                if (i < additionalStatsLength)
                {
                    if (i == 0 && optionText.secondStarObject != null)
                    {
                        optionText.secondStarObject.SetActive(additionalStatsLength < optionCount);
                    }

                    var (statType, additionalValue) = additionalStats[i];
                    optionText.totalText.text = $"{statType.GetLocalizedString()} +{additionalValue}";
                    optionText.rootObject.SetActive(true);
                }
                else if (i < additionalStatsLength + skillsCount)
                {
                    var skill = skills[i - additionalStatsLength];
                    optionText.totalText.text = $"{skill.SkillRow.GetLocalizedName()} {skill.Power} / {skill.Chance:P}";
                    optionText.rootObject.SetActive(true);
                }
                else
                {
                    optionText.rootObject.SetActive(false);
                }
            }

            // Animator.SetTrigger(AnimatorHashGreatSuccess);
            // Animator.SetTrigger(AnimatorHashSuccess);
            // Animator.SetTrigger(AnimatorHashFail);
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
