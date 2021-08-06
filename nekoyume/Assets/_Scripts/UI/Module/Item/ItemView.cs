using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.TableData;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using Coffee.UIEffects;
    using Nekoyume.Game.ScriptableObject;
    using UniRx;

    public class ItemView<TViewModel> : VanillaItemView
        where TViewModel : Model.Item
    {
        public TouchHandler touchHandler;
        public Button itemButton;
        public Image backgroundImage;
        public TextMeshProUGUI enhancementText;
        public GameObject enhancementImage;
        public Image selectionImage;
        public Image dimmedImage;

        [SerializeField]
        protected UIHsvModifier optionTagBg = null;

        [SerializeField]
        protected TextMeshProUGUI optionTagText = null;

        [SerializeField]
        protected OptionTagDataScriptableObject optionTagData = null;

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
        }

        #endregion

        public virtual void SetData(TViewModel model)
        {
            if (model is null)
            {
                Clear();
                return;
            }

            ItemSheet.Row row;

            row = Game.Game.instance.TableSheets.ItemSheet.Values
                    .FirstOrDefault(r => r.Id == model.ItemBase.Value.Id);

            if (row is null)
            {
                throw new ArgumentOutOfRangeException(nameof(ItemSheet.Row), model.ItemBase.Value.Id, null);
            }
            base.SetData(row);

            var viewData = base.itemViewData.GetItemViewData(row.Grade);
            enhancementImage.GetComponent<Image>().material = viewData.EnhancementMaterial;

            _disposablesAtSetData.DisposeAllAndClear();
            Model = model;
            Model.GradeEnabled.SubscribeTo(gradeImage).AddTo(_disposablesAtSetData);
            Model.Enhancement.SubscribeTo(enhancementText).AddTo(_disposablesAtSetData);
            Model.EnhancementEnabled.SubscribeTo(enhancementText).AddTo(_disposablesAtSetData);
            Model.EnhancementEffectEnabled
                .Subscribe(x => enhancementImage.gameObject.SetActive(x))
                .AddTo(_disposablesAtSetData);
            var tagData = optionTagData.GetOptionTagData(row.Grade);
            Model.Options.Subscribe(count => SetOptionTag(count, tagData)).AddTo(_disposablesAtSetData);
            Model.Dimmed.Subscribe(SetDim).AddTo(_disposablesAtSetData);
            if (dimmedImage != null)
            {
                Model.Dimmed.SubscribeTo(dimmedImage.gameObject).AddTo(_disposablesAtSetData);
            }

            Model.Selected.SubscribeTo(selectionImage.gameObject).AddTo(_disposablesAtSetData);
            UpdateView();
        }

        public void SetData(TViewModel model, bool isConsumable)
        {
            if (model is null)
            {
                Clear();
                return;
            }

            ItemSheet.Row row;

            if (isConsumable)
            {
                row = Game.Game.instance.TableSheets.ConsumableItemSheet.Values
                    .FirstOrDefault(r => r.Id == model.ItemBase.Value.Id);
            }
            else
            {
                row = Game.Game.instance.TableSheets.EquipmentItemSheet.Values
                    .FirstOrDefault(r => r.Id == model.ItemBase.Value.Id);
            }

            if (row is null)
            {
                throw new ArgumentOutOfRangeException(nameof(ItemSheet.Row), model.ItemBase.Value.Id, null);
            }
            base.SetData(row);

            var viewData = itemViewData.GetItemViewData(row.Grade);
            _disposablesAtSetData.DisposeAllAndClear();
            Model = model;
            Model.GradeEnabled.SubscribeTo(gradeImage).AddTo(_disposablesAtSetData);
            Model.Enhancement.SubscribeTo(enhancementText).AddTo(_disposablesAtSetData);
            Model.EnhancementEnabled.SubscribeTo(enhancementText).AddTo(_disposablesAtSetData);
            Model.EnhancementEffectEnabled
                .Subscribe(x => enhancementImage.gameObject.SetActive(x))
                .AddTo(_disposablesAtSetData);
            var tagData = optionTagData.GetOptionTagData(row.Grade);
            Model.Options.Subscribe(count => SetOptionTag(count, tagData)).AddTo(_disposablesAtSetData);
            Model.Dimmed.Subscribe(SetDim).AddTo(_disposablesAtSetData);
            if (dimmedImage != null)
            {
                Model.Dimmed.SubscribeTo(dimmedImage).AddTo(_disposablesAtSetData);
            }
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

        public override void Clear()
        {
            Model = null;
            _disposablesAtSetData.DisposeAllAndClear();
            UpdateView();
            base.Clear();
        }

        protected override void SetDim(bool isDim)
        {
            base.SetDim(isDim);
            enhancementText.color = isDim ? DimmedColor : OriginColor;
            selectionImage.color = isDim ? DimmedColor : OriginColor;
        }

        private void UpdateView()
        {
            if (Model is null ||
                Model.ItemBase.Value is null)
            {
                enhancementText.enabled = false;
                if (selectionImage != null)
                {
                    selectionImage.enabled = false;
                }

                optionTagBg.gameObject.SetActive(false);
            }
        }

        protected void SetOptionTag(int count, OptionTagData optionViewData)
        {
            optionTagBg.gameObject.SetActive(false);
            if (Model is null)
            {
                return;
            }

            var itemBase = Model.ItemBase.Value;
            if (itemBase.TryGetOptionTagText(out var text))
            {
                optionTagBg.range = optionViewData.GradeHsvRange;
                optionTagBg.hue = optionViewData.GradeHsvHue;
                optionTagBg.saturation = optionViewData.GradeHsvSaturation;
                optionTagBg.value = optionViewData.GradeHsvValue;
                optionTagText.text = text;
                optionTagBg.gameObject.SetActive(true);
            }
        }
    }
}
