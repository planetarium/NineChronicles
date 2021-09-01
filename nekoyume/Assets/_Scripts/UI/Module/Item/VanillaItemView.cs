using Coffee.UIEffects;
using DG.Tweening;
using Nekoyume.Helper;
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

        public virtual void SetData(ItemSheet.Row itemRow)
        {
            if (itemRow is null)
            {
                Clear();
                return;
            }

            var data = itemViewData.GetItemViewData(itemRow.Grade);
            gradeImage.overrideSprite = data.GradeBackground;

            gradeHsv.range = data.GradeHsvRange;
            gradeHsv.hue = data.GradeHsvHue;
            gradeHsv.saturation = data.GradeHsvSaturation;
            gradeHsv.value = data.GradeHsvValue;

            var itemSprite = SpriteHelper.GetItemIcon(itemRow.Id);
            if (itemSprite is null)
                throw new FailedToLoadResourceException<Sprite>(itemRow.Id.ToString());

            iconImage.enabled = true;
            iconImage.overrideSprite = itemSprite;
            iconImage.SetNativeSize();
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

        public void KillTween()
        {
            if (_tweener?.IsPlaying() ?? false)
            {
                _tweener?.Kill();
            }

            _tweener = null;
        }
    }
}
