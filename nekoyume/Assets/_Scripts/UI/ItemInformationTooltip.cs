using System;
using System.Collections.Generic;
using DefaultNamespace;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ItemInformationTooltip : TooltipWidget
    {
        public Text titleText;
        public ItemInformation itemInformation;
        public GameObject closeGameObject;
        public Button closeButton;
        public Text closeButtonText;
        public GameObject submitGameObject;
        public Button submitButton;
        public Text submitButtonText;
        
        public Model.ItemInformationTooltip Model { get; private set; }
        
        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        protected override void Awake()
        {
            base.Awake();

            closeButton.OnClickAsObservable().Subscribe().AddTo(_disposablesForAwake);
            submitButton.OnClickAsObservable().Subscribe().AddTo(_disposablesForAwake);
        }

        private void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();
        }

        public void Show(Model.ItemInformationTooltip value)
        {
            _disposablesForModel.DisposeAllAndClear();
            Model = value;
            
            Show();
        }

        public override void Show()
        {
            base.Show();
            
            if (Model is null)
            {
                Close();
                
                return;
            }

            Model.titleText.SubscribeToText(titleText).AddTo(_disposablesForModel);
            Model.itemInformation.Subscribe(itemInformation.SetData).AddTo(_disposablesForAwake);
            Model.closeButtonText.Subscribe(value =>
            {
                if (string.IsNullOrEmpty(value))
                {
                    closeGameObject.SetActive(false);
                }
                closeButtonText.text = value;
                closeGameObject.SetActive(true);
            }).AddTo(_disposablesForAwake);
            Model.submitButtonText.Subscribe(value =>
            {
                if (string.IsNullOrEmpty(value))
                {
                    submitGameObject.SetActive(false);
                }
                submitButtonText.text = value;
                submitGameObject.SetActive(true);
            }).AddTo(_disposablesForAwake);
        }

        public override void Close()
        {
            _disposablesForModel.DisposeAllAndClear();
            Model = null;
            
            base.Close();
        }

        private void MoveInsideOf()
        {
            
        }
    }
}
