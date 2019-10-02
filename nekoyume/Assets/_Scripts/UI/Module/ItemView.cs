using System;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using Nekoyume.Helper;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class ItemView<T> : MonoBehaviour where T : Model.Item
    {
        protected static readonly Color DefaultColor = Color.white;
        protected static readonly Color DimColor = new Color(1f, 1f, 1f, 0.3f);

        public Image iconImage;
        public Image gradeImage;

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

        public T Model { get; private set; }

        #region Mono
        
        protected virtual void Awake()
        {
            this.ComponentFieldsNotNullTest();
            
            RectTransform = GetComponent<RectTransform>();
        }

        protected virtual void OnDestroy()
        {
            Clear();
        }

        #endregion

        public virtual void SetData(T model)
        {
            if (ReferenceEquals(model, null))
            {
                Clear();
                return;
            }

            Model = model;

            UpdateView();
        }

        public virtual void Clear()
        {
            Model = null;

            UpdateView();
        }

        protected virtual void SetDim(bool isDim)
        {
            iconImage.color = isDim ? DimColor : DefaultColor;
            if (gradeImage) gradeImage.color = isDim ? DimColor : DefaultColor;
        }

        private void UpdateView()
        {
            if (Model is null ||
                Model.ItemBase.Value is null)
            {
                iconImage.enabled = false;
                if (gradeImage) gradeImage.enabled = false;
                return;
            }

            var item = Model.ItemBase.Value;

            var itemSprite = item.GetIconSprite();
            if (itemSprite is null)
            {
                throw new FailedToLoadResourceException<Sprite>(item.Data.Id.ToString());
            }

            iconImage.enabled = true;
            iconImage.overrideSprite = itemSprite;
            iconImage.SetNativeSize();

            var gradeSprite = item.GetBackgroundSprite();
            if (gradeSprite is null)
            {
                throw new FailedToLoadResourceException<Sprite>(item.Data.Grade.ToString());
            }

            if (gradeImage)
            {
                gradeImage.enabled = true;
                gradeImage.overrideSprite = gradeSprite;
            }
        }
    }
}
