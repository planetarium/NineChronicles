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

        public Model.Combination Model { get; private set; }

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            this.ComponentFieldsNotNullTest();

            combinationButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    Model.onClickCombination.OnNext(Model);
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

            _stage = Game.Game.instance.stage;
            _stage.LoadBackground("combination");

            _player = _stage.GetPlayer();
            if (ReferenceEquals(_player, null))
            {
                throw new NotFoundComponentException<Player>();
            }

            _player.gameObject.SetActive(false);

            SetData(new Model.Combination(
                States.Instance.currentAvatarState.Value.inventory,
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

        private void SetData(Model.Combination model)
        {
            if (ReferenceEquals(model, null))
            {
                Clear();
                return;
            }

            _disposablesForSetData.DisposeAllAndClear();
            Model = model;
            Model.inventory.Value.selectedItemView.Subscribe(SubscribeInventorySelectedItem)
                .AddTo(_disposablesForSetData);
            Model.itemInfo.Value.item.Subscribe(OnItemInfoItem).AddTo(_disposablesForSetData);
            Model.itemCountPopup.Value.item.Subscribe(OnPopupItem).AddTo(_disposablesForSetData);
            Model.itemCountPopup.Value.onClickCancel.Subscribe(OnClickClosePopup).AddTo(_disposablesForSetData);
            Model.materials.ObserveAdd().Subscribe(OnAddStagedItems).AddTo(_disposablesForSetData);
            Model.materials.ObserveRemove().Subscribe(OnRemoveStagedItems).AddTo(_disposablesForSetData);
            Model.materials.ObserveReplace().Subscribe(_ => UpdateStagedItems()).AddTo(_disposablesForSetData);
            Model.readyForCombination.Subscribe(SetActiveCombinationButton).AddTo(_disposablesForSetData);
            Model.resultPopup.Subscribe(SubscribeResultPopup).AddTo(_disposablesForSetData);
            Model.onClickCombination.Subscribe(RequestCombination).AddTo(_disposablesForSetData);
            Model.onShowResultVFX.Subscribe(ShowResultVFX).AddTo(_disposablesForSetData);

            inventoryAndItemInfo.SetData(Model.inventory.Value, Model.itemInfo.Value);

            UpdateStagedItems();
        }

        private void Clear()
        {
            inventoryAndItemInfo.Clear();
            Model = null;
            _disposablesForSetData.DisposeAllAndClear();

            foreach (var item in materialViews)
            {
                item.Clear();
            }
        }

        private void UpdateStagedItems(int startIndex = 0)
        {
            var dataCount = Model.materials.Count;
            for (var i = startIndex; i < materialViews.Length; i++)
            {
                var item = materialViews[i];
                if (i < dataCount)
                {
                    item.SetData(Model.materials[i]);
                }
                else
                {
                    item.Clear();
                }
            }
        }

        private void SubscribeInventorySelectedItem(InventoryItemView view)
        {
            if (view is null)
            {
                return;
            }

            inventoryAndItemInfo.inventory.Tooltip.Show(
                view.RectTransform,
                view.Model,
                null,
                "재료 올리기",
                tooltip =>
                {
                    Model.RegisterToStagedItems(tooltip.itemInformation.Model.item.Value);
                    inventoryAndItemInfo.inventory.Tooltip.Close();
                });
        }

        private void OnItemInfoItem(InventoryItem data)
        {
            if (ReferenceEquals(data, null) ||
                data.dimmed.Value ||
                Model.IsMaterialsFulled)
            {
                Model.itemInfo.Value.buttonEnabled.Value = false;
            }
            else
            {
                Model.itemInfo.Value.buttonEnabled.Value = true;
            }
        }

        private void OnPopupItem(CountableItem data)
        {
            if (ReferenceEquals(data, null))
            {
                _simpleItemCountPopup.Close();
                return;
            }

            _simpleItemCountPopup.Pop(Model.itemCountPopup.Value);
        }

        private void OnClickClosePopup(Model.SimpleItemCountPopup data)
        {
            Model.itemCountPopup.Value.item.Value = null;
            _simpleItemCountPopup.Close();
        }

        private void OnAddStagedItems(CollectionAddEvent<CombinationMaterial> e)
        {
            if (e.Index >= materialViews.Length)
            {
                Model.materials.RemoveAt(e.Index);
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

            var dataCount = Model.materials.Count;
            for (var i = e.Index; i <= dataCount; i++)
            {
                var item = materialViews[i];

                if (i < dataCount)
                {
                    item.SetData(Model.materials[i]);
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
            var inventoryItemCount = States.Instance.currentAvatarState.Value.inventory.Items.Count();
            var materials = data.materials.ToList();
            foreach (var material in materials)
            {
                if (!States.Instance.currentAvatarState.Value.inventory.TryGetFungibleItem(material.item.Value.Data.id,
                    out var outFungibleItem))
                {
                    continue;
                }

                if (outFungibleItem.count == material.count.Value)
                {
                    inventoryItemCount--;
                }
            }

            ActionManager.instance.Combination(materials)
                .Subscribe(eval => ResponseCombination(inventoryItemCount))
                .AddTo(this);
            AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickCombinationCombination);
        }

        /// <summary>
        /// 결과를 직접 받아서 데이타에 넣어주는 방법 보다는,
        /// 네트워크 결과를 핸들링하는 곳에 핸들링 인터페이스를 구현한 데이타 모델을 등록하는 방법이 좋겠다. 
        /// </summary>
        private void ResponseCombination(int inventoryItemCount)
        {
            _loadingScreen.Close();

            var isSuccess = States.Instance.currentAvatarState.Value.inventory.Items.Count() > inventoryItemCount;

            Model.resultPopup.Value = new Model.CombinationResultPopup(isSuccess
                ? States.Instance.currentAvatarState.Value.inventory.TryGetNonFungibleItemFromLast(
                    out var outNonFungibleItem)
                    ? outNonFungibleItem
                    : null
                : null)
            {
                isSuccess = isSuccess,
                materialItems = Model.materials
            };

            AnalyticsManager.Instance.OnEvent(isSuccess
                ? AnalyticsManager.EventName.ActionCombinationSuccess
                : AnalyticsManager.EventName.ActionCombinationFail);
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

            // ToDo. 지금은 조합의 결과가 마지막에 더해지기 때문에 마지막 아이템을 갖고 오지만, 복수의 아이템을 한 번에 얻을 때에 대한 처리나 정렬 기능이 추가 되면 itemGuid로 갖고 와야함.
            var inventoryItem = Model.inventory.Value.items.Last();
            if (ReferenceEquals(inventoryItem, null))
            {
                yield break;
            }

            var index = Model.inventory.Value.items.Count - 1;
            var inventoryItemView = inventoryAndItemInfo.inventory.scrollerController.GetByIndex(index);
            if (ReferenceEquals(inventoryItemView, null))
            {
                yield break;
            }

            var position = inventoryItemView.transform.position;

            particleVFX.transform.position = _resultPopup.resultItem.transform.position;
            particleVFX.transform.DOMoveX(position.x, 0.6f);
            particleVFX.transform.DOMoveY(position.y, 0.6f).SetEase(Ease.InCubic)
                .onComplete = () => { resultItemVFX.SetActive(true); };
            particleVFX.SetActive(true);
            resultItemVFX.transform.position = position;
        }
    }
}
