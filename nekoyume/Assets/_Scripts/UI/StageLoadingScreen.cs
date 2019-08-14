using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class StageLoadingScreen : ScreenWidget
    {
        private const string Bg1Format = "images/{0}_1";
        private const string Bg2Format = "images/{0}_2";
        private const string DefaultBgString = "chapter_1_1";
        private const float ImageMargin = 700f;

        public List<Image> images;
        public bool closeEnd;

        private bool _shouldClose;
        private List<RectTransform> _rects;

        private static Sprite Load(string format, string background)
        {
            var path = string.Format(format, background);
            var sprite = Resources.Load<Sprite>(path);
            if (ReferenceEquals(sprite, null))
            {
                path = string.Format(format, DefaultBgString);
                sprite = Resources.Load<Sprite>(path);
            }

            return sprite;
        }
        
        public void Show(string background)
        {
            _shouldClose = false;
            _rects = new List<RectTransform>();
            var position = new Vector2(MainCanvas.instance.RectTransform.rect.width, 0f);
            for (var index = 0; index < images.Count; index++)
            {
                var image = images[index];
                var format = index % 2 == 0 ? Bg1Format : Bg2Format;
                var sprite = Load(format, background);
                image.gameObject.SetActive(true);
                image.overrideSprite = sprite;
                image.SetNativeSize();
                var rect = image.GetComponent<RectTransform>();
                rect.anchoredPosition = position;
                position.x += ImageMargin;
                _rects.Add(rect);
            }

            base.Show();
            StartCoroutine(CoRun());
            StartCoroutine(CoWaitForQuit());
        }
        
        public override IEnumerator CoClose()
        {
            _shouldClose = true;
            yield return new WaitUntil(() => closeEnd);
        }
        
        private IEnumerator CoRun()
        {
            var delta = _rects.Average(r => r.rect.width);

            while (true)
            {
                foreach (var rect in _rects)
                {
                    var pos = rect.anchoredPosition;
                    var value = pos.x - ImageMargin * Time.deltaTime;
                    if (value < -rect.rect.width - delta)
                    {
                        if (!_shouldClose)
                        {
                            pos.x = -rect.rect.width + ImageMargin * images.Count;
                            rect.anchoredPosition = pos;
                        }
                        else
                        {
                            rect.gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        pos.x = value;
                        rect.anchoredPosition = pos;
                    }
                }

                closeEnd = images.All(i => i.gameObject.activeSelf == false);
                if (closeEnd) break;
                yield return null;
            }

            yield return null;
        }

        private IEnumerator CoWaitForQuit()
        {
            yield return new WaitForSeconds(GameConfig.WaitSeconds);
            if (isActiveAndEnabled)
                Find<ActionFailPopup>().Show();
        }
    }
}
