using Coffee.UIEffects;
using DG.Tweening;
using Nekoyume.Game;
using Nekoyume.Game.ScriptableObject;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class VanillaItemView : MonoBehaviour
    {
        [SerializeField] protected ItemViewDataScriptableObject itemViewData;

        public enum ImageSizeType
        {
            Small,
            Middle
        }

        public Image iconImage = null;

        [SerializeField]
        protected Image gradeImage = null;

        [SerializeField]
        protected UIHsvModifier gradeHsv = null;

        private Tweener _tweener;

        protected virtual ImageSizeType imageSizeType => ImageSizeType.Middle;

        protected virtual void OnDisable()
        {
            KillTween();
        }

        public virtual void Show()
        {
            gameObject.SetActive(true);
        }

        public void ShowWithScaleTween(float delay = default)
        {
            PlayTween(delay).OnPlay(Show);
        }

        public virtual void Hide()
        {
            gameObject.SetActive(false);
        }

        public virtual void SetData(ItemBase itemBase, System.Action onClick = null)
        {
            if (itemBase is null)
            {
                Clear();
                return;
            }

            var data = GetItemViewData(itemBase);
            gradeImage.overrideSprite = data.GradeBackground;

            gradeHsv.range = data.GradeHsvRange;
            gradeHsv.hue = data.GradeHsvHue;
            gradeHsv.saturation = data.GradeHsvSaturation;
            gradeHsv.value = data.GradeHsvValue;

            var iconResourceId = itemBase.Id.GetIconResourceId(TableSheets.Instance.ArenaSheet);
            var itemSprite = SpriteHelper.GetItemIcon(iconResourceId);
            if (itemSprite is null)
            {
                throw new FailedToLoadResourceException<Sprite>(iconResourceId.ToString());
            }

            iconImage.enabled = true;
            iconImage.overrideSprite = itemSprite;
            iconImage.SetNativeSize();
        }

        public void SetData(ItemSheet.Row itemRow, System.Action onClick = null)
        {
            var material = new Nekoyume.Model.Item.Material(itemRow as MaterialItemSheet.Row);
            SetData(material, onClick);
        }

        protected ItemViewData GetItemViewData(ItemBase itemBase)
        {
            // if itemBase is TradableMaterial, upgrade view data.
            var upgrade = itemBase is TradableMaterial ? 1 : 0;
            return itemViewData.GetItemViewData(itemBase.Grade + upgrade);
        }

        public virtual void Clear()
        {
            gradeImage.enabled = false;
            iconImage.enabled = false;
        }

        protected Tweener PlayTween(float delay = default)
        {
            KillTween();
            var iconTransform = iconImage.transform;
            var origin = iconTransform.localScale;
            iconTransform.localScale = Vector3.zero;
            _tweener = iconImage.transform
                .DOScale(origin, 1f)
                .SetDelay(delay)
                .SetEase(Ease.OutElastic)
                .OnKill(() => iconImage.transform.localScale = origin);

            return _tweener;
        }

        private void KillTween()
        {
            if (_tweener != null && _tweener.IsActive() && _tweener.IsPlaying())
            {
                _tweener.Kill();
                _tweener = null;
            }
        }
    }
}
