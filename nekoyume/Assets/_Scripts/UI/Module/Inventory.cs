using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Model;
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
        
        public RectTransform RectTransform { get; private set; }
        public ItemInformationTooltip Tooltip { get; private set; }
        public Model.Inventory Model { get; private set; }
        
        #region Mono

        protected void Awake()
        {
            this.ComponentFieldsNotNullTest();

            titleText.text = LocalizationManager.Localize("UI_INVENTORY");
            equipmentsButtonText.text = LocalizationManager.Localize("UI_EQUIPMENTS");
            consumablesButtonText.text = LocalizationManager.Localize("UI_CONSUMABLES");
            materialsButtonText.text = LocalizationManager.Localize("UI_MATERIALS");

            _selectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_blue_01");
            _deselectedButtonSprite = Resources.Load<Sprite>("UI/Textures/button_black_03");
            _equipmentsButtonIconSpriteBlack = Resources.Load<Sprite>("UI/Textures/icon_inventory_01_black");
            _equipmentsButtonIconSpriteBlue = Resources.Load<Sprite>("UI/Textures/icon_inventory_01_blue");
            _consumablesButtonIconSpriteBlack = Resources.Load<Sprite>("UI/Textures/icon_inventory_02_black");
            _consumablesButtonIconSpriteBlue = Resources.Load<Sprite>("UI/Textures/icon_inventory_02_blue");
            _materialsButtonIconSpriteBlack = Resources.Load<Sprite>("UI/Textures/icon_inventory_03_black");
            _materialsButtonIconSpriteBlue = Resources.Load<Sprite>("UI/Textures/icon_inventory_03_blue");
            
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

        private void OnEnable()
        {
            Tooltip = Widget.Find<ItemInformationTooltip>();
        }

        private void OnDisable()
        {
            if (!ReferenceEquals(Tooltip, null))
            {
                Tooltip.Close();   
            }
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
            
            Tooltip.Close();
        }
    }
}
