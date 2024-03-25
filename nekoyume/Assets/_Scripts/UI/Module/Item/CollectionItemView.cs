using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
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

        [SerializeField]
        private Animator animator;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();
        private static readonly int Register = Animator.StringToHash("Register");

        public void Set(CollectionMaterial model, Action<CollectionMaterial> onClick)
        {
            _disposables.DisposeAllAndClear();

            baseItemView.Container.SetActive(true);
            baseItemView.EmptyObject.SetActive(false);
            baseItemView.MinusObject.gameObject.SetActive(false);

            var data = baseItemView.GetItemViewData(model.Grade);
            baseItemView.GradeImage.overrideSprite = data.GradeBackground;
            baseItemView.GradeHsv.range = data.GradeHsvRange;
            baseItemView.GradeHsv.hue = data.GradeHsvHue;
            baseItemView.GradeHsv.saturation = data.GradeHsvSaturation;
            baseItemView.GradeHsv.value = data.GradeHsvValue;

            baseItemView.TouchHandler.gameObject.SetActive(true);
            baseItemView.TouchHandler.OnClick.Select(_ => model).Subscribe(onClick).AddTo(_disposables);
            baseItemView.ItemImage.overrideSprite = SpriteHelper.GetItemIcon(model.Row.ItemId);

            var required = model.HasItem && !model.IsEnoughAmount;
            baseItemView.EnhancementText.gameObject.SetActive(model.ItemType == ItemType.Equipment);
            var level = model.Row.Level;
            baseItemView.EnhancementText.text = level > 0 ? $"+{level}" : string.Empty;
            baseItemView.EnhancementText.color = required ? requiredColor : Color.white;
            baseItemView.EnhancementText.enableVertexGradient = !required;

            baseItemView.CountText.gameObject.SetActive(model.ItemType == ItemType.Consumable ||
                                                        model.ItemType == ItemType.Material);
            baseItemView.CountText.text = model.Row.Count.ToString();
            baseItemView.CountText.color = required ? requiredColor : Color.white;

            baseItemView.OptionTag.gameObject.SetActive(model.Row.SkillContains);
            if (model.Row.SkillContains)
            {
                baseItemView.OptionTag.Set(model.Grade);
            }

            baseItemView.EnoughObject.SetActive(model.Enough);
            baseItemView.TradableObject.SetActive(!model.HasItem);

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
                .Subscribe(b => baseItemView.SelectCollectionObject.SetActive(b))
                .AddTo(_disposables);
            model.Focused
                .Subscribe(b => baseItemView.SelectArrowObject.SetActive(b))
                .AddTo(_disposables);
            model.Registered
                .Subscribe(_ => baseItemView.EnoughObject.SetActive(model.Enough))
                .AddTo(_disposables);
            model.Registered.Where(b => b)
                .Subscribe(_ => animator.SetTrigger(Register))
                .AddTo(_disposables);
        }
    }
}
