using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using EnhancedUI.EnhancedScroller;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class Inventory : MonoBehaviour
    {
        private static readonly Vector2 BtnHighlightSize = new Vector2(157f, 60f);
        private static readonly Vector2 BtnSize = new Vector2(130f, 36f);

        public TextMeshProUGUI titleText;
        public Button equipmentsButton;
        public Image equipmentsButtonImage;
        public Image equipmentsButtonIconImage;
        public TextMeshProUGUI equipmentsButtonText;
        public TextMeshProUGUI equipmentsButtonSelectedText;
        public Button consumablesButton;
        public Image consumablesButtonImage;
        public Image consumablesButtonIconImage;
        public TextMeshProUGUI consumablesButtonText;
        public TextMeshProUGUI consumablesButtonSelectedText;
        public Button materialsButton;
        public Image materialsButtonImage;
        public Image materialsButtonIconImage;
        public TextMeshProUGUI materialsButtonText;
        public TextMeshProUGUI materialsButtonSelectedText;
        public InventoryScrollerController scrollerController;

        private Sprite _selectedButtonSprite;
        private Sprite _deselectedButtonSprite;
        private Sprite _equipmentsButtonIconSpriteBlack;
        private Sprite _equipmentsButtonIconSpriteBlue;
        private Sprite _consumablesButtonIconSpriteBlack;
        private Sprite _consumablesButtonIconSpriteBlue;
        private Sprite _materialsButtonIconSpriteBlack;
        private Sprite _materialsButtonIconSpriteBlue;

        // todo: 분리..
        private ItemInformationTooltip _tooltip;

        private readonly Dictionary<ItemType, RectTransform> _switchButtonTransforms =
            new Dictionary<ItemType, RectTransform>(ItemTypeComparer.Instance);

        private readonly List<IDisposable> _disposablesAtOnEnable = new List<IDisposable>();

        public RectTransform RectTransform { get; private set; }

        // todo: 분리..
        public ItemInformationTooltip Tooltip => _tooltip
            ? _tooltip
            : _tooltip = Widget.Find<ItemInformationTooltip>();

        public Model.Inventory SharedModel { get; private set; }
        
        public readonly Subject<Inventory> OnResetItems = new Subject<Inventory>();

        #region Mono

        protected void Awake()
        {
            _selectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_yellow_02");
            _deselectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_brown_01");
            _equipmentsButtonIconSpriteBlack = Resources.Load<Sprite>("UI/Textures/icon_inventory_01_black");
            _equipmentsButtonIconSpriteBlue = Resources.Load<Sprite>("UI/Textures/icon_inventory_01_yellow");
            _consumablesButtonIconSpriteBlack = Resources.Load<Sprite>("UI/Textures/icon_inventory_02_black");
            _consumablesButtonIconSpriteBlue = Resources.Load<Sprite>("UI/Textures/icon_inventory_02_yellow");
            _materialsButtonIconSpriteBlack = Resources.Load<Sprite>("UI/Textures/icon_inventory_03_black");
            _materialsButtonIconSpriteBlue = Resources.Load<Sprite>("UI/Textures/icon_inventory_03_yellow");
            _switchButtonTransforms.Add(ItemType.Consumable, consumablesButton.GetComponent<RectTransform>());
            _switchButtonTransforms.Add(ItemType.Equipment, equipmentsButton.GetComponent<RectTransform>());
            _switchButtonTransforms.Add(ItemType.Material, materialsButton.GetComponent<RectTransform>());

            titleText.text = LocalizationManager.Localize("UI_INVENTORY");
            equipmentsButtonText.text = LocalizationManager.Localize("UI_EQUIPMENTS");
            equipmentsButtonSelectedText.text = LocalizationManager.Localize("UI_EQUIPMENTS");
            consumablesButtonText.text = LocalizationManager.Localize("UI_CONSUMABLES");
            consumablesButtonSelectedText.text = LocalizationManager.Localize("UI_CONSUMABLES");
            materialsButtonText.text = LocalizationManager.Localize("UI_MATERIALS");
            materialsButtonSelectedText.text = LocalizationManager.Localize("UI_MATERIALS");

            RectTransform = GetComponent<RectTransform>();

            SharedModel = new Model.Inventory();
            SharedModel.State.Subscribe(SubscribeState).AddTo(gameObject);
            SharedModel.SelectedItemView.Subscribe(SubscribeSelectedItemView).AddTo(gameObject);
            
            equipmentsButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                SharedModel.State.Value = ItemType.Equipment;
            }).AddTo(gameObject);
            consumablesButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                SharedModel.State.Value = ItemType.Consumable;
            }).AddTo(gameObject);
            materialsButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                SharedModel.State.Value = ItemType.Material;
            }).AddTo(gameObject);
        }

        private void OnEnable()
        {
            ReactiveAvatarState.Inventory.Subscribe(value =>
                {
                    scrollerController.DisposeAddedAtSetData();
                    SharedModel.ResetItems(value);
                    OnResetItems.OnNext(this);
                })
                .AddTo(_disposablesAtOnEnable);
        }

        private void OnDisable()
        {
            _disposablesAtOnEnable.DisposeAllAndClear();
            Tooltip.Close();
        }

        private void OnDestroy()
        {
            SharedModel.Dispose();
            SharedModel = null;
            OnResetItems.Dispose();
        }

        #endregion
        #region Subscribe

        private void SubscribeState(ItemType stateType)
        {
            equipmentsButtonText.gameObject.SetActive(false);
            equipmentsButtonSelectedText.gameObject.SetActive(false);
            consumablesButtonText.gameObject.SetActive(false);
            consumablesButtonSelectedText.gameObject.SetActive(false);
            materialsButtonText.gameObject.SetActive(false);
            materialsButtonSelectedText.gameObject.SetActive(false);
            switch (stateType)
            {
                case ItemType.Equipment:
                    equipmentsButtonImage.sprite = _selectedButtonSprite;
                    equipmentsButtonIconImage.sprite = _equipmentsButtonIconSpriteBlue;
                    consumablesButtonImage.sprite = _deselectedButtonSprite;
                    consumablesButtonIconImage.sprite = _consumablesButtonIconSpriteBlack;
                    materialsButtonImage.sprite = _deselectedButtonSprite;
                    materialsButtonIconImage.sprite = _materialsButtonIconSpriteBlack;
                    scrollerController.SetData(SharedModel.Equipments);
                    equipmentsButtonSelectedText.gameObject.SetActive(true);
                    consumablesButtonText.gameObject.SetActive(true);
                    materialsButtonText.gameObject.SetActive(true);
                    break;
                case ItemType.Consumable:
                    equipmentsButtonImage.sprite = _deselectedButtonSprite;
                    equipmentsButtonIconImage.sprite = _equipmentsButtonIconSpriteBlack;
                    consumablesButtonImage.sprite = _selectedButtonSprite;
                    consumablesButtonIconImage.sprite = _consumablesButtonIconSpriteBlue;
                    materialsButtonImage.sprite = _deselectedButtonSprite;
                    materialsButtonIconImage.sprite = _materialsButtonIconSpriteBlack;
                    scrollerController.SetData(SharedModel.Consumables);
                    equipmentsButtonText.gameObject.SetActive(true);
                    consumablesButtonSelectedText.gameObject.SetActive(true);
                    materialsButtonText.gameObject.SetActive(true);
                    break;
                case ItemType.Material:
                    equipmentsButtonImage.sprite = _deselectedButtonSprite;
                    equipmentsButtonIconImage.sprite = _equipmentsButtonIconSpriteBlack;
                    consumablesButtonImage.sprite = _deselectedButtonSprite;
                    consumablesButtonIconImage.sprite = _consumablesButtonIconSpriteBlack;
                    materialsButtonImage.sprite = _selectedButtonSprite;
                    materialsButtonIconImage.sprite = _materialsButtonIconSpriteBlue;
                    scrollerController.SetData(SharedModel.Materials);
                    equipmentsButtonText.gameObject.SetActive(true);
                    consumablesButtonText.gameObject.SetActive(true);
                    materialsButtonSelectedText.gameObject.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stateType), stateType, null);
            }

            // 선택된 버튼의 스프라이트가 1픽셀 내려가는 문제가 있음.

            foreach (var pair in _switchButtonTransforms)
            {
                var btn = pair.Value;
                // TextMeshPro 그림자 마테리얼 변경 해줘야함
                if (pair.Key == stateType)
                {
                    btn.anchoredPosition = new Vector2(btn.anchoredPosition.x, 1);
                    btn.sizeDelta = BtnHighlightSize;
                }
                else
                {
                    btn.anchoredPosition = new Vector2(btn.anchoredPosition.x, 0);
                    btn.sizeDelta = BtnSize;
                }
            }

            Tooltip.Close();
        }

        private void SubscribeSelectedItemView(InventoryItemView view)
        {
            if (view is null)
                return;

            AdjustmentScrollPosition(view);
        }
        
        #endregion

        private void AdjustmentScrollPosition(InventoryItemView view)
        {
            var scroller = scrollerController.scroller;
            var cellHeight = scrollerController.GetCellViewSize(scroller, 0);
            var skipCount = Mathf.FloorToInt(scrollerController.scrollRectTransform.rect.height / cellHeight) - 1;
            var index = -Mathf.CeilToInt(view.InventoryCellView.transform.localPosition.y / cellHeight);

            if (scroller.StartCellViewIndex + skipCount < index)
            {
                scroller.ScrollPosition = scroller.GetScrollPositionForCellViewIndex(index - skipCount,
                    EnhancedScroller.CellViewPositionEnum.Before);
            }
            else if (scroller.StartCellViewIndex == index)
            {
                scroller.ScrollPosition =
                    scroller.GetScrollPositionForCellViewIndex(index, EnhancedScroller.CellViewPositionEnum.Before);
            }
        }
    }
}
