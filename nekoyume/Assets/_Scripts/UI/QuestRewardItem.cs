using System;
using System.Collections;
using Nekoyume.Model.Item;
using Nekoyume.Game.VFX;
using UnityEngine;
using UnityEngine.UI;
using Nekoyume.UI.Module;
using Nekoyume.Game;
using Nekoyume.Helper;
using DG.Tweening;
using Nekoyume.Game.Controller;

namespace Nekoyume.UI
{
    class QuestRewardItem: HudWidget
    {
        private static Transform _inventoryTransform;

        public Canvas canvas;
        public Image itemImage;
        
        public static void Show(SimpleCountableItemView view, int index)
        {
            var result = Create<QuestRewardItem>(true);
            result.canvas.sortingLayerName = "UI";
            result.itemImage.sprite = SpriteHelper.GetItemIcon(view.Model.ItemBase.Value.Data.Id);
            var rect = result.RectTransform;
            rect.anchoredPosition = view.gameObject.transform.position.ToCanvasPosition(ActionCamera.instance.Cam, MainCanvas.instance.Canvas);

            result.CoPlay(index);
        }

        private void CoPlay(int index)
        {
            if (Equals(_inventoryTransform,null))
                UpdateInventoryTransform();

            Sequence seq = DOTween.Sequence();

            seq.AppendCallback(() => VFXController.instance.Create<ItemMoveVFX>(transform, Vector3.zero));

            var midPath = new Vector3(_inventoryTransform.position.x + 0.5f * (index + 1), (_inventoryTransform.position.y + transform.position.y) / 2, _inventoryTransform.position.z);
            Vector3[] path = new Vector3[] { transform.position, midPath, _inventoryTransform.position };
            seq.Append(transform.DOPath(path, 0.8f + 0.2f * index, PathType.CatmullRom).SetEase(Ease.OutCubic));

            seq.AppendCallback(() => VFXController.instance.Create<ItemMoveVFX>(_inventoryTransform, Vector3.zero));
            seq.AppendCallback(() => Destroy(gameObject));
            seq.Append(_inventoryTransform.DOScale(1.2f, 0.1f));
            seq.Append(_inventoryTransform.DOScale(1.0f, 0.5f).SetEase(Ease.OutBack));
            seq.Play();
        }

        private void UpdateInventoryTransform()
        {
            _inventoryTransform = Find<BottomMenu>().inventoryButton.button.transform;
        }
    }
}
