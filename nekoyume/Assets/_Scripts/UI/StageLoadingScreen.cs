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

        public List<Image> images;
        public bool closeEnd;

        private BattleResult _battleResult;
        private List<RectTransform> _rects;
        private const float ImageMargin = 700f;

        public void Show(BattleResult result, string background)
        {
            _battleResult = result;
            _rects = new List<RectTransform>();
            var position = new Vector2(MainCanvas.instance.RectTransform.rect.width, 0f);
            for (var index = 0; index < images.Count; index++)
            {
                var image = images[index];
                var format = index % 2 == 0 ? Bg1Format : Bg2Format;
                var sprite = Load(format, background);
                image.overrideSprite = sprite;
                image.gameObject.SetActive(true);
                image.SetNativeSize();
                var rect = image.GetComponent<RectTransform>();
                rect.anchoredPosition = position;
                position.x += ImageMargin;
                _rects.Add(rect);
            }

            base.Show();
            StartCoroutine(CoRun());
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
                        if (!_battleResult.actionEnd)
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

        public override IEnumerator CoClose()
        {
            yield return new WaitUntil(() => closeEnd);
            yield return StartCoroutine(base.CoClose());
        }
    }
}
