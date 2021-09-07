using Nekoyume.Game.VFX;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.Game;
using DG.Tweening;
using Nekoyume.Game.Controller;
using System.Collections;
using Nekoyume.UI.Module;

namespace Nekoyume.UI
{
    public class ItemMoveAnimation : AnimationWidget
    {
        public enum EndPoint
        {
            None,
            Inventory,
            Workshop
        }

        [SerializeField] private Image itemImage;
        [SerializeField] private AnimationCurve moveAnimationCurve;

        private Vector3 _endPosition;
        private float _middleXGap;


        public static ItemMoveAnimation Show(Sprite itemSprite, Vector3 startWorldPosition,
            Vector3 endWorldPosition,
            Vector2 defaultScale, bool moveToLeft = false, bool playItemMoveVFXOnPlay = false,
            float animationTime = 1f, float middleXGap = 0f, EndPoint endPoint = EndPoint.None)
        {
            var result = Create<ItemMoveAnimation>(true);
            result.Show();
            result.IsPlaying = true;
            result.itemImage.sprite = itemSprite;
            var rect = result.RectTransform;
            rect.anchoredPosition = startWorldPosition.ToCanvasPosition(ActionCamera.instance.Cam,
                MainCanvas.instance.Canvas);
            rect.localScale = defaultScale;

            result._endPosition = endWorldPosition;
            result._animationTime = animationTime;
            result._middleXGap = middleXGap;

            result.StartCoroutine(result.CoPlay(defaultScale, moveToLeft, playItemMoveVFXOnPlay,
                endPoint));
            return result;
        }

        private IEnumerator CoPlay(Vector2 defaultScale, bool moveToLeft, bool playItemMoveVFXOnPlay, EndPoint endPoint)
        {
            if (playItemMoveVFXOnPlay)
            {
                VFXController.instance.Create<ItemMoveVFX>(transform.position);
            }

            Tweener tweenScale = transform.DOScale(defaultScale * 1.2f, 0.1f).SetEase(moveAnimationCurve);
            yield return new WaitWhile(tweenScale.IsPlaying);

            yield return new WaitForSeconds(0.5f);

            var midPath = moveToLeft
                ? new Vector3(transform.position.x - _middleXGap,
                    (_endPosition.y + transform.position.y) * 0.5f, _endPosition.z)
                : new Vector3(transform.position.x + _middleXGap,
                    (_endPosition.y + transform.position.y) * 0.5f, _endPosition.z);

            var path = new[] {transform.position, midPath, _endPosition};

            Tweener tweenMove = transform.DOPath(path, _animationTime, PathType.CatmullRom)
                .SetEase(Ease.OutSine);
            yield return new WaitForSeconds(_animationTime - 0.5f);

            Find<HeaderMenu>().PlayVFX(endPoint);

            yield return new WaitWhile(tweenMove.IsPlaying);
            itemImage.enabled = false;

            if (endPoint == EndPoint.None)
            {
                var vfx = VFXController.instance.Create<ItemMoveVFX>(_endPosition);
                yield return new WaitWhile(() => vfx.gameObject.activeSelf);
            }

            IsPlaying = false;

            Close();
        }
    }
}
