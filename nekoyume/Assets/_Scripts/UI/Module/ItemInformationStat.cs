using System;
using System.Collections.Generic;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ItemInformationStat : MonoBehaviour
    {
        public Image bulletMainImage;
        public Image bulletSubImage;
        public TextMeshProUGUI keyText;
        public TextMeshProUGUI valueText;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        public bool IsShow => gameObject.activeSelf;
        public Model.ItemInformationStat Model { get; private set; }

        public void Show(Model.ItemInformationStat model)
        {
            if (model is null)
            {
                Hide();
                
                return;
            }
            
            _disposablesForModel.DisposeAllAndClear();
            Model = model;
            Model.IsMainStat.Subscribe(isMainStat =>
            {
                bulletMainImage.enabled = isMainStat;
                bulletSubImage.enabled = !isMainStat;
            }).AddTo(_disposablesForModel);
            Model.Key.SubscribeToText(keyText).AddTo(_disposablesForModel);
            Model.Value.SubscribeToText(valueText).AddTo(_disposablesForModel);
            
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
