using Nekoyume.Pattern;
using Nekoyume.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nekoyume.Game
{
    [RequireComponent(typeof(SpriteMask))]
    public class UIToWorldMask : MonoSingleton<UIToWorldMask>
    {
        [SerializeField]
        private SpriteMask spriteMask = null;

        public void FitToRectTransform(RectTransform rectTransform)
        {
            var rect = rectTransform.rect;

            transform.position = rectTransform.position;
            transform.localScale = new Vector3(1f, 1f, 1f);

            var spriteSize = spriteMask.sprite.bounds.size;
            var width = spriteSize.x;
            var height = spriteSize.y;

            var targetWidth = rect.width;
            var targetHeight = rect.height;

            var canvasScale = MainCanvas.instance.RectTransform.localScale;
            var pixelSize = new Vector3(
                    targetWidth / width,
                    targetHeight / height,
                    1f);
            pixelSize.Scale(canvasScale);

            transform.localScale = pixelSize;
        }

        public void PushChild(Transform child)
        {
            child.parent = transform;
        }

        public void PopChild(Transform child, Transform originalParent)
        {
            if (!child.IsChildOf(transform))
                return;

            child.parent = originalParent;
        }

    }
}
