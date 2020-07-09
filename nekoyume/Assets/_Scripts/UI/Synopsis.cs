using System;
using System.Collections;
using System.Linq;
using Assets.SimpleLocalization;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Factory;
using Nekoyume.State;
using Spine.Unity;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using mixpanel;

namespace Nekoyume.UI
{
    public class Synopsis : Widget
    {
        [Serializable]
        public class SynopsisScene
        {
            public enum ImageAnimationType
            {
                FadeIn,
                FadeOut,
                Immediately
            }
            [Tooltip("페이드 혹은 나타날 사진이 찍히는 Image컴포넌트")]
            public Image image;
            [Tooltip("SkeletonAnimation이 들어가는 여부")]
            public bool hasSkeletonAnimation;
            [Tooltip("페이드 혹은 나타날 SkeletonAnimation")]
            public SkeletonAnimation skeletonAnimation;
            [Tooltip("페이드 혹은 나타날 사진")]
            public Sprite sprite;
            [Tooltip("이미지가 나타날때 방법")]
            public ImageAnimationType imageAnmationType;
            [Tooltip("이미지가 나타날때 걸리는 시간")]
            public float imageAnimationTime;
            [Tooltip("이미지가 로딩된 후 텍스트가 로딩되기 전까지 기다리는 시간")]
            public float imageAnimationEndTerm;

            public enum TextAnimationType
            {
                Type,
                Immediately,
                TypeAndFade,
                ImmediatelyAndFade
            }
            [Space]

            [Tooltip("글씨가 나타날 TextMeshPro 컴포넌트")]
            public TextMeshProUGUI text;
            [Tooltip("그림자가 나타날 TextMeshPro 컴포넌트")]
            public TextMeshProUGUI shadowText;
            [Tooltip("대사의 LocalizationKey")]
            public string scriptsLocalizationKey;
            [NonSerialized]
            public string scripts;
            [Tooltip("대사가 나타날때 방식")]
            public TextAnimationType textAnimationTypes;
            [Tooltip("대사가 전부 나타다는데 걸리는 시간")]
            public float scriptsAnimationTime;
            [Tooltip("대사가 전부 나온 뒤 기다리는 시간")]
            public float scriptsEndTerm;
        }
        public SynopsisScene[] scripts;
        [Tooltip("대사가 사라질때 걸리는 시간")]
        public float textFadeOutTime = 0.5f;

        private int _part1EndIndex = 4;
        public bool prolgueEnd;
        private bool skipSynopsis;

        protected override void Awake()
        {
            base.Awake();

            foreach (var script in scripts)
            {
                if (script.text != null)
                {
                    script.scripts =
                        LocalizationManager.Localize(script.scriptsLocalizationKey);
                    script.text.text = string.Empty;
                }

                script.image.transform.parent.gameObject.SetActive(false);

            }

            CloseWidget = Skip;
            SubmitWidget = Skip;
        }

