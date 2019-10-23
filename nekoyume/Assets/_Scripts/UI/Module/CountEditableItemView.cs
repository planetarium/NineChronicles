using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CountEditableItemView<T> : CountableItemView<T> where T : Model.CountEditableItem
    {
        public Button minusButton;
        public Button plusButton;
        
        public readonly Subject<CountEditableItemView<T>> OnMinus = new Subject<CountEditableItemView<T>>();
        public readonly Subject<CountEditableItemView<T>> OnPlus = new Subject<CountEditableItemView<T>>();
        public readonly Subject<int> OnCountChange = new Subject<int>();

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            minusButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    DecreaseCount();
                    OnMinus.OnNext(this);
                })
                .AddTo(gameObject);
            
            plusButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    IncreaseCount();
                    OnPlus.OnNext(this);
                })
                .AddTo(gameObject);
        }
        
        protected override void OnDestroy()
        {
            OnMinus.Dispose();
            OnPlus.Dispose();
            OnCountChange.Dispose();
            base.OnDestroy();
        }

        #endregion

        public void IncreaseCount(int value = 1)
        {
            if (Model is null)
                return;

            if (Model.Count.Value + value > Model.MaxCount.Value)
                return;
                
            Model.Count.Value += value;
            OnCountChange.OnNext(Model.Count.Value);
        }

        public void DecreaseCount(int value = 1)
        {
            if (Model is null)
                return;

            if (Model.Count.Value - value < Model.MinCount.Value)
                return;
                
            Model.Count.Value -= value;
            OnCountChange.OnNext(Model.Count.Value);
        }
    }
}
