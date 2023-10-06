using Nekoyume.UI.Module;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.State;
using Nekoyume.UI.Tween;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Nekoyume.UI
{
    public class StageLoadingEffect : Widget
    {
        public override WidgetType WidgetType => WidgetType.Widget;

        private const string SpriteAtlasPathFormat = "SpriteAtlases/Background/{0}";
        private const string SpriteNameFormat01 = "{0}_01";
        private const string SpriteNameFormat02 = "{0}_02";
        private const float ImageMargin = 700f;

        [SerializeField]
        private VideoPlayer videoPlayer;

        [SerializeField]
        private DOTweenGroupAlpha loadingAlphaTweener;

        [SerializeField]
        private Image loadingImage;

        public bool LoadingEnd { get; private set; } = true;
        public List<Image> images;
        public bool closeEnd;
        public bool dialogEnd;
        public LoadingIndicator indicator;

        private bool _shouldClose;
        private List<RectTransform> _rects;

        private const int VideoPlayStage = 2;
        private const int WorkshopDialogStage = 3;

        protected override void Awake()
        {
            base.Awake();

            CloseWidget = null;
        }

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
            var chapter = background[..^3];
            var spriteAtlasPath = string.Format(SpriteAtlasPathFormat, chapter);
            var spriteAtlas = Resources.Load<SpriteAtlas>(spriteAtlasPath);
            if (spriteAtlas is null)
            {
                Debug.LogError($"Failed to load SpriteAtlas in \"Assets/Resources/{spriteAtlasPath}\"");
            }

            return spriteAtlas;
        }

        public void Show(
            StageType stageType,
            string background,
            string worldName,
            int stageId,
            bool isNext,
            int clearedStageId)
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

            base.Show();
            Find<HeaderMenuStatic>().Close();
            StartCoroutine(
                ShowSequence(stageType, worldName, stageId, isNext, clearedStageId));
            StartCoroutine(CoRun());
        }

        private IEnumerator ShowSequence(
            StageType stageType,
            string worldName,
            int stageId,
            bool isNext,
            int clearedStageId)
        {
            indicator.Close();
            dialogEnd = true;
            System.Func<IEnumerator> coroutine = null;
            if (isNext)
            {
                if (!States.Instance.CurrentAvatarState.worldInformation.IsStageCleared(stageId))
                {
                    switch (stageId)
                    {
                        case VideoPlayStage:
                            coroutine = PlayVideo;
                            LoadingEnd = false;
                            break;
                        case WorkshopDialogStage:
                            coroutine = PlaySmallDialog;
                            LoadingEnd = false;
                            break;
                        default: break;
                    }
                }

                yield return CoDialog(clearedStageId);
            }

            var message = string.Format(
                L10nManager.Localize("STAGE_BLOCK_CHAIN_MINING_TX"),
                worldName,
                StageInformation.GetStageIdString(stageType, stageId, true));
            indicator.Show(message);

            if (coroutine != null)
            {
                StartCoroutine(coroutine());
            }
        }

        private IEnumerator CoDialog(int worldStage)
        {
            dialogEnd = false;
            var stageDialogs = Game.Game.instance.TableSheets.StageDialogSheet.Values
                .Where(i => i.StageId == worldStage)
                .OrderBy(i => i.DialogId)
                .ToArray();
            if (!stageDialogs.Any())
            {
                dialogEnd = true;
                yield break;
            }

            var dialog = Widget.Find<DialogPopup>();
            foreach (var stageDialog in stageDialogs)
            {
                dialog.Show(stageDialog.DialogId);
                yield return new WaitWhile(() => dialog.gameObject.activeSelf);
            }

            dialogEnd = true;
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

                closeEnd = images.All(i => i.gameObject.activeSelf == false) && dialogEnd;
                if (closeEnd) break;
                yield return null;
            }

            yield return null;
        }

        private IEnumerator PlayVideo()
        {
            LoadingEnd = false;
            var audioController = AudioController.instance;
            audioController.StopAll(0.5f);
            videoPlayer.SetDirectAudioVolume(0, AudioListener.volume);
            videoPlayer.gameObject.SetActive(true);
            videoPlayer.Prepare();
            yield return new WaitUntil(() => videoPlayer.isPrepared);
            videoPlayer.Play();
            videoPlayer.Pause();
            loadingAlphaTweener.PlayForward().OnComplete(() =>
            {
                videoPlayer.Play();
            });

            yield return new WaitUntil(() => videoPlayer.isPlaying);
            yield return new WaitUntil(() => !videoPlayer.isPlaying);

            CompleteLoading();
        }

        private IEnumerator PlaySmallDialog()
        {
            LoadingEnd = false;
            loadingImage.gameObject.SetActive(true);
            loadingAlphaTweener.PlayForward();
            Find<Tutorial>().PlaySmallGuide(101);

            yield return null;
            yield return new WaitUntil(() => !Find<Tutorial>().isActiveAndEnabled);
            CompleteLoading();
        }

        public void CompleteLoading()
        {
            videoPlayer.Stop();
            var tween = loadingAlphaTweener.PlayReverse();
            tween.OnComplete(() =>
            {
                videoPlayer.gameObject.SetActive(false);
                loadingImage.gameObject.SetActive(false);
                LoadingEnd = true;
            });
        }
    }
}
