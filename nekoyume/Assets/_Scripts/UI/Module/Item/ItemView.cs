using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Nekoyume.EnumType;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.TableData;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Coffee.UIEffects;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.Helper;
using Nekoyume.Model.Item;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class ItemView<TViewModel> : VanillaItemView
        where TViewModel : Model.Item
    {
        public TouchHandler touchHandler;
        public Button itemButton;
        public Image backgroundImage;
        public TextMeshProUGUI enhancementText;
        public GameObject enhancementImage;

        [SerializeField]
        private GameObject selection;

        [SerializeField]
        private GameObject disable;

        [SerializeField]
        protected UIHsvModifier optionTagBg = null;

        [SerializeField]
        protected List<Image> optionTagImages = null;

        [SerializeField]
        protected OptionTagDataScriptableObject optionTagData = null;

        private readonly List<IDisposable> _disposablesAtSetData = new List<IDisposable>();

        public RectTransform RectTransform { get; private set; }

        public Vector3 CenterOffsetAsPosition
        {
            get
            {
                var pivotPosition =
                    RectTransform.GetPivotPositionFromAnchor(PivotPresetType.MiddleCenter);
                var position = new Vector3(pivotPosition.x, pivotPosition.y);
                return RectTransform.localToWorldMatrix * position;
            }
        }

        [CanBeNull]
        public TViewModel Model { get; private set; }

        public bool IsEmpty => Model?.ItemBase.Value is null;

        public readonly Subject<ItemView<TViewModel>> OnClick = new Subject<ItemView<TViewModel>>();

        public readonly Subject<ItemView<TViewModel>> OnDoubleClick =
            new Subject<ItemView<TViewModel>>();

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

            selection.transform.SetAsLastSibling();
            disable.transform.SetAsLastSibling();
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
                throw new ArgumentOutOfRangeException(nameof(ItemSheet.Row),
                    model.ItemBase.Value.Id, null);
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
            Model.HasOptions.Subscribe(hasOptions => SetOptionTag(hasOptions, tagData))
                .AddTo(_disposablesAtSetData);
            Model.Dimmed.SubscribeTo(disable).AddTo(_disposablesAtSetData);
            Model.Selected.SubscribeTo(selection).AddTo(_disposablesAtSetData);
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
                throw new ArgumentOutOfRangeException(nameof(ItemSheet.Row),
                    model.ItemBase.Value.Id, null);
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
            Model.HasOptions.Subscribe(hasOptions => SetOptionTag(hasOptions, tagData))
                .AddTo(_disposablesAtSetData);
            Model.Selected.SubscribeTo(selection).AddTo(_disposablesAtSetData);

            UpdateView();
        }

        public void SetDataExceptOptionTag(TViewModel model)
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
                throw new ArgumentOutOfRangeException(nameof(ItemSheet.Row),
                    model.ItemBase.Value.Id, null);
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
            Model.Dimmed.SubscribeTo(disable).AddTo(_disposablesAtSetData);
            Model.Selected.SubscribeTo(selection).AddTo(_disposablesAtSetData);
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

        private void UpdateView()
        {
            if (Model is null ||
                Model.ItemBase.Value is null)
            {
                enhancementImage.SetActive(false);
                enhancementText.enabled = false;
                selection.SetActive(false);
                optionTagBg.gameObject.SetActive(false);
            }
        }

        protected void SetOptionTag(bool hasOptions, OptionTagData data)
        {
            if (!hasOptions)
            {
                optionTagBg.gameObject.SetActive(false);
                return;
            }

            if (Model is null)
            {
                return;
            }

            foreach (var image in optionTagImages)
            {
                image.gameObject.SetActive(false);
            }

            optionTagBg.range = data.GradeHsvRange;
            optionTagBg.hue = data.GradeHsvHue;
            optionTagBg.saturation = data.GradeHsvSaturation;
            optionTagBg.value = data.GradeHsvValue;
            var optionInfo = new ItemOptionInfo(Model.ItemBase.Value as Equipment);

            var optionCount = optionInfo.StatOptions.Sum(x => x.count);
            var index = 0;
            for (var i = 0; i < optionCount; ++i)
            {
                var image = optionTagImages[index];
                image.gameObject.SetActive(true);
                image.sprite = optionTagData.StatOptionSprite;
                ++index;
            }

            for (var i = 0; i < optionInfo.SkillOptions.Count; ++i)
            {
                var image = optionTagImages[index];
                image.gameObject.SetActive(true);
                image.sprite = optionTagData.SkillOptionSprite;
                ++index;
            }

            optionTagBg.gameObject.SetActive(true);
        }
    }
}
