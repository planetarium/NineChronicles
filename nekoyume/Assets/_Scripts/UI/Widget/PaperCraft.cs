using System;
using Nekoyume.Game;
using Nekoyume.Model.Item;
using Nekoyume.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;
    public class PaperCraft : Widget
    {
        [Serializable]
        private class SubTypeButton
        {
            public ItemSubType itemSubType;
            public Button button;
        }

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private SubTypeButton[] subTypeButtons;

        // [SerializeField]
        // private ConditionalCostButton conditionalCostButton;

        [SerializeField]
        private Button craftButton;

        [SerializeField]
        private Button skillHelpButton;

        [SerializeField]
        private Button skillListButton;

        [SerializeField]
        private TextMeshProUGUI subTypePaperText;

        [SerializeField]
        private TextMeshProUGUI skillText;

        private ItemSubType _selectedSubType = ItemSubType.Weapon;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() =>
            {
                Close(true);
                Find<CombinationMain>().Show();
            });
            craftButton.onClick.AddListener(() =>
            {
                NcDebug.Log("craftButton onclick");
                Find<OutfitSelectPopup>().Show(_selectedSubType);
            });
            skillHelpButton.onClick.AddListener(() =>
            {
                NcDebug.Log("skillHelpButton onclick");
                // 뭐시기팝업.Show();
            });
            skillListButton.onClick.AddListener(() =>
            {
                Find<SummonSkillsPopup>().Show(TableSheets.Instance.SummonSheet.First);
                NcDebug.Log("skillListButton onclick");
            });
            foreach (var subTypeButton in subTypeButtons)
            {
                subTypeButton.button.onClick.AddListener(() =>
                {
                    OnItemSubtypeSelected(subTypeButton.itemSubType);
                });
            }
        }

        public override void Initialize()
        {
            base.Initialize();
            ReactiveAvatarState.ObservablePaperCraftingSkill
                .Where(_ => isActiveAndEnabled)
                .Subscribe(SetSkillView)
                .AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            SetSkillView(ReactiveAvatarState.PaperCraftingSkill);
            OnItemSubtypeSelected(ItemSubType.Weapon);
        }

        /// <summary>
        /// 숙련도의 상태를 표시하는 View update 코드이다.
        /// State를 보여주는 기능으로, ActionRenderHandler나 ReactiveAvatarState를 반영해야 한다.
        /// </summary>
        /// <param name="skill"></param>
        private void SetSkillView(long skill)
        {
            skillText.SetText($"SKILL: {skill}");
        }

        /// <summary>
        /// 어떤 종류의 장비를 만들지 ItemSubType을 선택하면 실행될 콜백, View 업데이트를 한다
        /// </summary>
        /// <param name="type"></param>
        private void OnItemSubtypeSelected(ItemSubType type)
        {
            _selectedSubType = type;
            subTypePaperText.SetText($"{_selectedSubType} PAPER");
        }
    }
}
