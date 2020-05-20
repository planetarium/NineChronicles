using System;
using System.Collections;
using Assets.SimpleLocalization;
using DG.Tweening;
using Nekoyume.Game.Controller;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    [ExecuteInEditMode]
    public class Synopsis : Widget
    {
        [Serializable]
        public class SynopsisScene
        {
            public enum ImageAnimationType
            {
                Fade,
                Immediately
            }
            public Image image;
            public Sprite sprite;
            public ImageAnimationType imageAnmationType;
            public float imageAnimationTime;
            public float imageAnimationEndTerm;


            public enum TextAnimationType
            {
                Type,
                Immediately,
                TypeAndFade,
                ImmediatelyAndFade
            }
            [Space]
            public TextMeshProUGUI texts;
            public string scriptsLocalizationKey;
            [NonSerialized] public string scripts;
            public TextAnimationType textAnimationTypes;
            public float scriptsAnimationTime;
            public float scriptsEndTerm;
        }
        public SynopsisScene[] scripts;
        public float textFadeOutTime = 0.5f;

        private bool skipSynopsis;

        protected override void Awake()
        {
            base.Awake();

            foreach (var script in scripts)
            {
                script.image.sprite = script.sprite;

                script.scripts =
                    LocalizationManager.Localize(script.scriptsLocalizationKey);
                script.texts.text = string.Empty;

                script.image.transform.parent.gameObject.SetActive(false);
            }

            CloseWidget = Skip;
            SubmitWidget = Skip;
        }

        private IEnumerator StartSynopsis()
        {
            var delayedTime = 0f;

            foreach (var script in scripts)
            {
                skipSynopsis = false;
                script.image.transform.parent.gameObject.SetActive(true);

                switch (script.textAnimationTypes)
                {
                    case SynopsisScene.TextAnimationType.TypeAndFade:
                        break;
                    case SynopsisScene.TextAnimationType.Type:
                        break;
                    case SynopsisScene.TextAnimationType.ImmediatelyAndFade:
                    case SynopsisScene.TextAnimationType.Immediately:
                        script.texts.text = script.scripts;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                switch (script.imageAnmationType)
                {
                    case SynopsisScene.ImageAnimationType.Fade:
                        var color = script.image.color;
                        color.a = 0;
                        script.image.color = color;

                        var tweener = script.image.DOFade(1, script.imageAnimationTime);
                        tweener.Play();

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
                    var tweener = script.texts.DOFade(0, textFadeOutTime);
                    tweener.Play();

                    yield return new WaitUntil(() => !tweener.IsPlaying() || skipSynopsis);
                }
                else
                {
                    script.texts.text = string.Empty;
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

            script.texts.text =
                $"<color=#ffffff00>{script.scripts}</color=#ffffff00>";

            for (var j = 0; j < script.scripts.Length; j++)
            {
                script.texts.text =
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

                script.texts.text = script.scripts;

                if (skipSynopsis)
                {
                    break;
                }
            }
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            AudioController.instance.PlayMusic(AudioController.MusicCode.Prologue);
            StartCoroutine(StartSynopsis());
        }

        public void End()
        {
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
