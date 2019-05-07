using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ItemInfo : MonoBehaviour
    {
        public Text nameText;
        public Text infoText;
        public Text descriptionText;
        public SimpleCountableItemView itemView;
        public Button button;
        public Text buttonText;

        private Model.ItemInfo _data;
        
        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForUpdateView = new List<IDisposable>();
        
        #region Mono

        protected void Awake()
        {
            this.ComponentFieldsNotNullTest();
            
            button.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _data.onClick.OnNext(_data);
                    AudioController.instance.PlaySfx(AudioController.SfxCode.InputItem);
                })
                .AddTo(_disposablesForAwake);
        }

        private void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();
            Clear();
        }

        #endregion

        public void SetData(Model.ItemInfo data)
        {
            if (ReferenceEquals(data, null))
            {
                Clear();
                return;       
            }
            
            _disposablesForSetData.DisposeAllAndClear();
            _data = data;
            _data.item.Subscribe(_ => UpdateView()).AddTo(_disposablesForSetData);
            _data.buttonEnabled.Subscribe(SetButtonActive).AddTo(_disposablesForSetData);
            _data.buttonText.Subscribe(SetButtonText).AddTo(_disposablesForSetData);
            
            UpdateView();
        }

        public void Clear()
        {
            _disposablesForSetData.DisposeAllAndClear();
            _data = null;
            
            UpdateView();
        }
        
        private void UpdateView()
        {
            if (ReferenceEquals(_data, null) ||
                ReferenceEquals(_data.item.Value, null))
            {
                nameText.text = "아이템 자세히 보기";
                infoText.text = "상단의 아이템을 클릭하세요";
                descriptionText.text = "";
                button.gameObject.SetActive(false);
                
                itemView.Clear();
                
                return;
            }
            
            _disposablesForUpdateView.DisposeAllAndClear();
            var item = _data.item.Value;
            item.count.Where(value => value == 0).Subscribe(_ => _data.item.Value = null).AddTo(_disposablesForUpdateView);
            nameText.text = item.item.Value.Item.Data.name;
            infoText.text = item.item.Value.Item.ToItemInfo();
            descriptionText.text = item.item.Value.Item.Data.description;
            SetButtonText(_data.buttonText.Value);
            SetButtonActive(_data.buttonEnabled.Value);
            
            itemView.SetData(_data.item.Value);
        }
        
        private void SetButtonActive(bool isActive)
        {
            button.gameObject.SetActive(isActive);
        }

        private void SetButtonText(string text)
        {
            buttonText.text = text;
        }
    }
}
