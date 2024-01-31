using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.UI.Model;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using UniRx;

    [RequireComponent(typeof(BaseItemView))]
    public class CollectionItemView : MonoBehaviour
    {
        [SerializeField]
        private BaseItemView baseItemView;

        [SerializeField]
        private Color requiredColor;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Set(CollectionMaterial model, Action<CollectionMaterial> onClick)
        {
            _disposables.DisposeAllAndClear();

            baseItemView.Container.SetActive(true);
            baseItemView.EmptyObject.SetActive(false);
            baseItemView.MinusObject.gameObject.SetActive(false);

            var data = baseItemView.GetItemViewData(model.Row.Grade);
            baseItemView.GradeImage.overrideSprite = data.GradeBackground;
            baseItemView.GradeHsv.range = data.GradeHsvRange;
            baseItemView.GradeHsv.hue = data.GradeHsvHue;
            baseItemView.GradeHsv.saturation = data.GradeHsvSaturation;
            baseItemView.GradeHsv.value = data.GradeHsvValue;

            baseItemView.TouchHandler.gameObject.SetActive(true);
            baseItemView.EnhancementText.gameObject.SetActive(true);
            baseItemView.CountText.gameObject.SetActive(true);
            baseItemView.OptionTag.gameObject.SetActive(false);

            baseItemView.TouchHandler.OnClick.Select(_ => model).Subscribe(onClick).AddTo(_disposables);
            baseItemView.ItemImage.overrideSprite = SpriteHelper.GetItemIcon(model.Row.Id);
            baseItemView.EnhancementText.text = model.Level.ToString();
            baseItemView.EnhancementText.color = model.EnoughLevel ? Color.white : requiredColor;
            baseItemView.EnhancementText.enableVertexGradient = model.EnoughLevel;
            baseItemView.CountText.text = model.Count.ToString();
            baseItemView.CountText.color = model.EnoughCount ? Color.white : requiredColor;
            // baseItemView.OptionTag.Set(itemBase);

            baseItemView.EnoughObject.SetActive(model.HasItem && (model.EnoughLevel || model.EnoughCount));
            baseItemView.TradableObject.SetActive(!model.HasItem);
            baseItemView.SelectCollectionObject.SetActive(false);
            baseItemView.SelectArrowObject.SetActive(false);

            baseItemView.SpineItemImage.gameObject.SetActive(false);
            baseItemView.EnhancementImage.gameObject.SetActive(false);
            baseItemView.EquippedObject.SetActive(false);
            baseItemView.PriceText.gameObject.SetActive(false);
            baseItemView.NotificationObject.SetActive(false);
            baseItemView.MinusObject.SetActive(false);
            baseItemView.FocusObject.SetActive(false);
            baseItemView.ExpiredObject.SetActive(false);
            baseItemView.DimObject.SetActive(false);
            baseItemView.LevelLimitObject.SetActive(false);
            baseItemView.SelectObject.SetActive(false);
            baseItemView.SelectBaseItemObject.SetActive(false);
            baseItemView.SelectMaterialItemObject.SetActive(false);
            baseItemView.LockObject.SetActive(false);
            baseItemView.ShadowObject.SetActive(false);
            baseItemView.LoadingObject.SetActive(false);
            baseItemView.GrindingCountObject.SetActive(false);
            baseItemView.RuneNotificationObj.SetActive(false);
            baseItemView.RuneSelectMove.SetActive(false);

            model.Selected
                .Subscribe(b => baseItemView.SelectCollectionObject.SetActive(b)).AddTo(_disposables);
        }
    }
}
