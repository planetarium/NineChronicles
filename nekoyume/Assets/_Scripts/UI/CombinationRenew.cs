using System;
using System.Collections.Generic;
using Nekoyume.Action;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.UI.ItemInfo;
using Nekoyume.UI.ItemView;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class CombinationRenew : Widget
    {
        private Model.Combination _data;

        public InventoryRenew inventoryRenew;
        public ButtonedItemInfo selectedItemInfo;
        public CombinationStagedItemView[] stagedItems;
        public Button combinationButton;
        public Image combinationButtonImage;
        public Text combinationButtonText;
        public Button closeButton;

        private readonly List<IDisposable> _disposables = new List<IDisposable>();

        private Stage _stage;
        private Player _player;

        private SelectItemCountPopup _popup;

        #region Mono

        private void Awake()
        {
            if (ReferenceEquals(inventoryRenew, null) ||
                ReferenceEquals(selectedItemInfo, null) ||
                ReferenceEquals(combinationButton, null) ||
                ReferenceEquals(combinationButtonImage, null) ||
                ReferenceEquals(combinationButtonText, null) ||
                ReferenceEquals(closeButton, null))
            {
                throw new SerializeFieldNullException();
            }

            combinationButton.OnClickAsObservable()
                .Subscribe(_ => { Debug.Log("조합 시작!!"); })
                .AddTo(_disposables);

            closeButton.OnClickAsObservable()
                .Subscribe(_ => Close())
                .AddTo(_disposables);
        }

        private void OnEnable()
        {
            _stage = GameObject.Find("Stage").GetComponent<Stage>();
            if (ReferenceEquals(_stage, null))
            {
                throw new NotFoundComponentException<Stage>();
            }
        }

        private void OnDisable()
        {
            _stage = null;
            _player = null;
        }

        private void OnDestroy()
        {
            _disposables.ForEach(d => d.Dispose());
        }

        #endregion

        public override void Show()
        {
            _popup = Find<SelectItemCountPopup>();
            if (ReferenceEquals(_popup, null))
            {
                throw new NotFoundComponentException<SelectItemCountPopup>();
            }

            base.Show();

            _stage.LoadBackground("combination");

            _player = FindObjectOfType<Player>();
            if (ReferenceEquals(_player, null))
            {
                throw new NotFoundComponentException<Player>();
            }

            _player.gameObject.SetActive(false);

            _data = new Model.Combination(ActionManager.Instance.Avatar.Items, 5);
            _data.SelectedItemInfo.Value.Item.Subscribe(OnDataSelectedItemInfoItem);
            _data.Popup.Value.Item.Subscribe(OnDataPopupItem);
            _data.Popup.Value.OnClickClose.Subscribe(OnDataPopupOnClickClose);
            _data.StagedItems.ObserveAdd().Subscribe(OnDataStagedItemsAdd);
            _data.StagedItems.ObserveRemove().Subscribe(OnDataStagedItemsRemove);
            _data.ReadyForCombination.Subscribe(SetActiveCombinationButton);

            inventoryRenew.SetData(_data.Inventory.Value);
            inventoryRenew.Show();
            selectedItemInfo.SetData(_data.SelectedItemInfo.Value);

            var count = stagedItems.Length;
            for (int i = 0; i < count; i++)
            {
                var item = stagedItems[i];
                item.Clear();
            }
        }

        public override void Close()
        {
            _data.Dispose();

            _player.gameObject.SetActive(true);
            _stage.LoadBackground("room");

            Find<Status>()?.Show();
            Find<Menu>()?.Show();

            base.Close();
        }

        private void OnDataSelectedItemInfoItem(Model.Inventory.Item data)
        {
            if (ReferenceEquals(data, null) ||
                data.Dimmed.Value ||
                _data.IsStagedItemsFulled)
            {
                _data.SelectedItemInfo.Value.ButtonEnabled.Value = false;
            }
            else
            {
                _data.SelectedItemInfo.Value.ButtonEnabled.Value = true;
            }
        }

        private void OnDataPopupItem(Model.Inventory.Item data)
        {
            if (ReferenceEquals(data, null))
            {
                _popup.Close();
                return;
            }

            _popup.Pop(_data.Popup.Value);
        }

        private void OnDataPopupOnClickClose(Model.SelectItemCountPopup<Model.Inventory.Item> data)
        {
            _data.Popup.Value.Item.Value = null;
            _popup.Close();
        }

        private void OnDataStagedItemsAdd(CollectionAddEvent<CountEditableItem<Model.Inventory.Item>> e)
        {
            if (e.Index >= stagedItems.Length)
            {
                _data.StagedItems.RemoveAt(e.Index);
                throw new AddOutOfSpecificRangeException<CollectionAddEvent<CountEditableItem<Model.Inventory.Item>>>(
                    stagedItems.Length);
            }

            stagedItems[e.Index].SetData(e.Value);
        }

        private void OnDataStagedItemsRemove(CollectionRemoveEvent<CountEditableItem<Model.Inventory.Item>> e)
        {
            if (e.Index >= stagedItems.Length)
            {
                return;
            }

            var dataCount = _data.StagedItems.Count;
            for (var i = e.Index; i <= dataCount; i++)
            {
                var item = stagedItems[i];

                if (i < dataCount)
                {
                    item.SetData(_data.StagedItems[i]);
                }
                else
                {
                    item.Clear();
                }
            }
        }

        private void SetActiveCombinationButton(bool isActive)
        {
            if (isActive)
            {
                combinationButton.enabled = true;
                combinationButtonImage.sprite = Resources.Load<Sprite>("ui/button_blue_02");
                combinationButtonText.text = "ON";
            }
            else
            {
                combinationButton.enabled = false;
                combinationButtonImage.sprite = Resources.Load<Sprite>("ui/button_black_01");
                combinationButtonText.text = "OFF";
            }
        }
    }
}
