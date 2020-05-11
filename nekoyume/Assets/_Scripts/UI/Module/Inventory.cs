using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using EnhancedUI.EnhancedScroller;
using Nekoyume.Game.Controller;
using Nekoyume.Model.Item;
using Nekoyume.State;
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
        private Sprite _equipmentsButtonIconSpriteHighlighted;
        private Sprite _consumablesButtonIconSpriteBlack;
        private Sprite _consumablesButtonIconSpriteHighlighted;
        private Sprite _materialsButtonIconSpriteBlack;
        private Sprite _materialsButtonIconSpriteHighlighted;

        private readonly Dictionary<ItemType, RectTransform> _switchButtonTransforms =
            new Dictionary<ItemType, RectTransform>(ItemTypeComparer.Instance);

        public RectTransform RectTransform { get; private set; }

        public Model.Inventory SharedModel { get; private set; }

        public readonly Subject<Inventory> OnResetItems = new Subject<Inventory>();

        #region Mono

        protected void Awake()
        {
            _selectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_yellow_02");
            _deselectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_brown_01");
            _consumablesButtonIconSpriteBlack = Resources.Load<Sprite>("UI/Textures/icon_inventory_02_black");
            _consumablesButtonIconSpriteHighlighted = Resources.Load<Sprite>("UI/Textures/icon_inventory_02_yellow");
            // TODO: 인벤토리에 코스튬 대응하기
            // _costumesButtonIconSpriteBlack = Resources.Load<Sprite>("UI/Textures/icon_inventory_01_black");
            // _costumesButtonIconSpriteHighlighted = Resources.Load<Sprite>("UI/Textures/icon_inventory_01_yellow");
            _equipmentsButtonIconSpriteBlack = Resources.Load<Sprite>("UI/Textures/icon_inventory_01_black");
            _equipmentsButtonIconSpriteHighlighted = Resources.Load<Sprite>("UI/Textures/icon_inventory_01_yellow");
            _materialsButtonIconSpriteBlack = Resources.Load<Sprite>("UI/Textures/icon_inventory_03_black");
            _materialsButtonIconSpriteHighlighted = Resources.Load<Sprite>("UI/Textures/icon_inventory_03_yellow");
            _switchButtonTransforms.Add(ItemType.Equipment, equipmentsButton.GetComponent<RectTransform>());
            _switchButtonTransforms.Add(ItemType.Consumable, consumablesButton.GetComponent<RectTransform>());
            _switchButtonTransforms.Add(ItemType.Material, materialsButton.GetComponent<RectTransform>());
            // TODO: 인벤토리에 코스튬 대응하기
            // _switchButtonTransforms.Add(ItemType.Costume, costumesButton.GetComponent<RectTransform>());

            titleText.text = LocalizationManager.Localize("UI_INVENTORY");
            consumablesButtonText.text = LocalizationManager.Localize("UI_CONSUMABLES");
            consumablesButtonSelectedText.text = LocalizationManager.Localize("UI_CONSUMABLES");
            // TODO: 인벤토리에 코스튬 대응하기
            // costumesButtonText.text = LocalizationManager.Localize("UI_COSTUME");
            // costumesButtonSelectedText.text = LocalizationManager.Localize("UI_COSTUME");
            equipmentsButtonText.text = LocalizationManager.Localize("UI_EQUIPMENTS");
            equipmentsButtonSelectedText.text = LocalizationManager.Localize("UI_EQUIPMENTS");
            materialsButtonText.text = LocalizationManager.Localize("UI_MATERIALS");
            materialsButtonSelectedText.text = LocalizationManager.Localize("UI_MATERIALS");

            RectTransform = GetComponent<RectTransform>();

            SharedModel = new Model.Inventory();
            SharedModel.State.Subscribe(SubscribeState).AddTo(gameObject);
            SharedModel.SelectedItemView.Subscribe(SubscribeSelectedItemView).AddTo(gameObject);

            consumablesButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                SharedModel.State.Value = ItemType.Consumable;
            }).AddTo(gameObject);
            // TODO: 인벤토리에 코스튬 대응하기
            // costumesButton.OnClickAsObservable().Subscribe(_ =>
            // {
            //     AudioController.PlayClick();
            //     SharedModel.State.Value = ItemType.Costume;
            // }).AddTo(gameObject);
            equipmentsButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                SharedModel.State.Value = ItemType.Equipment;
            }).AddTo(gameObject);
            materialsButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                SharedModel.State.Value = ItemType.Material;
            }).AddTo(gameObject);
        }

        private void OnEnable()
        {
            if (States.Instance.CurrentAvatarState is null)
            {
                return;
            }

            var inventoryState = States.Instance.CurrentAvatarState.inventory;
            scrollerController.DisposeAddedAtSetData();
            SharedModel.ResetItems(inventoryState);
            OnResetItems.OnNext(this);
        }

        private void OnDisable()
        {
            Widget.Find<ItemInformationTooltip>().Close();
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
            consumablesButtonText.gameObject.SetActive(false);
            consumablesButtonSelectedText.gameObject.SetActive(false);
            // TODO: 인벤토리에 코스튬 대응하기
            // costumesButtonText.gameObject.SetActive(false);
            // costumesButtonSelectedText.gameObject.SetActive(false);
            equipmentsButtonText.gameObject.SetActive(false);
            equipmentsButtonSelectedText.gameObject.SetActive(false);
            materialsButtonText.gameObject.SetActive(false);
            materialsButtonSelectedText.gameObject.SetActive(false);
            switch (stateType)
            {
                case ItemType.Consumable:
                    equipmentsButtonImage.sprite = _deselectedButtonSprite;
                    equipmentsButtonIconImage.sprite = _equipmentsButtonIconSpriteBlack;
                    consumablesButtonImage.sprite = _selectedButtonSprite;
                    consumablesButtonIconImage.sprite = _consumablesButtonIconSpriteHighlighted;
                    materialsButtonImage.sprite = _deselectedButtonSprite;
                    materialsButtonIconImage.sprite = _materialsButtonIconSpriteBlack;
                    scrollerController.SetData(SharedModel.Consumables);
                    equipmentsButtonText.gameObject.SetActive(true);
                    consumablesButtonSelectedText.gameObject.SetActive(true);
                    materialsButtonText.gameObject.SetActive(true);
                    break;
                // TODO: 인벤토리에 코스튬 대응하기
                // case ItemType.Costume:
                //     break;
                case ItemType.Equipment:
                    equipmentsButtonImage.sprite = _selectedButtonSprite;
                    equipmentsButtonIconImage.sprite = _equipmentsButtonIconSpriteHighlighted;
                    consumablesButtonImage.sprite = _deselectedButtonSprite;
                    consumablesButtonIconImage.sprite = _consumablesButtonIconSpriteBlack;
                    materialsButtonImage.sprite = _deselectedButtonSprite;
                    materialsButtonIconImage.sprite = _materialsButtonIconSpriteBlack;
                    scrollerController.SetData(SharedModel.Equipments);
                    equipmentsButtonSelectedText.gameObject.SetActive(true);
                    consumablesButtonText.gameObject.SetActive(true);
                    materialsButtonText.gameObject.SetActive(true);
                    break;
                case ItemType.Material:
                    equipmentsButtonImage.sprite = _deselectedButtonSprite;
                    equipmentsButtonIconImage.sprite = _equipmentsButtonIconSpriteBlack;
                    consumablesButtonImage.sprite = _deselectedButtonSprite;
                    consumablesButtonIconImage.sprite = _consumablesButtonIconSpriteBlack;
                    materialsButtonImage.sprite = _selectedButtonSprite;
                    materialsButtonIconImage.sprite = _materialsButtonIconSpriteHighlighted;
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

            Widget.Find<ItemInformationTooltip>().Close();
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
