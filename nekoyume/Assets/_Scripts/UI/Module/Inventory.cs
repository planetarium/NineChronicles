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
        private static readonly Vector2 BtnHighlightSize = new Vector2(122f, 60f);
        private static readonly Vector2 BtnSize = new Vector2(95f, 36f);

        public TextMeshProUGUI titleText;
        public Button equipmentsButton;
        public Image equipmentsButtonImage;
        public TextMeshProUGUI equipmentsButtonText;
        public TextMeshProUGUI equipmentsButtonSelectedText;
        public Button consumablesButton;
        public Image consumablesButtonImage;
        public TextMeshProUGUI consumablesButtonText;
        public TextMeshProUGUI consumablesButtonSelectedText;
        public Button costumesButton;
        public Image costumesButtonImage;
        public TextMeshProUGUI costumesButtonText;
        public TextMeshProUGUI costumesButtonSelectedText;
        public Button materialsButton;
        public Image materialsButtonImage;
        public TextMeshProUGUI materialsButtonText;
        public TextMeshProUGUI materialsButtonSelectedText;
        public InventoryScrollerController scrollerController;

        private Sprite _selectedButtonSprite;
        private Sprite _deselectedButtonSprite;

        private readonly Dictionary<ItemType, RectTransform> _switchButtonTransforms =
            new Dictionary<ItemType, RectTransform>(ItemTypeComparer.Instance);

        private readonly List<IDisposable> _disposablesAtOnEnable = new List<IDisposable>();

        public RectTransform RectTransform { get; private set; }

        public Model.Inventory SharedModel { get; private set; }

        public readonly Subject<Inventory> OnResetItems = new Subject<Inventory>();

        #region Mono

        protected void Awake()
        {
            _selectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_yellow_02");
            _deselectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_brown_01");
            _switchButtonTransforms.Add(ItemType.Equipment, equipmentsButton.GetComponent<RectTransform>());
            _switchButtonTransforms.Add(ItemType.Consumable, consumablesButton.GetComponent<RectTransform>());
            _switchButtonTransforms.Add(ItemType.Costume, costumesButton.GetComponent<RectTransform>());
            _switchButtonTransforms.Add(ItemType.Material, materialsButton.GetComponent<RectTransform>());

            titleText.text = LocalizationManager.Localize("UI_INVENTORY");
            consumablesButtonText.text = LocalizationManager.Localize("UI_CONSUMABLES");
            consumablesButtonSelectedText.text = LocalizationManager.Localize("UI_CONSUMABLES");
            costumesButtonText.text = LocalizationManager.Localize("UI_COSTUME");
            costumesButtonSelectedText.text = LocalizationManager.Localize("UI_COSTUME");
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
            costumesButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                SharedModel.State.Value = ItemType.Costume;
            }).AddTo(gameObject);
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
            ReactiveAvatarState.Inventory.Subscribe(inventoryState =>
            {
                scrollerController.DisposeAddedAtSetData();
                SharedModel.ResetItems(inventoryState);
                OnResetItems.OnNext(this);
            }).AddTo(_disposablesAtOnEnable);
        }

        private void OnDisable()
        {
            _disposablesAtOnEnable.DisposeAllAndClear();
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
            costumesButtonText.gameObject.SetActive(false);
            costumesButtonSelectedText.gameObject.SetActive(false);
            equipmentsButtonText.gameObject.SetActive(false);
            equipmentsButtonSelectedText.gameObject.SetActive(false);
            materialsButtonText.gameObject.SetActive(false);
            materialsButtonSelectedText.gameObject.SetActive(false);
            switch (stateType)
            {
                case ItemType.Consumable:
                    equipmentsButtonImage.sprite = _deselectedButtonSprite;
                    costumesButtonImage.sprite = _deselectedButtonSprite;
                    consumablesButtonImage.sprite = _selectedButtonSprite;
                    materialsButtonImage.sprite = _deselectedButtonSprite;
                    scrollerController.SetData(SharedModel.Consumables);
                    equipmentsButtonText.gameObject.SetActive(true);
                    costumesButtonText.gameObject.SetActive(true);
                    consumablesButtonSelectedText.gameObject.SetActive(true);
                    materialsButtonText.gameObject.SetActive(true);
                    break;
                case ItemType.Costume:
                    equipmentsButtonImage.sprite = _deselectedButtonSprite;
                    costumesButtonImage.sprite = _selectedButtonSprite;
                    consumablesButtonImage.sprite = _deselectedButtonSprite;
                    materialsButtonImage.sprite = _deselectedButtonSprite;
                    scrollerController.SetData(SharedModel.Costumes);
                    equipmentsButtonText.gameObject.SetActive(true);
                    costumesButtonSelectedText.gameObject.SetActive(true);
                    consumablesButtonText.gameObject.SetActive(true);
                    materialsButtonText.gameObject.SetActive(true);
                    break;
                case ItemType.Equipment:
                    equipmentsButtonImage.sprite = _selectedButtonSprite;
                    costumesButtonImage.sprite = _deselectedButtonSprite;
                    consumablesButtonImage.sprite = _deselectedButtonSprite;
                    materialsButtonImage.sprite = _deselectedButtonSprite;
                    scrollerController.SetData(SharedModel.Equipments);
                    equipmentsButtonSelectedText.gameObject.SetActive(true);
                    costumesButtonText.gameObject.SetActive(true);
                    consumablesButtonText.gameObject.SetActive(true);
                    materialsButtonText.gameObject.SetActive(true);
                    break;
                case ItemType.Material:
                    equipmentsButtonImage.sprite = _deselectedButtonSprite;
                    costumesButtonImage.sprite = _deselectedButtonSprite;
                    consumablesButtonImage.sprite = _deselectedButtonSprite;
                    materialsButtonImage.sprite = _selectedButtonSprite;
                    scrollerController.SetData(SharedModel.Materials);
                    equipmentsButtonText.gameObject.SetActive(true);
                    costumesButtonText.gameObject.SetActive(true);
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
            {
                return;
            }

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
