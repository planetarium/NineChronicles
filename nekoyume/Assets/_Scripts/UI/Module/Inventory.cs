using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using EnhancedUI.EnhancedScroller;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.UI.Scroller;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class Inventory : MonoBehaviour
    {
        public Text titleText;
        public Button equipmentsButton;
        public Image equipmentsButtonImage;
        public Image equipmentsButtonIconImage;
        public Text equipmentsButtonText;
        public Button consumablesButton;
        public Image consumablesButtonImage;
        public Image consumablesButtonIconImage;
        public Text consumablesButtonText;
        public Button materialsButton;
        public Image materialsButtonImage;
        public Image materialsButtonIconImage;
        public Text materialsButtonText;
        public InventoryScrollerController scrollerController;

        private readonly List<IDisposable> _disposablesForModel = new List<IDisposable>();

        private Sprite _selectedButtonSprite;
        private Sprite _deselectedButtonSprite;
        private Sprite _equipmentsButtonIconSpriteBlack;
        private Sprite _equipmentsButtonIconSpriteBlue;
        private Sprite _consumablesButtonIconSpriteBlack;
        private Sprite _consumablesButtonIconSpriteBlue;
        private Sprite _materialsButtonIconSpriteBlack;
        private Sprite _materialsButtonIconSpriteBlue;

        private ItemInformationTooltip _tooltip;
        private RectTransform[] _switchButtonTransforms;
        private static Vector2 BtnHighlightSize = new Vector2(157f, 60f);
        private static Vector2 BtnSize = new Vector2(130f, 36f);

        public RectTransform RectTransform { get; private set; }

        public ItemInformationTooltip Tooltip => _tooltip
            ? _tooltip
            : _tooltip = Widget.Find<ItemInformationTooltip>();

        public Model.Inventory Model { get; private set; }

        #region Mono

        protected void Awake()
        {
            this.ComponentFieldsNotNullTest();

            _switchButtonTransforms = new RectTransform[]
            {
                equipmentsButton.GetComponent<RectTransform>(),
                consumablesButton.GetComponent<RectTransform>(),
                materialsButton.GetComponent<RectTransform>(),
            };

            titleText.text = LocalizationManager.Localize("UI_INVENTORY");
            equipmentsButtonText.text = LocalizationManager.Localize("UI_EQUIPMENTS");
            consumablesButtonText.text = LocalizationManager.Localize("UI_CONSUMABLES");
            materialsButtonText.text = LocalizationManager.Localize("UI_MATERIALS");

            _selectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_yellow_02");
            _deselectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_brown_01");
            _equipmentsButtonIconSpriteBlack = Resources.Load<Sprite>("UI/Textures/icon_inventory_01_black");
            _equipmentsButtonIconSpriteBlue = Resources.Load<Sprite>("UI/Textures/icon_inventory_01_yellow");
            _consumablesButtonIconSpriteBlack = Resources.Load<Sprite>("UI/Textures/icon_inventory_02_black");
            _consumablesButtonIconSpriteBlue = Resources.Load<Sprite>("UI/Textures/icon_inventory_02_yellow");
            _materialsButtonIconSpriteBlack = Resources.Load<Sprite>("UI/Textures/icon_inventory_03_black");
            _materialsButtonIconSpriteBlue = Resources.Load<Sprite>("UI/Textures/icon_inventory_03_yellow");

            RectTransform = GetComponent<RectTransform>();

            equipmentsButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                Model.state.Value = UI.Model.Inventory.State.Equipments;
            }).AddTo(this);
            consumablesButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                Model.state.Value = UI.Model.Inventory.State.Consumables;
            }).AddTo(this);
            materialsButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                Model.state.Value = UI.Model.Inventory.State.Materials;
            }).AddTo(this);
        }

        private void OnDisable()
        {
            if (Tooltip)
                Tooltip.Close();
        }

        private void OnDestroy()
        {
            Clear();
        }

        #endregion

        public void SetData(Model.Inventory model)
        {
            if (ReferenceEquals(model, null))
            {
                Clear();
                return;
            }

            _disposablesForModel.DisposeAllAndClear();
            Model = model;
            Model.state.Subscribe(SubscribeState).AddTo(_disposablesForModel);
            Model.selectedItemView.Subscribe(view =>
            {
                if (!view) return;

                var scroller = scrollerController.scroller;
                var cellHeight = scrollerController.GetCellViewSize(scroller, 0);
                var skipCount = Mathf.FloorToInt(scrollerController.scrollRectTransform.rect.height / cellHeight) - 1;
                int idx = -Mathf.CeilToInt(view.inventoryCellView.transform.localPosition.y / cellHeight);

                if (scroller.StartCellViewIndex + skipCount < idx)
                {
                    scroller.ScrollPosition = scroller.GetScrollPositionForCellViewIndex(idx - skipCount,
                        EnhancedScroller.CellViewPositionEnum.Before);
                }
                else if (scroller.StartCellViewIndex == idx)
                {
                    scroller.ScrollPosition =
                        scroller.GetScrollPositionForCellViewIndex(idx, EnhancedScroller.CellViewPositionEnum.Before);
                }
            }).AddTo(_disposablesForModel);
        }

        public void Clear()
        {
            _disposablesForModel.DisposeAllAndClear();
            Model = null;
            scrollerController.Clear();
        }

        private void SubscribeState(Model.Inventory.State state)
        {
            switch (state)
            {
                case UI.Model.Inventory.State.Equipments:
                    equipmentsButtonImage.sprite = _selectedButtonSprite;
                    equipmentsButtonIconImage.sprite = _equipmentsButtonIconSpriteBlue;
                    consumablesButtonImage.sprite = _deselectedButtonSprite;
                    consumablesButtonIconImage.sprite = _consumablesButtonIconSpriteBlack;
                    materialsButtonImage.sprite = _deselectedButtonSprite;
                    materialsButtonIconImage.sprite = _materialsButtonIconSpriteBlack;
                    scrollerController.SetData(Model.equipments);
                    break;
                case UI.Model.Inventory.State.Consumables:
                    equipmentsButtonImage.sprite = _deselectedButtonSprite;
                    equipmentsButtonIconImage.sprite = _equipmentsButtonIconSpriteBlack;
                    consumablesButtonImage.sprite = _selectedButtonSprite;
                    consumablesButtonIconImage.sprite = _consumablesButtonIconSpriteBlue;
                    materialsButtonImage.sprite = _deselectedButtonSprite;
                    materialsButtonIconImage.sprite = _materialsButtonIconSpriteBlack;
                    scrollerController.SetData(Model.consumables);
                    break;
                case UI.Model.Inventory.State.Materials:
                    equipmentsButtonImage.sprite = _deselectedButtonSprite;
                    equipmentsButtonIconImage.sprite = _equipmentsButtonIconSpriteBlack;
                    consumablesButtonImage.sprite = _deselectedButtonSprite;
                    consumablesButtonIconImage.sprite = _consumablesButtonIconSpriteBlack;
                    materialsButtonImage.sprite = _selectedButtonSprite;
                    materialsButtonIconImage.sprite = _materialsButtonIconSpriteBlue;
                    scrollerController.SetData(Model.materials);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state, null);
            }

            // ���õ� ��ư�� ��������Ʈ�� 1�ȼ� �������� ������ ����.

            var btn = _switchButtonTransforms[(int) state];
            btn.anchoredPosition = new Vector2(btn.anchoredPosition.x, 1);
            btn.sizeDelta = BtnHighlightSize;
            var shadows = btn.GetComponentsInChildren<Shadow>();
            foreach (var shadow in shadows)
            {
                shadow.effectColor = ColorHelper.HexToColorRGB("a35400");
            }

            for (int i = 0; i < 3; ++i)
            {
                if (i == (int) state) continue;
                btn = _switchButtonTransforms[i];
                btn.anchoredPosition = new Vector2(btn.anchoredPosition.x, 0);
                btn.sizeDelta = BtnSize;
                shadows = btn.GetComponentsInChildren<Shadow>();
                foreach (var shadow in shadows)
                {
                    shadow.effectColor = Color.black;
                }
            }

            if (Tooltip)
                Tooltip.Close();
        }
    }
}
