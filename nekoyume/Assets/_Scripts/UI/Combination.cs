using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Manager;
using Nekoyume.Action;
using Nekoyume.Data;
using Nekoyume.Data.Table;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Stage = Nekoyume.Game.Stage;

namespace Nekoyume.UI
{
    public class Combination : Widget
    {
        private Model.Combination _data;

        public Module.InventoryAndItemInfo inventoryAndItemInfo;
        public CombinationStagedItemView[] stagedItems;
        public Button combinationButton;
        public Image combinationButtonImage;
        public Button closeButton;

        private readonly List<IDisposable> _disposablesForAwake = new List<IDisposable>();
        private readonly List<IDisposable> _disposablesForSetData = new List<IDisposable>();

        private Stage _stage;
        private Player _player;

        private SimpleItemCountPopup _simpleItemCountPopup;
        private CombinationResultPopup _resultPopup;
        private GrayLoadingScreen _loadingScreen;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            this.ComponentFieldsNotNullTest();

            combinationButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    _data.onClickCombination.OnNext(_data);
                    AudioController.PlayClick();
                })
                .AddTo(_disposablesForAwake);

            closeButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    Close();
                    AudioController.PlayClick();
                })
                .AddTo(_disposablesForAwake);
        }

        private void OnDestroy()
        {
            _disposablesForAwake.DisposeAllAndClear();

            Clear();
        }

        #endregion

        public override void Show()
        {
            _simpleItemCountPopup = Find<SimpleItemCountPopup>();
            if (ReferenceEquals(_simpleItemCountPopup, null))
            {
                throw new NotFoundComponentException<SimpleItemCountPopup>();
            }

            _resultPopup = Find<CombinationResultPopup>();
            if (ReferenceEquals(_resultPopup, null))
            {
                throw new NotFoundComponentException<CombinationResultPopup>();
            }

            _loadingScreen = Find<GrayLoadingScreen>();
            if (ReferenceEquals(_loadingScreen, null))
            {
                throw new NotFoundComponentException<LoadingScreen>();
            }

            base.Show();

            _stage = GameObject.Find("Stage").GetComponent<Stage>();
            if (ReferenceEquals(_stage, null))
            {
                throw new NotFoundComponentException<Stage>();
            }
            _stage.LoadBackground("combination");

            _player = _stage.GetPlayer();
            if (ReferenceEquals(_player, null))
            {
                throw new NotFoundComponentException<Player>();
            }
            _player.gameObject.SetActive(false);

            SetData(new Model.Combination(ActionManager.instance.Avatar.Items, stagedItems.Length));
            
            AudioController.instance.PlayMusic(AudioController.MusicCode.Combination);
        }
        
        public override void Close()
        {
            Clear();

            _stage.GetPlayer(_stage.RoomPosition);
            _stage.LoadBackground("room");
            _player.gameObject.SetActive(true);

            Find<Status>()?.Show();
            Find<Menu>()?.Show();

            base.Close();
            
            AudioController.instance.PlayMusic(AudioController.MusicCode.Main);
        }
        
        private void SetData(Model.Combination value)
        {
            if (ReferenceEquals(value, null))
            {
                Clear();
                return;
            }
            
            _disposablesForSetData.DisposeAllAndClear();
            _data = value;
            _data.inventoryAndItemInfo.Value.itemInfo.Value.item.Subscribe(OnItemInfoItem).AddTo(_disposablesForSetData);
            _data.itemCountPopup.Value.item.Subscribe(OnPopupItem).AddTo(_disposablesForSetData);
            _data.itemCountPopup.Value.onClickClose.Subscribe(OnClickClosePopup).AddTo(_disposablesForSetData);
            _data.stagedItems.ObserveAdd().Subscribe(OnAddStagedItems).AddTo(_disposablesForSetData);
            _data.stagedItems.ObserveRemove().Subscribe(OnRemoveStagedItems).AddTo(_disposablesForSetData);
            _data.stagedItems.ObserveReplace().Subscribe(_ => UpdateStagedItems()).AddTo(_disposablesForSetData);
            _data.readyForCombination.Subscribe(SetActiveCombinationButton).AddTo(_disposablesForSetData);
            _data.onClickCombination.Subscribe(RequestCombination).AddTo(_disposablesForSetData);
            _data.resultPopup.Subscribe(SubscribeResultPopup).AddTo(_disposablesForSetData);
            inventoryAndItemInfo.SetData(_data.inventoryAndItemInfo.Value);
            
            UpdateStagedItems();
        }

        private void Clear()
        {
            inventoryAndItemInfo.Clear();
            _data = null;
            _disposablesForSetData.DisposeAllAndClear();
            
            foreach (var item in stagedItems)
            {
                item.Clear();
            }
        }
        
        private void UpdateStagedItems(int startIndex = 0)
        {
            var dataCount = _data.stagedItems.Count;
            for (var i = startIndex; i < stagedItems.Length; i++)
            {
                var item = stagedItems[i];
                if (i < dataCount)
                {
                    item.SetData(_data.stagedItems[i]);
                }
                else
                {
                    item.Clear();
                }
            }
        }

        private void OnItemInfoItem(InventoryItem data)
        {
            if (ReferenceEquals(data, null) ||
                data.dimmed.Value ||
                _data.IsStagedItemsFulled)
            {
                _data.inventoryAndItemInfo.Value.itemInfo.Value.buttonEnabled.Value = false;
            }
            else
            {
                _data.inventoryAndItemInfo.Value.itemInfo.Value.buttonEnabled.Value = true;
            }
        }

        private void OnPopupItem(CountableItem data)
        {
            if (ReferenceEquals(data, null))
            {
                _simpleItemCountPopup.Close();
                return;
            }

            _simpleItemCountPopup.Pop(_data.itemCountPopup.Value);
        }

        private void OnClickClosePopup(Model.SimpleItemCountPopup data)
        {
            _data.itemCountPopup.Value.item.Value = null;
            _simpleItemCountPopup.Close();
        }

        private void OnAddStagedItems(CollectionAddEvent<CountEditableItem> e)
        {
            if (e.Index >= stagedItems.Length)
            {
                _data.stagedItems.RemoveAt(e.Index);
                throw new AddOutOfSpecificRangeException<CollectionAddEvent<CountEditableItem>>(
                    stagedItems.Length);
            }

            stagedItems[e.Index].SetData(e.Value);
        }

        private void OnRemoveStagedItems(CollectionRemoveEvent<CountEditableItem> e)
        {
            if (e.Index >= stagedItems.Length)
            {
                return;
            }

            var dataCount = _data.stagedItems.Count;
            for (var i = e.Index; i <= dataCount; i++)
            {
                var item = stagedItems[i];

                if (i < dataCount)
                {
                    item.SetData(_data.stagedItems[i]);
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
                combinationButtonImage.sprite = Resources.Load<Sprite>("UI/Textures/button_blue_01");
            }
            else
            {
                combinationButton.enabled = false;
                combinationButtonImage.sprite = Resources.Load<Sprite>("UI/Textures/button_black_01");
            }
        }

        private void RequestCombination(Model.Combination data)
        {
            _loadingScreen.Show();
            ActionManager.instance.Combination(_data.stagedItems.ToList())
                .Select(eval => eval.Action)
                .ObserveOnMainThread()
                .Subscribe(ResponseCombination)
                .AddTo(this);
            AnalyticsManager.instance.OnEvent(AnalyticsManager.EventName.ClickCombinationCombination);
        }

        /// <summary>
        /// 결과를 직접 받아서 데이타에 넣어주는 방법 보다는,
        /// 네트워크 결과를 핸들링하는 곳에 핸들링 인터페이스를 구현한 데이타 모델을 등록하는 방법이 좋겠다. 
        /// </summary>
        private void ResponseCombination(Action.Combination action)
        {
            var result = action.Result;
            if (result.ErrorCode == GameActionResult.ErrorCode.Success)
            {
                ItemEquipment itemData;
                if (!Tables.instance.ItemEquipment.TryGetValue(result.Item.id, out itemData))
                {
                    _loadingScreen.Close();
                    throw new InvalidActionException("`Combination` action's `Result` is invalid.");
                }

                var itemModel = new Game.Item.Inventory.InventoryItem(new Equipment(itemData), action.Result.Item.count);

                _data.resultPopup.Value = new Model.CombinationResultPopup(itemModel, itemModel.Count)
                {
                    isSuccess = true,
                    materialItems = _data.stagedItems
                };
                
                AnalyticsManager.instance.OnEvent(AnalyticsManager.EventName.ActionCombinationSuccess);
            }
            else
            {
                _data.resultPopup.Value = new Model.CombinationResultPopup(null, 0)
                {
                    isSuccess = false,
                    materialItems = _data.stagedItems
                };
                
                AnalyticsManager.instance.OnEvent(AnalyticsManager.EventName.ActionCombinationFail);
            }
        }

        private void SubscribeResultPopup(Model.CombinationResultPopup data)
        {
            if (ReferenceEquals(data, null))
            {
                _resultPopup.Close();
                return;
            }
            
            _loadingScreen.Close();
            _resultPopup.Pop(data);
        }
    }
}
