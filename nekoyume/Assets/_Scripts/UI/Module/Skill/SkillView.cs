using Nekoyume.UI.Module.Common;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using EnumType;
    using Model;
    using UniRx;

    public class SkillView : VanillaSkillView
    {
        public RectTransform informationArea;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI powerText;
        public TextMeshProUGUI chanceText;
        public Button skillDescriptionButton;

        private readonly List<IDisposable> _disposablesForModel = new();
        private System.Action _onClickDesc;

        public Model.SkillView Model { get; private set; }

        private void Awake()
        {
            skillDescriptionButton.onClick.AddListener(() => _onClickDesc?.Invoke());
        }

        public void SetData(Model.SkillView model, SkillPositionTooltip tooltip)
        {
            if (model is null)
            {
                Hide();

                return;
            }

            _disposablesForModel.DisposeAllAndClear();
            Model = model;
            tooltip.Hide();
            _onClickDesc = () =>
            {
                if (!tooltip.gameObject.activeSelf)
                {
                    tooltip.Show(model.Skill);
                    var rect = skillDescriptionButton.GetComponent<RectTransform>();
                    tooltip.transform.position = rect.GetWorldPositionOfPivot(PivotPresetType.MiddleLeft);
                }
                else
                {
                    tooltip.Hide();
                }
            };
            Model.Name.SubscribeTo(nameText).AddTo(_disposablesForModel);
            Model.Power.SubscribeTo(powerText).AddTo(_disposablesForModel);
            Model.Chance.SubscribeTo(chanceText).AddTo(_disposablesForModel);
        }

        public void Show(string name, string description, string chance)
        {
            nameText.text = name;
            powerText.text = description;
            chanceText.text = chance;
            base.Show();
        }

        public override void Hide()
        {
            base.Hide();
            Model?.Dispose();
            Model = null;
            _disposablesForModel.DisposeAllAndClear();
        }
    }
}
