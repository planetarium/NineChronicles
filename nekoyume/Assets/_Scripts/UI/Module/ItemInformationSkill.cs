using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ItemInformationSkill : MonoBehaviour
    {
        public Image iconImage;
        public RectTransform informationArea;
        public Text nameText;
        public Text powerText;
        public Text chanceText;
        
        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        public bool IsShow => gameObject.activeSelf;
        public Model.ItemInformationSkill Model { get; private set; }

        public void Show(Model.ItemInformationSkill model)
        {
            if (model is null)
            {
                Hide();
                
                return;
            }
            
            _disposablesForModel.DisposeAllAndClear();
            Model = model;
            Model.iconSprite.SubscribeTo(iconImage).AddTo(_disposablesForModel);
            Model.name.SubscribeToText(nameText).AddTo(_disposablesForModel);
            Model.power.SubscribeToText(powerText).AddTo(_disposablesForModel);
            Model.chance.SubscribeToText(chanceText).AddTo(_disposablesForModel);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            Model?.Dispose();
            Model = null;
            _disposablesForModel.DisposeAllAndClear();
        }
    }
}
