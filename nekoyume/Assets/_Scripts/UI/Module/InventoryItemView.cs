using System;
using System.Collections.Generic;
using Nekoyume.Game.Controller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    [RequireComponent(typeof(Button))]
    public class InventoryItemView : CountableItemView<Model.InventoryItem>
    {
        public Image coverImage;
        public Image selectionImage;
        public Image glowImage;
        public TextMeshProUGUI equipmentText;

        private Button _button;

        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        private readonly TimeSpan _timeSpan200Milliseconds = TimeSpan.FromMilliseconds(200);

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            this.ComponentFieldsNotNullTest();

            _button = GetComponent<Button>();
            var buttonClickStream = _button.OnClickAsObservable();
            buttonClickStream
                .Subscribe(_ =>
                {
                    AudioController.PlaySelect();
                    Data.onClick.OnNext(Data);
                    var model = new Model.ItemInformationTooltip(Data);
                    model.target.Value = GetComponent<RectTransform>();
                    Widget.Find<ItemInformationTooltip>()?.Show(model);
                })
                .AddTo(_disposablesForAwake);
            buttonClickStream
                .Buffer(buttonClickStream.Throttle(_timeSpan200Milliseconds))
                .Where(_ => _.Count >= 2)
                .Subscribe(_ =>
                {
                    Data.onDoubleClick.OnNext(Data);
                }).AddTo(_disposablesForAwake);
        }

        protected override void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();
            Clear();
        }

        #endregion

        #region override

        public override void SetData(Model.InventoryItem value)
        {
            if (ReferenceEquals(value, null))
            {
                Clear();
                return;
            }

            base.SetData(value);
            _disposablesForSetData.DisposeAllAndClear();
            Data.covered.Subscribe(SetCover).AddTo(_disposablesForSetData);
            Data.dimmed.Subscribe(SetDim).AddTo(_disposablesForSetData);
            Data.selected.Subscribe(SetSelect).AddTo(_disposablesForSetData);
            Data.glowed.Subscribe(SetGlow).AddTo(_disposablesForSetData);
            Data.count.Subscribe(SetCount).AddTo(_disposablesForSetData);

            UpdateView();
        }

        public override void Clear()
        {
            _disposablesForSetData.DisposeAllAndClear();
            base.Clear();

            UpdateView();
        }

        protected override void SetDim(bool isDim)
        {
            base.SetDim(isDim);

            selectionImage.color = isDim ? DimColor : DefaultColor;
        }

        #endregion

        private void UpdateView()
        {
            if (ReferenceEquals(Data, null))
            {
                selectionImage.enabled = false;

                return;
            }

            coverImage.enabled = Data.covered.Value;
            selectionImage.enabled = Data.selected.Value;
            SetDim(Data.dimmed.Value);
        }

        private void SetCover(bool isCover)
        {
            coverImage.enabled = isCover;
        }

        private void SetSelect(bool isSelect)
        {
            selectionImage.enabled = isSelect;
        }

        private void SetGlow(bool isGlow)
        {
            glowImage.enabled = isGlow;
        }
    }
}
