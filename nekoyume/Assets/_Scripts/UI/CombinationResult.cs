using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.Model.Stat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class CombinationResult : Widget
    {
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
        }
#endif

        [Obsolete("Use `Show(ItemUsable equipment)` instead.")]
        public override void Show(bool ignoreShowAnimation = false)
        {
            // ignore.
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            // ignore.
        }

        public void Show(ItemUsable itemUsable)
        {
            base.Show(true);

            if (itemUsable is Equipment equipment)
            {
                Animator.SetTrigger(equipment.optionCountFromCombination == 4
                    ? AnimatorHashGreatSuccess
                    : AnimatorHashSuccess);
            }
            else
            {
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
