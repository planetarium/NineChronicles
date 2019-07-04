using System;
using System.Collections.Generic;
using Nekoyume.EnumType;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ItemInformationTooltip : VerticalTooltipWidget<Model.ItemInformationTooltip>
    {
        public Text titleText;
        public ItemInformation itemInformation;
        public GameObject closeGameObject;
        public Button closeButton;
        public Text closeButtonText;
        public GameObject submitGameObject;
        public Button submitButton;
        public Text submitButtonText;
        
        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();
        
        protected override void Awake()
        {
            base.Awake();

            closeButton.OnClickAsObservable().Subscribe(_ => { Close(); }).AddTo(_disposablesForAwake);
            submitButton.OnClickAsObservable().Subscribe(_ => { Close(); }).AddTo(_disposablesForAwake);
        }

        private void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();
        }

        public override void Show()
        {
            if (Model is null)
            {
                Close();
                
                return;
            }
            
            base.Show();

            itemInformation.SetData(Model.itemInformation);
            
            Model.titleText.SubscribeToText(titleText).AddTo(_disposablesForModel);
            Model.itemInformation.item.Subscribe(value =>
            {
                base.SubscribeTarget(Model.target.Value);
            }).AddTo(_disposablesForModel);
            Model.closeButtonText.Subscribe(value =>
            {
                if (string.IsNullOrEmpty(value))
                {
                    closeGameObject.SetActive(false);

                    return;
                }
                
                closeButtonText.text = value;
                closeGameObject.SetActive(true);
            }).AddTo(_disposablesForModel);
            Model.submitButtonText.Subscribe(value =>
            {
                if (string.IsNullOrEmpty(value))
                {
                    submitGameObject.SetActive(false);

                    return;
                }
                
                submitButtonText.text = value;
                submitGameObject.SetActive(true);
            }).AddTo(_disposablesForModel);
        }

        public override void Close()
        {
            _disposablesForModel.DisposeAllAndClear();
            
            base.Close();
        }
        
        protected override void SubscribeTarget(RectTransform target)
        {
            // 타겟이 바뀔 때, 아이템도 바뀌니 아이템을 구독하는 쪽 한 곳에 로직을 구현한다. 
        }
    }
}
