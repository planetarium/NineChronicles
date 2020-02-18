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
        private Vector3 endPosition;
        public Image itemImage = null;
        
        public static void Show(Sprite itemSprite, Vector3 startWorldPosition, Vector3 endWorldPosition, bool moveToLeft = false)
        {
            var result = Create<ItemMoveAnimation>(true);

            result.itemImage.sprite = itemSprite;
            var rect = result.RectTransform;
            rect.anchoredPosition = startWorldPosition.ToCanvasPosition(ActionCamera.instance.Cam, MainCanvas.instance.Canvas);

            result.endPosition = endWorldPosition;

            result.StartCoroutine(result.CoPlay(moveToLeft));
        }

        private IEnumerator CoPlay(bool moveToLeft)
        {
            VFXController.instance.Create<ItemMoveVFX>(transform.position);

            Vector3 midPath;
            if (moveToLeft)
                midPath = new Vector3(endPosition.x - (Mathf.Abs(endPosition.x - transform.position.x)), (endPosition.y + transform.position.y) / 2, endPosition.z);
            else
                midPath = new Vector3(endPosition.x + (Mathf.Abs(endPosition.x - transform.position.x)), (endPosition.y + transform.position.y) / 2, endPosition.z);

            Vector3[] path = new Vector3[] { transform.position, midPath, endPosition };

            transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            Tweener tweenMove;
            tweenMove = transform.DOPath(path, 0.8f, PathType.CatmullRom).SetEase(Ease.OutSine);

            yield return new WaitWhile(tweenMove.IsPlaying);
            VFXController.instance.Create<ItemMoveVFX>(endPosition);
            Destroy(gameObject);
        }
    }
}
