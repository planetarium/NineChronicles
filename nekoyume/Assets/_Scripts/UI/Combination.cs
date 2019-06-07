using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Manager;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.Data;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Stage = Nekoyume.Game.Stage;
using System.Collections;

namespace Nekoyume.UI
{
    public class Combination : Widget
    {
        private Model.Combination _data;

        public InventoryAndItemInfo inventoryAndItemInfo;
        public CombinationMaterialView[] materialViews;
        public Button combinationButton;
        public Image combinationButtonImage;
        public Button closeButton;

        public GameObject particleVFX;
        public GameObject resultItemVFX;

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

            SetData(new Model.Combination(
                States.Instance.currentAvatarState.Value.items,
                materialViews.Length));
            
            AudioController.instance.PlayMusic(AudioController.MusicCode.Combination);
        }
        
        public override void Close()
        {
            Clear();

            _stage.GetPlayer(_stage.roomPosition);
            _stage.LoadBackground("room");
            _player.gameObject.SetActive(true);

            Find<Menu>()?.ShowRoom();

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
            _data.itemInfo.Value.item.Subscribe(OnItemInfoItem).AddTo(_disposablesForSetData);
            _data.itemCountPopup.Value.item.Subscribe(OnPopupItem).AddTo(_disposablesForSetData);
            _data.itemCountPopup.Value.onClickClose.Subscribe(OnClickClosePopup).AddTo(_disposablesForSetData);
            _data.materials.ObserveAdd().Subscribe(OnAddStagedItems).AddTo(_disposablesForSetData);
            _data.materials.ObserveRemove().Subscribe(OnRemoveStagedItems).AddTo(_disposablesForSetData);
            _data.materials.ObserveReplace().Subscribe(_ => UpdateStagedItems()).AddTo(_disposablesForSetData);
            _data.readyForCombination.Subscribe(SetActiveCombinationButton).AddTo(_disposablesForSetData);
            _data.resultPopup.Subscribe(SubscribeResultPopup).AddTo(_disposablesForSetData);
            _data.onClickCombination.Subscribe(RequestCombination).AddTo(_disposablesForSetData);
            _data.onShowResultVFX.Subscribe(ShowResultVFX).AddTo(_disposablesForSetData);
            inventoryAndItemInfo.SetData(_data.inventory.Value, _data.itemInfo.Value);
            
            UpdateStagedItems();
        }

        private void Clear()
        {
            inventoryAndItemInfo.Clear();
            _data = null;
            _disposablesForSetData.DisposeAllAndClear();
            
            foreach (var item in materialViews)
            {
                item.Clear();
            }
        }
        
        private void UpdateStagedItems(int startIndex = 0)
        {
            var dataCount = _data.materials.Count;
            for (var i = startIndex; i < materialViews.Length; i++)
            {
                var item = materialViews[i];
                if (i < dataCount)
                {
                    item.SetData(_data.materials[i]);
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
                _data.IsMaterialsFulled)
            {
                _data.itemInfo.Value.buttonEnabled.Value = false;
            }
            else
            {
                _data.itemInfo.Value.buttonEnabled.Value = true;
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

        private void OnAddStagedItems(CollectionAddEvent<CombinationMaterial> e)
        {
            if (e.Index >= materialViews.Length)
            {
                _data.materials.RemoveAt(e.Index);
                throw new AddOutOfSpecificRangeException<CollectionAddEvent<CountEditableItem>>(
                    materialViews.Length);
            }

            materialViews[e.Index].SetData(e.Value);
        }

        private void OnRemoveStagedItems(CollectionRemoveEvent<CombinationMaterial> e)
        {
            if (e.Index >= materialViews.Length)
            {
                return;
            }

            var dataCount = _data.materials.Count;
            for (var i = e.Index; i <= dataCount; i++)
            {
                var item = materialViews[i];

                if (i < dataCount)
                {
                    item.SetData(_data.materials[i]);
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
            ActionManager.instance.Combination(_data.materials.ToList())
                .Subscribe(ResponseCombination)
                .AddTo(this);
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickCombinationCombination);
        }

        /// <summary>
        /// 결과를 직접 받아서 데이타에 넣어주는 방법 보다는,
        /// 네트워크 결과를 핸들링하는 곳에 핸들링 인터페이스를 구현한 데이타 모델을 등록하는 방법이 좋겠다. 
        /// </summary>
        private void ResponseCombination(ActionBase.ActionEvaluation<Action.Combination> eval)
        {
            if (eval.Action.errorCode != GameAction.ErrorCode.Success)
            {
                _data.resultPopup.Value = new Model.CombinationResultPopup(null, 0)
                {
                    isSuccess = false,
                    materialItems = _data.materials
                };
                
                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionCombinationFail);
                _loadingScreen.Close();
                return;
            }
            
            var result = eval.Action.Result;
            if (!Tables.instance.TryGetItemEquipment(result.Item.id, out var itemEquipment))
            {
                _loadingScreen.Close();
                throw new InvalidActionException("`Combination` action's `Result` is invalid.");
            }

            _data.resultPopup.Value = new Model.CombinationResultPopup(ItemBase.ItemFactory(itemEquipment), result.Item.count)
            {
                isSuccess = true,
                materialItems = _data.materials
            };
            
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ActionCombinationSuccess);
            _loadingScreen.Close();
        }

        private void SubscribeResultPopup(Model.CombinationResultPopup data)
        {
            if (ReferenceEquals(data, null))
            {
                _resultPopup.Close();
                return;
            }
            
            _resultPopup.Pop(data);
        }

        private void ShowResultVFX(Model.CombinationResultPopup data)
        {
            StartCoroutine(CoShowResultVFX(data));
        }

        private IEnumerator CoShowResultVFX(Model.CombinationResultPopup data)
        {
            if (!data.isSuccess)
            {
                yield break;
            }
            
            yield return null;
            particleVFX.SetActive(false);
            resultItemVFX.SetActive(false);
            
            var inventoryItem = _data.inventory.Value.items.Single(i => i.item.Value.Data.id == data.item.Value.Data.id);
            if (ReferenceEquals(inventoryItem, null))
            {
                yield break;
            }
            
            var index = _data.inventory.Value.items.IndexOf(inventoryItem);
            var inventoryItemView = inventoryAndItemInfo.inventory.scrollerController.GetByIndex(index);
            if (ReferenceEquals(inventoryItemView, null))
            {
                yield break;
            }

            var position = inventoryItemView.transform.position;
            
            particleVFX.transform.position = _resultPopup.resultItem.transform.position;
            particleVFX.transform.DOMoveX(position.x, 0.6f);
            particleVFX.transform.DOMoveY(position.y, 0.6f).SetEase(Ease.InCubic)
                .onComplete = () =>{
                resultItemVFX.SetActive(true);
            };
            particleVFX.SetActive(true);
            resultItemVFX.transform.position = position;
        }
    }
}
