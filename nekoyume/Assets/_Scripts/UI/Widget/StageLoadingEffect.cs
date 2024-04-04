using Nekoyume.UI.Module;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.UI.Tween;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Nekoyume.UI
{
    // TODO: 전투 씬 분리후 제거
    public class StageLoadingEffect : Widget
    {
        public override WidgetType WidgetType => WidgetType.Widget;

        private const string SpriteAtlasPathFormat = "SpriteAtlases/Background/{0}";
        private const string SpriteNameFormat01 = "{0}_01";
        private const string SpriteNameFormat02 = "{0}_02";
        private const float ImageMargin = 700f;

        [SerializeField]
        private DOTweenGroupAlpha loadingAlphaTweener;

        [SerializeField]
        private VideoPlayer skippableVideoPlayer;

        [SerializeField]
        private GameObject loadingVideoObject;

        [SerializeField]
        private GraphicAlphaTweener loadingDimTweener;

        private CanvasGroup _canvasGroup;

        public bool LoadingEnd { get; private set; } = true;
        public List<Image> images;
        public bool closeEnd;
        public bool dialogEnd;
        public LoadingModule loadingModule;

        private bool _shouldClose;
        private List<RectTransform> _rects;

        private const int WorkShopDialogId = 101;

        protected override void Awake()
        {
            base.Awake();

            // TODO: GetOrAddComponent
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            CloseWidget = null;

            loadingModule.Initialize();
        }

        protected override void Update()
        {
            base.Update();
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
                NcDebug.LogError($"Failed to get sprite in \"{spriteAtlas.name}\" by {spriteName}");
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
                NcDebug.LogError($"Failed to load SpriteAtlas in \"Assets/Resources/{spriteAtlasPath}\"");
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
            //background 값은 로딩뿐아니라 실제 BackGround에서 사용하고있고 로딩용 리소스는 1밖에 없기 때문에 예외처리.
            if (background == "chapter_08_03" || background == "chapter_08_02")
            {
                background = "chapter_08_01";
            }

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

            _canvasGroup.alpha = 1f;

            StartCoroutine(ShowSequence(stageType, worldName, stageId, isNext, clearedStageId));
            StartCoroutine(CoRun());
        }

        private IEnumerator ShowSequence(
            StageType stageType,
            string worldName,
            int stageId,
            bool isNext,
            int clearedStageId)
        {
            loadingModule.Close();
            dialogEnd = true;
            System.Func<IEnumerator> coroutine = null;
            AudioController.instance.PlayMusic(AudioController.MusicCode.BattleLoading);
            if (isNext)
            {
                yield return CoDialog(clearedStageId);
            }

            var message = string.Format(
                L10nManager.Localize("STAGE_BLOCK_CHAIN_MINING_TX"),
                worldName,
                StageInformation.GetStageIdString(stageType, stageId, true));
            loadingModule.Show(message);
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
            skippableVideoPlayer.SetDirectAudioVolume(0, AudioListener.volume);
            skippableVideoPlayer.gameObject.SetActive(true);
            skippableVideoPlayer.Prepare();
            loadingDimTweener.PlayForward().OnComplete(() =>
            {
                skippableVideoPlayer.Play();
                skippableVideoPlayer.Pause();
                loadingAlphaTweener.ResetToEndingValue();
                loadingDimTweener.PlayReverse().OnComplete(() =>
                {
                    skippableVideoPlayer.Play();
                });
            });

            yield return new WaitUntil(() => skippableVideoPlayer.isPlaying);
            yield return new WaitUntil(() => !skippableVideoPlayer.isPlaying);

            CompleteLoading();
        }

        private IEnumerator PlaySmallDialog()
        {
            LoadingEnd = false;
            loadingVideoObject.SetActive(true);
            var tweenEnd = false;
            loadingDimTweener.PlayForward().OnComplete(() =>
            {
                loadingAlphaTweener.ResetToEndingValue();
                loadingDimTweener.PlayReverse();
                Find<Tutorial>().PlaySmallGuide(WorkShopDialogId);
                tweenEnd = true;
            });

            yield return null;
            yield return new WaitUntil(() => !Find<Tutorial>().isActiveAndEnabled && tweenEnd);
            CompleteLoading();
        }

        public void CompleteLoading()
        {
            skippableVideoPlayer.Stop();
            loadingDimTweener.PlayForward().OnComplete(() =>
            {
                loadingAlphaTweener.ResetToBeginningValue();
                skippableVideoPlayer.gameObject.SetActive(false);
                loadingVideoObject.SetActive(false);
                loadingDimTweener.PlayReverse().OnComplete(() => LoadingEnd = true);
            });
        }
    }
}
