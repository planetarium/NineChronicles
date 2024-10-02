using System;
using System.Collections.Generic;
using Nekoyume.Model.Item;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using Helper;
    using UniRx;

    [RequireComponent(typeof(BaseItemView))]
    public class SweepItemView : MonoBehaviour
    {
        [SerializeField]
        private BaseItemView baseItemView;

        private readonly List<IDisposable> _disposables = new();

        public void Set(ItemBase itemBase, int count)
        {
            if (itemBase == null)
            {
                return;
            }

            _disposables.DisposeAllAndClear();

            baseItemView.ClearItem();
            baseItemView.TouchHandler.gameObject.SetActive(false);
            baseItemView.MinusObject.gameObject.SetActive(false);

            var data = baseItemView.GetItemViewData(itemBase);
            baseItemView.GradeImage.overrideSprite = data.GradeBackground;
            baseItemView.GradeHsv.range = data.GradeHsvRange;
            baseItemView.GradeHsv.hue = data.GradeHsvHue;
            baseItemView.GradeHsv.saturation = data.GradeHsvSaturation;
            baseItemView.GradeHsv.value = data.GradeHsvValue;

            baseItemView.ItemImage.overrideSprite = BaseItemView.GetItemIcon(itemBase);
            baseItemView.SpineItemImage.gameObject.SetActive(false);
            baseItemView.EnhancementImage.gameObject.SetActive(false);
            baseItemView.EnhancementText.gameObject.SetActive(false);
            baseItemView.CountText.text = count.ToString();
            baseItemView.PriceText.gameObject.SetActive(false);
            baseItemView.OptionTag.gameObject.SetActive(false);
            baseItemView.RuneSelectMove.SetActive(false);
            baseItemView.SelectCollectionObject.SetActive(false);
            baseItemView.SelectArrowObject.SetActive(false);

            if (itemBase is Equipment equipmentItem)
            {
                baseItemView.CustomCraftArea.SetActive(equipmentItem.ByCustomCraft);
            }
        }
    }
}
