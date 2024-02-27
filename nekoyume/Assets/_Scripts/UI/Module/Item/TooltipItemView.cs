using Nekoyume.Helper;
using Nekoyume.Model.Item;
using System.Collections.Generic;
using Coffee.UIEffects;
using Nekoyume.TableData;
using Spine.Unity;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    [RequireComponent(typeof(BaseItemView))]
    public class TooltipItemView : MonoBehaviour
    {
        [SerializeField]
        private SpineTooltipDataScriptableObject tooltipDataScriptableObject;

        [SerializeField]
        private BaseItemView baseItemView;

        [SerializeField]
        private ParticleSystem gradeEffect;

        [SerializeField]
        private Color levelLimitedItemColor;

        [SerializeField]
        private UIShiny uiShiny;

        private GameObject _costumeSpineObject;

        private Dictionary<int, GameObject> _skeletonPool;

        private void OnDisable()
        {
            if (_costumeSpineObject)
            {
                _costumeSpineObject.SetActive(false);
            }
        }

        public void Set(ItemBase itemBase, int count, bool levelLimit)
        {
            InitTooltipItemView();

            baseItemView.ItemImage.overrideSprite = BaseItemView.GetItemIcon(itemBase);

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
            else if (itemBase is Costume costume)
            {
                baseItemView.EnhancementText.gameObject.SetActive(false);
                baseItemView.EnhancementImage.gameObject.SetActive(false);
                var tooltipData = tooltipDataScriptableObject.GetSpineTooltipData(costume.Id);
                if (tooltipData != null)
                {
                    if (_costumeSpineObject)
                    {
                        _costumeSpineObject.SetActive(false);
                    }

                    if (_skeletonPool is null)
                    {
                        CreateSkeletonPool();
                    }

                    if (_skeletonPool.TryGetValue(tooltipData.ResourceID, out var skeleton))
                    {
                        _costumeSpineObject = skeleton;
                        _costumeSpineObject.transform.localPosition = tooltipData.Position;
                        _costumeSpineObject.transform.localScale = tooltipData.Scale;
                        _costumeSpineObject.transform.rotation = Quaternion.Euler(tooltipData.Rotation);
                        var particle = gradeEffect.main;
                        particle.startColor = tooltipData.GradeColor;
                        _costumeSpineObject.SetActive(true);

                        if(_costumeSpineObject.TryGetComponent(out SkeletonGraphic skeletonGraphic))
                        {
                            var currentAnim = skeletonGraphic.AnimationState.GetCurrent(0).Animation.Name;
                            var isLoop = skeletonGraphic.AnimationState.GetCurrent(0).Loop;
                            skeletonGraphic.AnimationState.SetAnimation(0, currentAnim, isLoop);
                        }

                        baseItemView.ItemImage.gameObject.SetActive(false);
                        baseItemView.SpineItemImage.gameObject.SetActive(true);
                    }
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
            baseItemView.ItemImage.color = levelLimit ? levelLimitedItemColor : Color.white;
            uiShiny.Play();
        }

        public void Set(ItemSheet.Row row, CollectionSheet.RequiredMaterial material)
        {
            InitTooltipItemView();
            baseItemView.OptionTag.gameObject.SetActive(false);
            baseItemView.CountText.gameObject.SetActive(false);
            baseItemView.LevelLimitObject.SetActive(false);

            baseItemView.ItemImage.overrideSprite = SpriteHelper.GetItemIcon(row.Id);

            var data = baseItemView.GetItemViewData(row.Grade);
            baseItemView.GradeImage.overrideSprite = data.GradeBackground;
            baseItemView.GradeHsv.range = data.GradeHsvRange;
            baseItemView.GradeHsv.hue = data.GradeHsvHue;
            baseItemView.GradeHsv.saturation = data.GradeHsvSaturation;
            baseItemView.GradeHsv.value = data.GradeHsvValue;

            if (row.ItemType == ItemType.Equipment)
            {
                if (material.Level > 0)
                {
                    baseItemView.EnhancementText.gameObject.SetActive(true);
                    baseItemView.EnhancementText.text = $"+{material.Level}";
                }

                if (material.Level >= Util.VisibleEnhancementEffectLevel)
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
            else if (row.ItemType == ItemType.Equipment)
            {
                baseItemView.EnhancementText.gameObject.SetActive(false);
                baseItemView.EnhancementImage.gameObject.SetActive(false);
                var tooltipData = tooltipDataScriptableObject.GetSpineTooltipData(row.Id);
                if (tooltipData != null)
                {
                    if (_costumeSpineObject)
                    {
                        _costumeSpineObject.SetActive(false);
                    }

                    if (_skeletonPool is null)
                    {
                        CreateSkeletonPool();
                    }

                    if (_skeletonPool.TryGetValue(tooltipData.ResourceID, out var skeleton))
                    {
                        _costumeSpineObject = skeleton;
                        _costumeSpineObject.transform.localPosition = tooltipData.Position;
                        _costumeSpineObject.transform.localScale = tooltipData.Scale;
                        _costumeSpineObject.transform.rotation = Quaternion.Euler(tooltipData.Rotation);
                        var particle = gradeEffect.main;
                        particle.startColor = tooltipData.GradeColor;
                        _costumeSpineObject.SetActive(true);

                        baseItemView.ItemImage.gameObject.SetActive(false);
                        baseItemView.SpineItemImage.gameObject.SetActive(true);
                    }
                }
            }
            else
            {
                baseItemView.EnhancementText.gameObject.SetActive(false);
                baseItemView.EnhancementImage.gameObject.SetActive(false);
            }

            uiShiny.Play();
        }

        private void InitTooltipItemView()
        {
            baseItemView.Container.SetActive(true);
            baseItemView.EmptyObject.SetActive(false);
            baseItemView.EnoughObject.SetActive(false);
            baseItemView.MinusObject.SetActive(false);
            baseItemView.FocusObject.SetActive(false);
            baseItemView.ExpiredObject.SetActive(false);
            baseItemView.TradableObject.SetActive(false);
            baseItemView.DimObject.SetActive(false);
            baseItemView.SelectObject.SetActive(false);
            baseItemView.SelectBaseItemObject.SetActive(false);
            baseItemView.SelectMaterialItemObject.SetActive(false);
            baseItemView.LockObject.SetActive(false);
            baseItemView.ShadowObject.SetActive(false);
            baseItemView.PriceText.gameObject.SetActive(false);
            baseItemView.EquippedObject.SetActive(false);
            baseItemView.NotificationObject.SetActive(false);
            baseItemView.ItemGradeParticle.gameObject.SetActive(false);
            baseItemView.ItemImage.gameObject.SetActive(true);
            baseItemView.SpineItemImage.gameObject.SetActive(false);
            baseItemView.LoadingObject.SetActive(false);
            baseItemView.GrindingCountObject.SetActive(false);
            baseItemView.RuneNotificationObj.SetActiveSafe(false);
            baseItemView.RuneSelectMove.SetActive(false);
            baseItemView.SelectCollectionObject.SetActive(false);
            baseItemView.SelectArrowObject.SetActive(false);
        }

        private void CreateSkeletonPool()
        {
            _skeletonPool = new Dictionary<int, GameObject>();

            foreach (var data in tooltipDataScriptableObject.Datas)
            {
                var go = Instantiate(data.Prefab, baseItemView.SpineItemImage.transform);
                _skeletonPool.Add(data.ResourceID, go);
                go.SetActive(false);
            }
        }
    }
}
