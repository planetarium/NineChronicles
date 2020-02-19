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

        public bool IsMinCount => !(Model is null) && Model.Count.Value == Model.MinCount.Value;
        public bool IsMaxCount => !(Model is null) && Model.Count.Value == Model.MaxCount.Value;

        protected override ImageSizeType imageSizeType => ImageSizeType.Middle;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            minusButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    TryDecreaseCount();
                    OnMinus.OnNext(this);
                })
                .AddTo(gameObject);
            
            plusButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    TryIncreaseCount();
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

        public bool TryIncreaseCount(int value = 1)
        {
            if (Model is null)
                return false;

            if (Model.Count.Value + value > Model.MaxCount.Value)
                return false;
                
            Model.Count.Value += value;
            OnCountChange.OnNext(Model.Count.Value);
            return true;
        }

        public bool TryDecreaseCount(int value = 1)
        {
            if (Model is null)
                return false;

            if (Model.Count.Value - value < Model.MinCount.Value)
                return false;
                
            Model.Count.Value -= value;
            OnCountChange.OnNext(Model.Count.Value);
            return true;
        }
    }
}
