using Nekoyume.UI.Module;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.L10n;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class StageLoadingScreen : ScreenWidget
    {
        private const string SpriteAtlasPathFormat = "SpriteAtlases/Background/{0}";
        private const string SpriteNameFormat01 = "{0}_01";
        private const string SpriteNameFormat02 = "{0}_02";
        private const float ImageMargin = 700f;

        public List<Image> images;
        public bool closeEnd;
        public LoadingIndicator indicator;

        private bool _shouldClose;
        private List<RectTransform> _rects;

        private static Sprite GetSprite(string background, string spriteNameFormat)
        {
            var spriteAtlas = GetSpriteAtlas(background);
            if (spriteAtlas is null)
            {
                return null;
            }

            var spriteName = string.Format(spriteNameFormat, background.ToLower());
            var sprite = spriteAtlas.GetSprite(spriteName);
            if (sprite is null)
            {
                Debug.LogError($"Failed to get sprite in \"{spriteAtlas.name}\" by {spriteName}");
            }

            return sprite;
        }

        private static SpriteAtlas GetSpriteAtlas(string background)
        {
            var chapter = background.Substring(0, background.Length - 3);
            var spriteAtlasPath = string.Format(SpriteAtlasPathFormat, chapter);
            var spriteAtlas = Resources.Load<SpriteAtlas>(spriteAtlasPath);
            if (spriteAtlas is null)
            {
                Debug.LogError($"Failed to load SpriteAtlas in \"Assets/Resources/{spriteAtlasPath}\"");
            }

            return spriteAtlas;
        }

        public void Show(string background)
        {
            _shouldClose = false;
            _rects = new List<RectTransform>();
            var position = new Vector2(MainCanvas.instance.RectTransform.rect.width, 0f);
            for (var index = 0; index < images.Count; index++)
            {
                var image = images[index];
                var format = index % 2 == 0 ? SpriteNameFormat01 : SpriteNameFormat02;
                var sprite = GetSprite(background, format);
                image.gameObject.SetActive(true);
                image.overrideSprite = sprite;
                image.SetNativeSize();
                var rect = image.GetComponent<RectTransform>();
                rect.anchoredPosition = position;
                position.x += ImageMargin;
                _rects.Add(rect);
            }

            var message = L10nManager.Localize("BLOCK_CHAIN_MINING_TX") + "...";
            indicator.Show(message);
            base.Show();
            StartCoroutine(CoRun());
        }

        public override IEnumerator CoClose()
        {
            _shouldClose = true;
            yield return new WaitUntil(() => closeEnd);
            gameObject.SetActive(false);
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
    }
}