        private IEnumerator StartSynopsis()
        {
            var delayedTime = 0f;

            var skipPrologue = States.Instance.AgentState.avatarAddresses.Any();
            var startIndex = 0;
            if (!skipPrologue && prolgueEnd)
            {
                startIndex = _part1EndIndex + 2;
            }
            for (var index = startIndex; index < scripts.Length; index++)
            {
                var script = scripts[index];
                if (index == _part1EndIndex && !skipPrologue)
                {
                    yield return StartCoroutine(Find<Blind>().FadeIn(2f, ""));
                    Close();
                    Game.Game.instance.prologue.StartPrologue();
                    yield return null;
                }

                skipSynopsis = false;
                script.image.transform.parent.gameObject.SetActive(true);
                script.image.overrideSprite = script.sprite;

                switch (script.textAnimationTypes)
                {
                    case SynopsisScene.TextAnimationType.TypeAndFade:
                        break;
                    case SynopsisScene.TextAnimationType.Type:
                        break;
                    case SynopsisScene.TextAnimationType.ImmediatelyAndFade:
                    case SynopsisScene.TextAnimationType.Immediately:
                        script.text.text = script.scripts;
                        script.shadowText.text = script.scripts;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                Color color;
                TweenerCore<Color, Color, ColorOptions> tweener;
                TweenerCore<float, float, FloatOptions> skeletonTweener = null;
                switch (script.imageAnmationType)
                {
                    case SynopsisScene.ImageAnimationType.FadeIn:
                        color = script.image.color;
                        color.a = 0;
                        script.image.color = color;

                        tweener = script.image.DOFade(1, script.imageAnimationTime);

                        if (script.hasSkeletonAnimation)
                        {
                            script.skeletonAnimation.skeleton.A = 1;
                            skeletonTweener = DOTween.To(() => script.skeletonAnimation.skeleton.A,
                                alpha => script.skeletonAnimation.skeleton.A = alpha, 0,
                                script.imageAnimationTime);
                        }

                        tweener.Play();
                        skeletonTweener?.Play();

                        yield return new WaitUntil(() => !tweener.IsPlaying() || skipSynopsis);

                        if (skipSynopsis)
                        {
                            tweener.Complete();
                        }

                        break;
                    case SynopsisScene.ImageAnimationType.FadeOut:
                        color = script.image.color;
                        color.a = 1;
                        script.image.color = color;

                        tweener = script.image.DOFade(0, script.imageAnimationTime);

                        if (script.hasSkeletonAnimation)
                        {
                            script.skeletonAnimation.skeleton.A = 0;
                            skeletonTweener = DOTween.To(() => script.skeletonAnimation.skeleton.A,
                                alpha => script.skeletonAnimation.skeleton.A = alpha, 1,
                                script.imageAnimationTime);
                        }

                        tweener.Play();
                        skeletonTweener?.Play();

                        yield return new WaitUntil(() => !tweener.IsPlaying() || skipSynopsis);

                        if (skipSynopsis)
                        {
                            tweener.Complete();
                        }

                        break;
                    case SynopsisScene.ImageAnimationType.Immediately:

                        script.image.sprite = script.sprite;

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (script.imageAnimationEndTerm > 0)
                {
                    delayedTime = 0f;
                    yield return new WaitUntil(() =>
                    {
                        if (delayedTime >= script.imageAnimationEndTerm || skipSynopsis)
                        {
                            return true;
                        }

                        delayedTime += Time.deltaTime;
                        return false;
                    });
                }

                if (skipSynopsis)
                {
                    continue;
                }

                if (script.scriptsLocalizationKey == string.Empty)
                {
                    script.image.transform.parent.gameObject.SetActive(false);
                    continue;
                }

                var fade = false;

                switch (script.textAnimationTypes)
                {
                    case SynopsisScene.TextAnimationType.TypeAndFade:
                        fade = true;
                        yield return StartCoroutine(TypingText(script));
                        break;
                    case SynopsisScene.TextAnimationType.Type:
                        yield return StartCoroutine(TypingText(script));
                        break;
                    case SynopsisScene.TextAnimationType.ImmediatelyAndFade:
                        fade = true;
                        break;
                    case SynopsisScene.TextAnimationType.Immediately:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                delayedTime = 0f;

                if (script.scriptsEndTerm > 0)
                {
                    yield return new WaitUntil(() =>
                    {
                        if (delayedTime >= script.scriptsEndTerm || skipSynopsis)
                        {
                            return true;
                        }

                        delayedTime += Time.deltaTime;
                        return false;
                    });
                }

                if (fade)
                {
                    var tweener1 = script.text.DOFade(0, textFadeOutTime);
                    var tweener2 = script.shadowText.DOFade(0, textFadeOutTime);
                    tweener1.Play();
                    tweener2.Play();

                    yield return new WaitUntil(() =>
                        (!tweener1.IsPlaying() && !tweener2.IsPlaying()) || skipSynopsis);
                }
                else
                {
                    script.text.text = string.Empty;
                }

                if (skipSynopsis)
                {
                    continue;
                }
                script.image.transform.parent.gameObject.SetActive(false);
            }
            End();

            yield return null;
        }

        private IEnumerator TypingText(SynopsisScene script)
        {
            var delayedTime = 0f;
            var characterPerTime =
                script.scriptsAnimationTime / script.scripts.Length;

            script.text.text =
                $"<color=#ffffff00>{script.scripts}</color=#ffffff00>";

            for (var j = 0; j <= script.scripts.Length; j++)
            {
                script.text.text =
                    $"{script.scripts.Substring(0, j)}<color=#ffffff00>{script.scripts.Substring(j)}</color=#ffffff00>";
                script.shadowText.text =
                    $"{script.scripts.Substring(0, j)}<color=#ffffff00>{script.scripts.Substring(j)}</color=#ffffff00>";

                delayedTime = 0f;
                yield return new WaitUntil(() =>
                {
                    if (delayedTime >= characterPerTime || skipSynopsis)
                    {
                        return true;
                    }

                    delayedTime += Time.deltaTime;
                    return false;
                });

                if (skipSynopsis)
                {
                    script.text.text = script.scripts;
                    script.shadowText.text = script.scripts;
                    break;
                }
            }
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            Mixpanel.Track("Unity/Synopsis");
            AudioController.instance.PlayMusic(AudioController.MusicCode.Prologue);
            StartCoroutine(StartSynopsis());
        }

        public void End()
        {
            PlayerFactory.Create();
            Game.Event.OnNestEnter.Invoke();
            Find<Login>().Show();
            Close();
        }

        public void Skip()
        {
            skipSynopsis = true;
        }
    }
}
