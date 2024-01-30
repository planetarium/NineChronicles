using System;
using System.Collections.Generic;
using Nekoyume.Model.Item;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    [RequireComponent(typeof(BaseItemView))]
    public class CollectionItemView : MonoBehaviour
    {
        [SerializeField]
        private BaseItemView baseItemView;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        public void Set(ItemBase itemBase, int level, int count)
        {
            if (itemBase == null)
            {
                return;
            }

            _disposables.DisposeAllAndClear();

            baseItemView.Container.SetActive(true);
            baseItemView.EmptyObject.SetActive(false);
            baseItemView.TouchHandler.gameObject.SetActive(false);
            baseItemView.MinusObject.gameObject.SetActive(false);

            var data = baseItemView.GetItemViewData(itemBase);
            baseItemView.GradeImage.overrideSprite = data.GradeBackground;
            baseItemView.GradeHsv.range = data.GradeHsvRange;
            baseItemView.GradeHsv.hue = data.GradeHsvHue;
            baseItemView.GradeHsv.saturation = data.GradeHsvSaturation;
            baseItemView.GradeHsv.value = data.GradeHsvValue;

            baseItemView.ItemImage.overrideSprite = BaseItemView.GetItemIcon(itemBase);
            baseItemView.EnhancementText.gameObject.SetActive(true);
            baseItemView.EnhancementText.text = level.ToString();
            baseItemView.CountText.gameObject.SetActive(true);
            baseItemView.CountText.text = count.ToString();
            baseItemView.OptionTag.gameObject.SetActive(true);
            baseItemView.OptionTag.Set(itemBase);
            baseItemView.EnoughObject.SetActive(false);
            baseItemView.TradableObject.SetActive(false);
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
        }
    }
}
