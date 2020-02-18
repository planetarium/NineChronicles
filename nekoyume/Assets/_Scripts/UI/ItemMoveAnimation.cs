using Nekoyume.Game.VFX;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.UI.Module;
using Nekoyume.Game;
using Nekoyume.Helper;
using DG.Tweening;
using Nekoyume.Game.Controller;
using System.Collections;

namespace Nekoyume.UI
{
    class ItemMoveAnimation: AnimationWidget
    {
        private Vector3 _endPosition;
        public Image itemImage = null;
        
        public static ItemMoveAnimation Show(Sprite itemSprite, Vector3 startWorldPosition, Vector3 endWorldPosition, bool moveToLeft = false, float animationTime = 1f)
        {
            var result = Create<ItemMoveAnimation>(true);

            result.IsPlaying = true;
            result.itemImage.sprite = itemSprite;
            var rect = result.RectTransform;
            rect.anchoredPosition = startWorldPosition.ToCanvasPosition(ActionCamera.instance.Cam, MainCanvas.instance.Canvas);

            result._endPosition = endWorldPosition;
            result._animationTime = animationTime;

            result.StartCoroutine(result.CoPlay(moveToLeft));
            return result;
        }

        private IEnumerator CoPlay(bool moveToLeft)
        {
            VFXController.instance.Create<ItemMoveVFX>(transform.position);

            Vector3 midPath;
            if (moveToLeft)
                midPath = new Vector3(_endPosition.x - (Mathf.Abs(_endPosition.x - transform.position.x)), (_endPosition.y + transform.position.y) / 2, _endPosition.z);
            else
                midPath = new Vector3(_endPosition.x + (Mathf.Abs(_endPosition.x - transform.position.x)), (_endPosition.y + transform.position.y) / 2, _endPosition.z);

            Vector3[] path = new Vector3[] { transform.position, midPath, _endPosition };

            transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            Tweener tweenMove;
            tweenMove = transform.DOPath(path, _animationTime, PathType.CatmullRom).SetEase(Ease.OutSine);

            yield return new WaitWhile(tweenMove.IsPlaying);
            var vfx = VFXController.instance.Create<ItemMoveVFX>(_endPosition);
            Destroy(gameObject);

            yield return new WaitWhile(() => vfx.gameObject.activeSelf);
            IsPlaying = false;
        }
    }
}
