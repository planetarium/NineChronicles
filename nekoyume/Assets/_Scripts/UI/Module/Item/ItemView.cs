using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ItemView<TViewModel> : MonoBehaviour
        where TViewModel : Model.Item
    {
        public TouchHandler touchHandler;
        public Button itemButton;
        public Image backgroundImage;
        public Image gradeImage;
        public Image iconImage;
        public TextMeshProUGUI enhancementText;
        public Image selectionImage;

        private readonly List<IDisposable> _disposablesAtSetData = new List<IDisposable>();

        public RectTransform RectTransform { get; private set; }

        public Vector3 CenterOffsetAsPosition
        {
            get
            {
                var pivotPosition = RectTransform.GetPivotPositionFromAnchor(PivotPresetType.MiddleCenter);
                var position = new Vector3(pivotPosition.x, pivotPosition.y);
                return RectTransform.localToWorldMatrix * position;
            }
        }

        [CanBeNull] public TViewModel Model { get; private set; }
        public bool IsEmpty => Model?.ItemBase.Value is null;

        public readonly Subject<ItemView<TViewModel>> OnClick = new Subject<ItemView<TViewModel>>();
        public readonly Subject<ItemView<TViewModel>> OnDoubleClick = new Subject<ItemView<TViewModel>>();

        #region Mono

        protected virtual void Awake()
        {
            RectTransform = GetComponent<RectTransform>();

            touchHandler.OnClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                OnClick.OnNext(this);
                Model?.OnClick.OnNext(Model);
            }).AddTo(gameObject);
            touchHandler.OnDoubleClick.Subscribe(_ =>
            {
                AudioController.PlayClick();
                OnDoubleClick.OnNext(this);
                Model?.OnDoubleClick.OnNext(Model);
            }).AddTo(gameObject);
        }

        protected virtual void OnDestroy()
        {
            Model?.Dispose();
            OnClick.Dispose();
            OnDoubleClick.Dispose();
            Clear();
        }

        #endregion
        
        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public virtual void SetData(TViewModel model)
        {
            if (model is null)
            {
                Clear();
                return;
            }
            
            _disposablesAtSetData.DisposeAllAndClear();
            Model = model;
            Model.GradeEnabled.SubscribeTo(gradeImage).AddTo(_disposablesAtSetData);
            Model.Enhancement.SubscribeTo(enhancementText).AddTo(_disposablesAtSetData);
            Model.EnhancementEnabled.SubscribeTo(enhancementText).AddTo(_disposablesAtSetData);
            Model.Dimmed.Subscribe(SetDim).AddTo(_disposablesAtSetData);
            Model.Selected.SubscribeTo(selectionImage).AddTo(_disposablesAtSetData);

            UpdateView();
        }

        public virtual void SetToUnknown()
        {
            Clear();
            iconImage.enabled = true;
            iconImage.overrideSprite = Resources.Load<Sprite>("UI/Textures/UI_icon_item_question");
            iconImage.SetNativeSize();
        }

        public virtual void Clear()
        {
            Model = null;
            _disposablesAtSetData.DisposeAllAndClear();

            UpdateView();
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        protected virtual void SetDim(bool isDim)
        {
            var alpha = isDim ? .3f : 1f;
            gradeImage.color = GetColor(gradeImage.color, alpha);
            iconImage.color = GetColor(gradeImage.color, alpha);
            enhancementText.color = GetColor(gradeImage.color, alpha);
            selectionImage.color = GetColor(gradeImage.color, alpha);
        }

        protected Color GetColor(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        private void UpdateView()
        {
            if (Model is null ||
                Model.ItemBase.Value is null)
            {
                gradeImage.enabled = false;
                iconImage.enabled = false;
                enhancementText.enabled = false;
                selectionImage.enabled = false;
                return;
            }

            var item = Model.ItemBase.Value;
            
            var gradeSprite = item.GetBackgroundSprite();
            if (gradeSprite is null)
                throw new FailedToLoadResourceException<Sprite>(item.Data.Grade.ToString());

            gradeImage.overrideSprite = gradeSprite;

            var itemSprite = item.GetIconSprite();
            if (itemSprite is null)
                throw new FailedToLoadResourceException<Sprite>(item.Data.Id.ToString());

            iconImage.enabled = true;
            iconImage.overrideSprite = itemSprite;
            iconImage.SetNativeSize();
        }
    }
}
