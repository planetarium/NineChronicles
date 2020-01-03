using DG.Tweening;
using Nekoyume.UI.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CombinationMaterialView : CountEditableItemView<CombinationMaterial>, ILockable
    {
        public Image ncgEffectImage;
        public Image effectImage;
        public Image[] effectImages;

        public bool IsLocked => !itemButton.interactable;

        public InventoryItem InventoryItemViewModel { get; private set; }
        
        private bool _isUnlockedAsNCG;
        private bool _isTwinkledOn;
        private Tweener _twinkleTweener;
        private Tweener _setTweener;

        private void OnDisable()
        {
            _twinkleTweener?.Kill();
            _setTweener?.Kill();
        }

        public virtual void Set(InventoryItem inventoryItemViewModel, int count = 1)
        {
            if (inventoryItemViewModel is null ||
                inventoryItemViewModel.ItemBase.Value is null)
            {
                Clear();
                return;
            }
            
            var model = new CombinationMaterial(
                inventoryItemViewModel.ItemBase.Value,
                count,
                1,
                inventoryItemViewModel.Count.Value);
            base.SetData(model);
            SetTwinkled(_isTwinkledOn);
            SetEnableEffectImages(true);
            InventoryItemViewModel = inventoryItemViewModel;
            
            _setTweener?.Kill();
            var origin = iconImage.transform.localScale;
            iconImage.transform.localScale = Vector3.zero;
            _setTweener = iconImage.transform
                .DOScale(origin, 1f)
                .SetEase(Ease.OutElastic);
            _setTweener.onKill = () => iconImage.transform.localScale = origin;
        }

        public override void Clear()
        {
            _setTweener?.Kill();
            
            InventoryItemViewModel = null;
            ncgEffectImage.enabled = _isUnlockedAsNCG;
            effectImage.enabled = false;
            SetTwinkled(false);
            SetEnableEffectImages(false);
            base.Clear();
        }

        public void Lock()
        {
            Clear();
            itemButton.interactable = false;
            backgroundImage.overrideSprite = Resources.Load<Sprite>("UI/Textures/ui_box_Inventory_05");
            backgroundImage.SetNativeSize();
            ncgEffectImage.enabled = false;
            effectImage.enabled = false;
            SetTwinkled(false);
        }
        
        public void Unlock()
        {
            itemButton.interactable = true;
            backgroundImage.overrideSprite = Resources.Load<Sprite>("UI/Textures/ui_box_Inventory_02");
            backgroundImage.SetNativeSize();
            ncgEffectImage.enabled = false;
            _isUnlockedAsNCG = false;
            SetTwinkled(_isTwinkledOn);
        }

        public void UnlockAsNCG()
        {
            itemButton.interactable = true;
            backgroundImage.overrideSprite = Resources.Load<Sprite>("UI/Textures/ui_box_Inventory_04");
            backgroundImage.SetNativeSize();
            ncgEffectImage.enabled = true;
            _isUnlockedAsNCG = true;
            SetTwinkled(_isTwinkledOn);
        }

        public void SetTwinkled(bool isOn)
        {
            _twinkleTweener?.Kill();
            var color = effectImage.color;
            
            if (isOn)
            {
                color.a = .2f;
                effectImage.color = color;
                color.a = 1f;
                _twinkleTweener = effectImage.DOColor(color, 1.5f).SetEase(Ease.InCubic).SetLoops(-1, LoopType.Yoyo);
            }
            else
            {
                color.a = 1f;
                effectImage.color = color;
            }
            
            effectImage.enabled = isOn;
            _isTwinkledOn = isOn;
        }

        private void SetEnableEffectImages(bool enable)
        {
            foreach (var effectImage in effectImages)
            {
                effectImage.enabled = enable;
            }
        }
    }
}
