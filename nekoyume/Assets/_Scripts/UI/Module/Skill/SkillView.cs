using System;
using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class SkillView : VanillaSkillView
    {
        public RectTransform informationArea;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI powerText;
        public TextMeshProUGUI chanceText;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        public Model.SkillView Model { get; private set; }

        public void SetData(Model.SkillView model)
        {
            if (model is null)
            {
                Hide();

                return;
            }

            _disposablesForModel.DisposeAllAndClear();
            Model = model;
            Model.name.SubscribeTo(nameText).AddTo(_disposablesForModel);
            Model.power.SubscribeTo(powerText).AddTo(_disposablesForModel);
            Model.chance.SubscribeTo(chanceText).AddTo(_disposablesForModel);
        }

        public void Show(string name, string power, string chance)
        {
            nameText.text = name;
            powerText.text = power;
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
