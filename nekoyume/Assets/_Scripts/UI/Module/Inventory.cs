using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
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
        [Serializable]
        private struct ButtonSet
        {
#pragma warning disable 0649
            public Button button;
            public Image image;
            public TextMeshProUGUI text;
            public TextMeshProUGUI selectedText;
#pragma warning restore 0649
        }

        private static readonly Vector2 BtnHighlightSize = new Vector2(122f, 60f);
        private static readonly Vector2 BtnSize = new Vector2(95f, 36f);

        [SerializeField]
        private ButtonSet equipmentsButton = default;

        [SerializeField]
        private ButtonSet consumablesButton = default;

        [SerializeField]
        private ButtonSet costumesButton = default;

        [SerializeField]
        private ButtonSet materialsButton = default;

        [SerializeField]
        private InventoryScroll scroll = null;

        private Sprite _selectedButtonSprite;
        private Sprite _deselectedButtonSprite;

        private readonly Dictionary<ItemType, RectTransform> _switchButtonTransforms =
            new Dictionary<ItemType, RectTransform>(ItemTypeComparer.Instance);

        private readonly List<IDisposable> _disposablesAtOnEnable = new List<IDisposable>();

        public readonly Subject<Inventory> OnResetItems = new Subject<Inventory>();

        public readonly Subject<InventoryItemView> OnDoubleClickItemView =
            new Subject<InventoryItemView>();

        public Model.Inventory SharedModel { get; set; }

        #region Mono

        protected void Awake()
        {
            _selectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_yellow_02");
            _deselectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_brown_01");
            _switchButtonTransforms.Add(ItemType.Equipment,
                equipmentsButton.button.GetComponent<RectTransform>());
            _switchButtonTransforms.Add(ItemType.Consumable,
                consumablesButton.button.GetComponent<RectTransform>());
            _switchButtonTransforms.Add(ItemType.Costume,
                costumesButton.button.GetComponent<RectTransform>());
            _switchButtonTransforms.Add(ItemType.Material,
                materialsButton.button.GetComponent<RectTransform>());

            consumablesButton.text.text = LocalizationManager.Localize("UI_CONSUMABLES");
            consumablesButton.selectedText.text = LocalizationManager.Localize("UI_CONSUMABLES");
            costumesButton.text.text = LocalizationManager.Localize("UI_COSTUME");
            costumesButton.selectedText.text = LocalizationManager.Localize("UI_COSTUME");
            equipmentsButton.text.text = LocalizationManager.Localize("UI_EQUIPMENTS");
            equipmentsButton.selectedText.text = LocalizationManager.Localize("UI_EQUIPMENTS");
            materialsButton.text.text = LocalizationManager.Localize("UI_MATERIALS");
            materialsButton.selectedText.text = LocalizationManager.Localize("UI_MATERIALS");

            SharedModel = new Model.Inventory();
            SharedModel.State.Subscribe(SubscribeState).AddTo(gameObject);
            SharedModel.SelectedItemView.Subscribe(SubscribeSelectedItemView).AddTo(gameObject);

            scroll.OnClick
                .Subscribe(cell => SharedModel.SubscribeItemOnClick(cell.View))
                .AddTo(gameObject);

            scroll.OnDoubleClick
                .Subscribe(cell =>
                {
                    SharedModel.DeselectItemView();
                    OnDoubleClickItemView.OnNext(cell.View);
                })
                .AddTo(gameObject);

            consumablesButton.button.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                SharedModel.State.Value = ItemType.Consumable;
            }).AddTo(gameObject);
            costumesButton.button.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                SharedModel.State.Value = ItemType.Costume;
            }).AddTo(gameObject);
            equipmentsButton.button.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                SharedModel.State.Value = ItemType.Equipment;
            }).AddTo(gameObject);
            materialsButton.button.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                SharedModel.State.Value = ItemType.Material;
            }).AddTo(gameObject);
        }

        private void OnEnable()
        {
            ReactiveAvatarState.Inventory.Subscribe(inventoryState =>
            {
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
        }

        #endregion

        #region Subscribe

        private void SubscribeState(ItemType stateType)
        {
            consumablesButton.text.gameObject.SetActive(false);
            consumablesButton.selectedText.gameObject.SetActive(false);
            costumesButton.text.gameObject.SetActive(false);
            costumesButton.selectedText.gameObject.SetActive(false);
            equipmentsButton.text.gameObject.SetActive(false);
            equipmentsButton.selectedText.gameObject.SetActive(false);
            materialsButton.text.gameObject.SetActive(false);
            materialsButton.selectedText.gameObject.SetActive(false);
            switch (stateType)
            {
                case ItemType.Consumable:
                    equipmentsButton.image.sprite = _deselectedButtonSprite;
                    costumesButton.image.sprite = _deselectedButtonSprite;
                    consumablesButton.image.sprite = _selectedButtonSprite;
                    materialsButton.image.sprite = _deselectedButtonSprite;
                    scroll.UpdateData(SharedModel.Consumables);
                    equipmentsButton.text.gameObject.SetActive(true);
                    costumesButton.text.gameObject.SetActive(true);
                    consumablesButton.selectedText.gameObject.SetActive(true);
                    materialsButton.text.gameObject.SetActive(true);
                    break;
                case ItemType.Costume:
                    equipmentsButton.image.sprite = _deselectedButtonSprite;
                    costumesButton.image.sprite = _selectedButtonSprite;
                    consumablesButton.image.sprite = _deselectedButtonSprite;
                    materialsButton.image.sprite = _deselectedButtonSprite;
                    scroll.UpdateData(SharedModel.Costumes);
                    equipmentsButton.text.gameObject.SetActive(true);
                    costumesButton.selectedText.gameObject.SetActive(true);
                    consumablesButton.text.gameObject.SetActive(true);
                    materialsButton.text.gameObject.SetActive(true);
                    break;
                case ItemType.Equipment:
                    equipmentsButton.image.sprite = _selectedButtonSprite;
                    costumesButton.image.sprite = _deselectedButtonSprite;
                    consumablesButton.image.sprite = _deselectedButtonSprite;
                    materialsButton.image.sprite = _deselectedButtonSprite;
                    scroll.UpdateData(SharedModel.Equipments);
                    equipmentsButton.selectedText.gameObject.SetActive(true);
                    costumesButton.text.gameObject.SetActive(true);
                    consumablesButton.text.gameObject.SetActive(true);
                    materialsButton.text.gameObject.SetActive(true);
                    break;
                case ItemType.Material:
                    equipmentsButton.image.sprite = _deselectedButtonSprite;
                    costumesButton.image.sprite = _deselectedButtonSprite;
                    consumablesButton.image.sprite = _deselectedButtonSprite;
                    materialsButton.image.sprite = _selectedButtonSprite;
                    scroll.UpdateData(SharedModel.Materials);
                    equipmentsButton.text.gameObject.SetActive(true);
                    costumesButton.text.gameObject.SetActive(true);
                    consumablesButton.text.gameObject.SetActive(true);
                    materialsButton.selectedText.gameObject.SetActive(true);
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

            scroll.ScrollTo(view.Model);
        }

        #endregion
    }
}
