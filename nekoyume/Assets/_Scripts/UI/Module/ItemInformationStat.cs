using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ItemInformationStat : MonoBehaviour
    {
        public Image image;
        public Text keyText;
        public Text valueText;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        public bool IsShow => gameObject.activeSelf;
        public Model.ItemInformationStat Model { get; private set; }

        public void Show(Model.ItemInformationStat model)
        {
            _disposablesForModel.DisposeAllAndClear();
            Model = model;
            Model.key.SubscribeToText(keyText).AddTo(_disposablesForModel);
            Model.value.SubscribeToText(valueText).AddTo(_disposablesForModel);
            
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
