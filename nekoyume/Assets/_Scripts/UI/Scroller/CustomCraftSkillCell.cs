using System;
using Nekoyume.TableData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    using UniRx;
    public class CustomCraftSkillCell : RectCell<CustomCraftSkillCell.Model, CustomCraftSkillScroll.ContextModel>
    {
        public class Model
        {
            public string SkillName;
            public string SkillRatio;
            public SkillSheet.Row SkillRow;
            public EquipmentItemOptionSheet.Row OptionRow;
        }

        [SerializeField]
        private TextMeshProUGUI nameText;

        [SerializeField]
        private TextMeshProUGUI ratioText;

        [SerializeField]
        private Button detailButton;

        private SkillSheet.Row _skillRow;
        private EquipmentItemOptionSheet.Row _optionRow;

        private void Awake()
        {
            detailButton.OnClickAsObservable()
                .ThrottleFirst(new TimeSpan(0, 0, 1))
                .Subscribe(OnClickDetailButton)
                .AddTo(gameObject);
        }

        public override void UpdateContent(Model itemData)
        {
            nameText.SetText(itemData.SkillName);
            ratioText.SetText(itemData.SkillRatio);
            _skillRow = itemData.SkillRow;
            _optionRow = itemData.OptionRow;
        }

        private void OnClickDetailButton(Unit _)
        {
            Context.OnClickDetailButton.OnNext((new Model {OptionRow = _optionRow, SkillRow = _skillRow}, detailButton.transform));
        }
    }
}
