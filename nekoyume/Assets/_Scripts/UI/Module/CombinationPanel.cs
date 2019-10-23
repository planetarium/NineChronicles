using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Helper;
using Nekoyume.Model;
using Nekoyume.UI.Model;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public interface ICombinationPanel
    {
        int CostNCG { get; }
        int CostAP { get; }
    }

    public abstract class CombinationPanel<T> : MonoBehaviour, ICombinationPanel
        where T : CombinationPanel<T>
    {
        private readonly List<IDisposable> _disposablesAtShow = new List<IDisposable>();

        [CanBeNull] public CombinationMaterialView baseMaterial;
        public List<CombinationMaterialView> otherMaterials;
        public GameObject costNCG;
        public TextMeshProUGUI costNCGText;
        public GameObject costAP;
        public TextMeshProUGUI costAPText;
        public Button submitButton;
        public TextMeshProUGUI submitButtonText;

        public readonly Subject<InventoryItem> OnMaterialAdd = new Subject<InventoryItem>();
        public readonly Subject<InventoryItem> OnBaseMaterialAdd = new Subject<InventoryItem>();
        public readonly Subject<InventoryItem> OnOtherMaterialAdd = new Subject<InventoryItem>();
        public readonly Subject<InventoryItem> OnMaterialRemove = new Subject<InventoryItem>();
        public readonly Subject<InventoryItem> OnBaseMaterialRemove = new Subject<InventoryItem>();
        public readonly Subject<InventoryItem> OnOtherMaterialRemove = new Subject<InventoryItem>();
        public readonly Subject<int> OnCostNCGChange = new Subject<int>();
        public readonly Subject<int> OnCostAPChange = new Subject<int>();

        public readonly Subject<CombinationPanel<T>> OnSubmitClick =
            new Subject<CombinationPanel<T>>();

        public int CostNCG { get; private set; }
        public int CostAP { get; private set; }
        
        public abstract bool IsSubmittable { get; }

        #region Initialize & Terminate

        protected virtual void Awake()
        {
            if (!(baseMaterial is null))
            {
                InitMaterialView(baseMaterial);
            }

            foreach (var otherMaterial in otherMaterials)
            {
                InitMaterialView(otherMaterial);
            }

            submitButton.OnClickAsObservable().Subscribe(_ =>
            {
                AudioController.PlayClick();
                OnSubmitClick.OnNext(this);
            }).AddTo(gameObject);
        }

        protected virtual void OnDestroy()
        {
            OnMaterialAdd.Dispose();
            OnBaseMaterialAdd.Dispose();
            OnOtherMaterialAdd.Dispose();
            OnMaterialRemove.Dispose();
            OnBaseMaterialRemove.Dispose();
            OnOtherMaterialRemove.Dispose();
            OnCostNCGChange.Dispose();
            OnCostAPChange.Dispose();
            OnSubmitClick.Dispose();
        }

        private void InitMaterialView(CombinationMaterialView view)
        {
            view.OnClick.Subscribe(_ =>
                {
                    var tooltip = Widget.Find<ItemInformationTooltip>();
                    if (tooltip.Target &&
                        tooltip.Target.Equals(_.RectTransform))
                        tooltip.Close();
                    else
                        tooltip.Show(_.RectTransform, _.Model);
                })
                .AddTo(gameObject);
            view.OnRightClick.Subscribe(_ =>
            {
                Debug.LogWarning("view.OnRightClick!!");
                if (_ is CombinationMaterialView materialView)
                {
                    TryRemoveMaterial(materialView);
                }
            }).AddTo(gameObject);
            view.OnCountChange.Subscribe(_ => OnMaterialCountChanged()).AddTo(gameObject);
        }

        #endregion

        public virtual void Show()
        {
            gameObject.SetActive(true);
            ReactiveAgentState.Gold.Subscribe(SubscribeNCG).AddTo(_disposablesAtShow);
            ReactiveCurrentAvatarState.ActionPoint.Subscribe(SubscribeActionPoint).AddTo(_disposablesAtShow);
            OnMaterialCountChanged();
        }

        public virtual void Hide()
        {
            _disposablesAtShow.DisposeAllAndClear();

            RemoveMaterialsAll();

            gameObject.SetActive(false);
        }

        public virtual bool DimFunc(InventoryItem inventoryItem)
        {
            if (inventoryItem.ItemBase.Value is ItemUsable itemUsable)
            {
                if (!(baseMaterial is null))
                {
                    if (baseMaterial.InventoryItemViewModel?.ItemBase.Value is ItemUsable itemUsable2)
                    {
                        if (itemUsable.ItemId.Equals(itemUsable2.ItemId))
                            return true;
                    }
                }

                foreach (var otherMaterial in otherMaterials)
                {
                    if (otherMaterial.InventoryItemViewModel?.ItemBase.Value is ItemUsable itemUsable2)
                    {
                        if (itemUsable.ItemId.Equals(itemUsable2.ItemId))
                            return true;
                    }
                }

                return false;
            }

            if (!(baseMaterial is null) &&
                !(baseMaterial.InventoryItemViewModel is null))
            {
                if (baseMaterial.InventoryItemViewModel.ItemBase.Value.Data.Id.Equals(
                    inventoryItem.ItemBase.Value.Data.Id))
                    return true;
            }

            foreach (var otherMaterial in otherMaterials)
            {
                if (!(otherMaterial.InventoryItemViewModel is null) &&
                    otherMaterial.InventoryItemViewModel.ItemBase.Value.Data.Id.Equals(
                        inventoryItem.ItemBase.Value.Data.Id))
                    return true;
            }

            return false;
        }

        protected abstract int GetCostNCG();
        protected abstract int GetCostAP();

        #region Add Material

        public virtual bool TryAddMaterial(InventoryItemView view)
        {
            if (view is null ||
                view.Model is null)
                return false;

            if (TryAddBaseMaterial(view))
            {
                OnMaterialAdd.OnNext(view.Model);
                OnBaseMaterialAdd.OnNext(view.Model);
                OnMaterialCountChanged();
                return true;
            }

            if (TryAddOtherMaterial(view))
            {
                OnMaterialAdd.OnNext(view.Model);
                OnOtherMaterialAdd.OnNext(view.Model);
                OnMaterialCountChanged();
                return true;
            }

            return false;
        }

        protected virtual bool TryAddBaseMaterial(InventoryItemView view)
        {
            if (baseMaterial is null)
                return false;

            if (!(baseMaterial.InventoryItemViewModel is null))
            {
                OnMaterialRemove.OnNext(baseMaterial.InventoryItemViewModel);
                OnBaseMaterialRemove.OnNext(baseMaterial.InventoryItemViewModel);
            }

            baseMaterial.Set(view);
            return true;
        }

        protected virtual bool TryAddOtherMaterial(InventoryItemView view)
        {
            var sameMaterial = otherMaterials.FirstOrDefault(e =>
            {
                if (e.Model?.ItemBase.Value is null ||
                    view.Model?.ItemBase.Value is null)
                    return false;

                return e.Model.ItemBase.Value.Data.Id == view.Model.ItemBase.Value.Data.Id;
            });
            if (sameMaterial is null)
            {
                // 새로 더하기.
                var possibleMaterial = otherMaterials.FirstOrDefault(e => e.IsEmpty && !e.IsLocked);
                if (possibleMaterial is null)
                {
                    // 제료가 이미 가득 찼어요!
                    return false;
                }

                possibleMaterial.Set(view);
                return true;
            }

            // 하나 증가.
            sameMaterial.IncreaseCount();
            return true;
        }

        #endregion

        #region Remove Material

        public virtual bool TryRemoveMaterial(CombinationMaterialView view)
        {
            if (view is null ||
                view.Model is null)
                return false;

            var inventoryItemView = view.InventoryItemViewModel;

            if (TryRemoveBaseMaterial(view))
            {
                OnMaterialRemove.OnNext(inventoryItemView);
                OnBaseMaterialRemove.OnNext(inventoryItemView);
                OnMaterialCountChanged();
                return true;
            }

            if (TryRemoveOtherMaterial(view))
            {
                OnMaterialRemove.OnNext(inventoryItemView);
                OnOtherMaterialRemove.OnNext(inventoryItemView);
                OnMaterialCountChanged();
                return true;
            }

            return false;
        }

        protected virtual bool TryRemoveBaseMaterial(CombinationMaterialView view)
        {
            if (baseMaterial is null ||
                baseMaterial.Model?.ItemBase.Value is null ||
                view is null ||
                view.Model?.ItemBase.Value is null ||
                baseMaterial.Model.ItemBase.Value.Data.Id != view.Model.ItemBase.Value.Data.Id)
                return false;

            baseMaterial.Set(null);
            return true;
        }

        protected virtual bool TryRemoveOtherMaterial(CombinationMaterialView view)
        {
            var sameMaterial = otherMaterials.FirstOrDefault(e =>
            {
                if (e.Model?.ItemBase.Value is null ||
                    view.Model?.ItemBase.Value is null)
                    return false;

                return e.Model.ItemBase.Value.Data.Id == view.Model.ItemBase.Value.Data.Id;
            });
            if (sameMaterial is null)
                return false;

            sameMaterial.Set(null);
            return true;
        }

        public virtual void RemoveMaterialsAll()
        {
            if (!(baseMaterial is null))
            {
                OnMaterialRemove.OnNext(baseMaterial.InventoryItemViewModel);
                baseMaterial.Set(null);
            }

            foreach (var material in otherMaterials)
            {
                OnMaterialRemove.OnNext(material.InventoryItemViewModel);
                material.Set(null);
            }

            OnMaterialCountChanged();
        }

        #endregion

        protected virtual void SubscribeNCG(decimal ncg)
        {
            if (CostNCG > 0)
            {
                costNCG.SetActive(true);
                costNCGText.text = CostNCG.ToString();
                costNCGText.color = ncg >= CostNCG ? Color.white : Color.red;
            }
            else
            {
                costNCG.SetActive(false);
            }
        }

        protected virtual void SubscribeActionPoint(int actionPoint)
        {
            if (CostAP > 0)
            {
                costAP.SetActive(true);
                costAPText.text = CostAP.ToString();
                costAPText.color = actionPoint >= CostAP ? Color.white : Color.red;
            }
            else
            {
                costAP.SetActive(false);
            }
        }

        protected virtual void OnMaterialCountChanged()
        {
            CostNCG = GetCostNCG();
            SubscribeNCG(ReactiveAgentState.Gold.Value);
            CostAP = GetCostAP();
            SubscribeActionPoint(ReactiveCurrentAvatarState.ActionPoint.Value);
            OnCostAPChange.OnNext(CostAP);
            UpdateSubmittable();
        }

        private void UpdateSubmittable()
        {
            if (IsSubmittable)
            {
                submitButton.enabled = true;
                submitButtonText.color = Color.white;
            }
            else
            {
                submitButton.enabled = false;
                submitButtonText.color = ColorHelper.HexToColorRGB("92A3B5");
            }
        }
    }
}
