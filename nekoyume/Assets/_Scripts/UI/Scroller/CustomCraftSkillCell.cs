using System;
using Nekoyume.TableData;
using Nekoyume.UI.Module.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Scroller
{
    using UniRx;
    public class CustomCraftSkillCell : RectCell<CustomCraftSkillCell.Model, RectScrollDefaultContext>
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

        [SerializeField]
        private SkillPositionTooltip skillPositionTooltip;

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
            skillPositionTooltip.Show(_skillRow, _optionRow);
        }
    }
}
