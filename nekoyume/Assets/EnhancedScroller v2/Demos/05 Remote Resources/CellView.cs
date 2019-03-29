using UnityEngine;
using UnityEngine.UI;
using EnhancedUI.EnhancedScroller;
using System.Collections;

namespace EnhancedScrollerDemos.RemoteResourcesDemo
{
    public class CellView : EnhancedScrollerCellView
    {
        public Image cellImage;
        public Sprite defaultSprite;

        public void SetData(Data data)
        {
            StartCoroutine(LoadRemoteImage(data));
        }

        public IEnumerator LoadRemoteImage(Data data)
        {
            string path = data.imageUrl;
            WWW www = new WWW(path);
            yield return www;

            cellImage.sprite = Sprite.Create(www.texture, new Rect(0, 0, data.imageDimensions.x, data.imageDimensions.y), new Vector2(0, 0), data.imageDimensions.x);
        }

        public void ClearImage()
        {
            cellImage.sprite = defaultSprite;
        }
    }
}