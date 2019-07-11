using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ItemInformationSkill : MonoBehaviour
    {
        public Image headerImage;
        public Text headerKeyText;
        public Text headerValueText;
        public Text firstLineKeyText;
        public Text firstLineValueText;
        public GameObject secondLine;
        public Text secondLineKeyText;
        public Text secondLineValueText;
        
        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        public bool IsShow => gameObject.activeSelf;
        public Model.ItemInformationSkill Model { get; private set; }

        public void Show(Model.ItemInformationSkill model)
        {
            _disposablesForModel.DisposeAllAndClear();
            Model = model;
            Model.headerKey.SubscribeToText(headerKeyText).AddTo(_disposablesForModel);
            Model.headerValue.SubscribeToText(headerValueText).AddTo(_disposablesForModel);
            Model.firstLineKey.SubscribeToText(firstLineKeyText).AddTo(_disposablesForModel);
            Model.firstLineValue.SubscribeToText(firstLineValueText).AddTo(_disposablesForModel);
            Model.secondLineEnabled.Subscribe(secondLine.SetActive).AddTo(_disposablesForModel);
            Model.secondLineKey.SubscribeToText(secondLineKeyText).AddTo(_disposablesForModel);
            Model.secondLineValue.SubscribeToText(secondLineValueText).AddTo(_disposablesForModel);

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            
            _disposablesForModel.DisposeAllAndClear();
            Model = null;
        }
    }
}
