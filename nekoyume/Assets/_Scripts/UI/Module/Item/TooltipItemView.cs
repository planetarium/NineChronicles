using Nekoyume.Helper;
using Nekoyume.Model.Item;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    [RequireComponent(typeof(BaseItemView))]
    public class TooltipItemView : MonoBehaviour
    {
        [SerializeField]
        private BaseItemView baseItemView;

        public void Set(ItemBase itemBase, int count, bool levelLimit)
        {
            baseItemView.Container.SetActive(true);
            baseItemView.EmptyObject.SetActive(false);
            baseItemView.EnoughObject.SetActive(false);
            baseItemView.MinusObject.SetActive(false);
            baseItemView.FocusObject.SetActive(false);
            baseItemView.ExpiredObject.SetActive(false);
            baseItemView.TradableObject.SetActive(false);
            baseItemView.ElementalDisableObject.SetActive(false);
            baseItemView.SelectObject.SetActive(false);
            baseItemView.SelectBaseItemObject.SetActive(false);
            baseItemView.SelectMaterialItemObject.SetActive(false);
            baseItemView.LockObject.SetActive(false);
            baseItemView.ShadowObject.SetActive(false);
            baseItemView.PriceText.gameObject.SetActive(false);
            baseItemView.EquippedObject.SetActive(false);
            baseItemView.NotificationObject.SetActive(false);
            baseItemView.ItemGradeParticle.gameObject.SetActive(false);

            baseItemView.ItemImage.overrideSprite = baseItemView.GetItemIcon(itemBase);

            var data = baseItemView.GetItemViewData(itemBase);
            baseItemView.GradeImage.overrideSprite = data.GradeBackground;
            baseItemView.GradeHsv.range = data.GradeHsvRange;
            baseItemView.GradeHsv.hue = data.GradeHsvHue;
            baseItemView.GradeHsv.saturation = data.GradeHsvSaturation;
            baseItemView.GradeHsv.value = data.GradeHsvValue;

            if (itemBase is Equipment equipment && equipment.level > 0)
            {
                baseItemView.EnhancementText.gameObject.SetActive(true);
                baseItemView.EnhancementText.text = $"+{equipment.level}";
                if (equipment.level >= Util.VisibleEnhancementEffectLevel)
                {
                    baseItemView.EnhancementImage.material = data.EnhancementMaterial;
                    baseItemView.EnhancementImage.gameObject.SetActive(true);
                    baseItemView.ItemGradeParticle.gameObject.SetActive(true);
                    var mainModule = baseItemView.ItemGradeParticle.main;
                    mainModule.startColor = data.ItemGradeParticleColor;
                    baseItemView.ItemGradeParticle.Play();
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

            baseItemView.OptionTag.Set(itemBase);
            baseItemView.CountText.gameObject.SetActive(count > 0 &&
                                                        itemBase.ItemType == ItemType.Material);
            baseItemView.CountText.text = count.ToString();

            baseItemView.LevelLimitObject.SetActive(levelLimit);
        }
    }
}
