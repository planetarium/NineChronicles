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
using Nekoyume.UI.ItemInfo;
using Nekoyume.UI.ItemView;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Stage = Nekoyume.Game.Stage;

namespace Nekoyume.UI
{
    public class Combination : Widget
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

        private SelectItemCountPopup _selectItemCountPopup;
        private CombinationResultPopup _resultPopup;
        private GrayLoadingScreen _loadingScreen;
        private int _count;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

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
                .Subscribe(_ =>
                {
                    _data.OnClickCombination.OnNext(_data);
                    AudioController.PlayClick();
                })
                .AddTo(_disposables);

            closeButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    Close();
                    AudioController.PlayClick();
                })
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
            _count = 0;
        }

        private void OnDestroy()
        {
            _disposables.DisposeAllAndClear();
        }

        #endregion

        public override void Show()
        {
            _selectItemCountPopup = Find<SelectItemCountPopup>();
            if (ReferenceEquals(_selectItemCountPopup, null))
            {
                throw new NotFoundComponentException<SelectItemCountPopup>();
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

            _stage.LoadBackground("combination");

            _player = FindObjectOfType<Player>();
            if (ReferenceEquals(_player, null))
            {
                throw new NotFoundComponentException<Player>();
            }

            _player.gameObject.SetActive(false);

            _data = new Model.Combination(ActionManager.instance.Avatar.Items, stagedItems.Length);
            _data.SelectedItemInfo.Value.Item.Subscribe(OnDataSelectedItemInfoItem);
            _data.SelectItemCountPopup.Value.Item.Subscribe(OnDataPopupItem);
            _data.SelectItemCountPopup.Value.OnClickClose.Subscribe(OnDataPopupOnClickClose);
            _data.StagedItems.ObserveAdd().Subscribe(OnDataStagedItemsAdd);
            _data.StagedItems.ObserveRemove().Subscribe(OnDataStagedItemsRemove);
            _data.StagedItems.ObserveReplace().Subscribe(OnDataStagedItemsReplace);
            _data.ReadyForCombination.Subscribe(SetActiveCombinationButton);
            _data.OnClickCombination.Subscribe(RequestCombination);
            _data.ResultPopup.Subscribe(SubscribeResultPopup);

            inventoryRenew.SetData(_data.Inventory.Value);
            inventoryRenew.Show();
            selectedItemInfo.SetData(_data.SelectedItemInfo.Value);
            UpdateStagedItems();
            
            AudioController.instance.PlayMusic(AudioController.MusicCode.Combination);
        }

        public override void Close()
        {
            _data.Dispose();

            _player.gameObject.SetActive(true);
            _stage.LoadBackground("room");

            Find<Status>()?.Show();
            Find<Menu>()?.Show();

            base.Close();
            
            AudioController.instance.PlayMusic(AudioController.MusicCode.Main);
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
                _selectItemCountPopup.Close();
                return;
            }

            _selectItemCountPopup.Pop(_data.SelectItemCountPopup.Value);
        }

        private void OnDataPopupOnClickClose(SelectItemCountPopup<Model.Inventory.Item> data)
        {
            _data.SelectItemCountPopup.Value.Item.Value = null;
            _selectItemCountPopup.Close();
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

        private void OnDataStagedItemsReplace(CollectionReplaceEvent<CountEditableItem<Model.Inventory.Item>> e)
        {
            if (ReferenceEquals(e.NewValue, null))
            {
                UpdateStagedItems();
            }
        }

        private void SetActiveCombinationButton(bool isActive)
        {
            if (isActive)
            {
                combinationButton.enabled = true;
                combinationButtonImage.sprite = Resources.Load<Sprite>("ui/button_blue_02");
            }
            else
            {
                combinationButton.enabled = false;
                combinationButtonImage.sprite = Resources.Load<Sprite>("ui/button_black_01");
            }
        }

        private IDisposable _combinationDisposable;

        private void RequestCombination(Model.Combination data)
        {
            _loadingScreen.Show();
            _combinationDisposable = Action.Combination.EndOfExecuteSubject.ObserveOnMainThread().Subscribe(ResponseCombination);
            ActionManager.instance.Combination(_data.StagedItems.ToList());
            AnalyticsManager.instance.OnEvent(AnalyticsManager.EventName.ClickCombinationCombination);
        }

        /// <summary>
        /// 결과를 직접 받아서 데이타에 넣어주는 방법 보다는,
        /// 네트워크 결과를 핸들링하는 곳에 핸들링 인터페이스를 구현한 데이타 모델을 등록하는 방법이 좋겠다. 
        /// </summary>
        private void ResponseCombination(Action.Combination action)
        {
            //FIXME Block.Validate 시 이벤트가 호출되는 문제가 있음.
            //액션이 처리되서 아바타가 변경되었다는 이벤트를 받았을때만 호출되야함.
            _count++;
            if (_count <= 1)
                return;

            _combinationDisposable.Dispose();
            
            var result = action.Result;
            if (result.ErrorCode == ActionBase.ErrorCode.Success)
            {
                ItemEquipment itemData;
                if (!Tables.instance.ItemEquipment.TryGetValue(result.Item.id, out itemData))
                {
                    _loadingScreen.Close();
                    throw new InvalidActionException("`Combination` action's `Result` is invalid.");
                }

                var itemModel = new Model.Inventory.Item(
                    new Game.Item.Inventory.InventoryItem(new Equipment(itemData), action.Result.Item.count));

                _data.ResultPopup.Value = new CombinationResultPopup<Model.Inventory.Item>()
                {
                    IsSuccess = true,
                    ResultItem = itemModel,
                    MaterialItems = _data.StagedItems
                };
                
                AnalyticsManager.instance.OnEvent(AnalyticsManager.EventName.ActionCombinationSuccess);
            }
            else
            {
                _data.ResultPopup.Value = new CombinationResultPopup<Model.Inventory.Item>()
                {
                    IsSuccess = false,
                    MaterialItems = _data.StagedItems
                };
                
                AnalyticsManager.instance.OnEvent(AnalyticsManager.EventName.ActionCombinationFail);
            }
        }

        private void SubscribeResultPopup(CombinationResultPopup<Model.Inventory.Item> data)
        {
            if (ReferenceEquals(data, null))
            {
                _resultPopup.Close();
            }
            else
            {
                _loadingScreen.Close();
                _resultPopup.Pop(_data.ResultPopup.Value);
            }
        }

        private void UpdateStagedItems(int startIndex = 0)
        {
            var dataCount = _data.StagedItems.Count;
            for (var i = startIndex; i < stagedItems.Length; i++)
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
    }
}
