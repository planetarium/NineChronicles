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
    class QuestRewardItem: AnimationWidget
    {
        private static Transform _inventoryTransform;

        public Image itemImage;
        
        public static void Show(SimpleCountableItemView view, int index)
        {
            var result = Create<QuestRewardItem>(true);

            result.itemImage.sprite = SpriteHelper.GetItemIcon(view.Model.ItemBase.Value.Data.Id);
            var rect = result.RectTransform;
            rect.anchoredPosition = view.gameObject.transform.position.ToCanvasPosition(ActionCamera.instance.Cam, MainCanvas.instance.Canvas);

            result.StartCoroutine(result.CoPlay(index));
        }

        private IEnumerator CoPlay(int index)
        {
            if (Equals(_inventoryTransform,null))
                UpdateInventoryTransform();

            VFXController.instance.Create<ItemMoveVFX>(transform.position);

            Vector3 midPath = new Vector3(_inventoryTransform.position.x + 0.5f * (index + 1), (_inventoryTransform.position.y + transform.position.y) / 2, _inventoryTransform.position.z);

            Vector3[] path = new Vector3[] { transform.position, midPath, _inventoryTransform.position };

            transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            Tweener tweenMove;
            tweenMove = transform.DOPath(path, 0.8f + 0.2f * index, PathType.CatmullRom).SetEase(Ease.OutSine);

            yield return new WaitWhile(tweenMove.IsPlaying);
            VFXController.instance.Create<ItemMoveVFX>(_inventoryTransform.position);
            Destroy(gameObject);
        }

        private void UpdateInventoryTransform()
        {
            _inventoryTransform = Find<BottomMenu>().inventoryButton.button.transform;
        }
    }
}
