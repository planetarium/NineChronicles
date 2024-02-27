using System;
using System.Collections.Generic;
using System.Globalization;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.UI.Module;
using UnityEngine;

namespace Nekoyume
{
    [RequireComponent(typeof(BaseItemView))]
    public class MailRewardItemView : MonoBehaviour
    {
        [SerializeField]
        private BaseItemView baseItemView;

        [SerializeField]
        private GameObject effect;

        private readonly List<IDisposable> _disposables = new();

        public void Set(MailReward mailReward)
        {
            _disposables.DisposeAllAndClear();
            baseItemView.Container.SetActive(true);
            baseItemView.EmptyObject.SetActive(false);
            baseItemView.EnoughObject.SetActive(false);
            baseItemView.MinusObject.SetActive(false);
            baseItemView.ExpiredObject.SetActive(false);
            baseItemView.SelectBaseItemObject.SetActive(false);
            baseItemView.SelectMaterialItemObject.SetActive(false);
            baseItemView.LockObject.SetActive(false);
            baseItemView.ShadowObject.SetActive(false);
            baseItemView.PriceText.gameObject.SetActive(false);
            baseItemView.LoadingObject.SetActive(false);
            baseItemView.EquippedObject.SetActive(false);
            baseItemView.DimObject.SetActive(false);
            baseItemView.TradableObject.SetActive(false);
            baseItemView.SelectObject.SetActive(false);
            baseItemView.FocusObject.SetActive(false);
            baseItemView.NotificationObject.SetActive(false);
            baseItemView.GrindingCountObject.SetActive((false));
            baseItemView.LevelLimitObject.SetActive(false);
            baseItemView.RuneNotificationObj.SetActiveSafe(false);
            baseItemView.RuneSelectMove.SetActive(false);
            baseItemView.SelectCollectionObject.SetActive(false);
            baseItemView.SelectArrowObject.SetActive(false);

            if (mailReward.ItemBase is not null)
            {
                baseItemView.ItemImage.overrideSprite =
                    BaseItemView.GetItemIcon(mailReward.ItemBase);

                var data = baseItemView.GetItemViewData(mailReward.ItemBase);
                baseItemView.GradeImage.overrideSprite = data.GradeBackground;
                baseItemView.GradeHsv.range = data.GradeHsvRange;
                baseItemView.GradeHsv.hue = data.GradeHsvHue;
                baseItemView.GradeHsv.saturation = data.GradeHsvSaturation;
                baseItemView.GradeHsv.value = data.GradeHsvValue;

                if (mailReward.ItemBase is Equipment { level: > 0 } equipment)
                {
                    baseItemView.EnhancementText.gameObject.SetActive(true);
                    baseItemView.EnhancementText.text = $"+{equipment.level}";
                    if (equipment.level >= Util.VisibleEnhancementEffectLevel)
                    {
                        baseItemView.EnhancementImage.material = data.EnhancementMaterial;
                        baseItemView.EnhancementImage.gameObject.SetActive(true);
                    }
                    else
                    {
                        baseItemView.EnhancementImage.gameObject.SetActive(false);
                    }
                }
                else
                {
                    baseItemView.EnhancementText.gameObject.SetActive(false);
                    baseItemView.EnhancementImage.gameObject.SetActive(false);
                }

                baseItemView.OptionTag.gameObject.SetActive(true);
                baseItemView.OptionTag.Set(mailReward.ItemBase);

                baseItemView.CountText.gameObject.SetActive(
                    mailReward.ItemBase.ItemType == ItemType.Material);
                baseItemView.CountText.text = mailReward.Count.ToString();
            }
            else
            {
                var fav = mailReward.FavFungibleAssetValue;
                var grade = Util.GetTickerGrade(fav.Currency.Ticker);
                baseItemView.ItemImage.overrideSprite = fav.GetIconSprite();

                var data = baseItemView.GetItemViewData(grade);
                baseItemView.GradeImage.overrideSprite = data.GradeBackground;
                baseItemView.GradeHsv.range = data.GradeHsvRange;
                baseItemView.GradeHsv.hue = data.GradeHsvHue;
                baseItemView.GradeHsv.saturation = data.GradeHsvSaturation;
                baseItemView.GradeHsv.value = data.GradeHsvValue;

                baseItemView.EnhancementText.gameObject.SetActive(false);
                baseItemView.EnhancementImage.gameObject.SetActive(false);
                baseItemView.OptionTag.gameObject.SetActive(false);
                baseItemView.CountText.text = mailReward.FavFungibleAssetValue.ToCurrencyNotation();
            }

            effect.SetActive(false);
        }

        public void ShowEffect()
        {
            effect.SetActive(true);
        }
    }
}
