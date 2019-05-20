using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.Pattern;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nekoyume.Game.Controller
{
    public class AudioController : MonoSingleton<AudioController>
    {
        public struct MusicCode
        {
            public const string SelectCharacter = "bgm_selectcharacter";
            public const string Main = "bgm_main";
            public const string Shop = "bgm_shop";
            public const string Ranking = "bgm_ranking";
            public const string Combination = "bgm_combination";
            public const string StageGreen = "bgm_stage_green";
            public const string StageBlue = "bgm_stage_blue";
            public const string StageOrange = "bgm_stage_orange";
            public const string Boss1 = "bgm_boss1";
            public const string Win = "bgm_win";
            public const string Lose = "bgm_lose";
        }

        public struct SfxCode
        {
            public const string Select = "sfx_select";
            public const string Cash = "sfx_cash";
            public const string InputItem = "sfx_inputitem";
            public const string Success = "sfx_success";
            public const string Failed = "sfx_failed";
            public const string ChainMail2 = "sfx_chainmail2";
            public const string Equipment = "sfx_equipmount";
            public const string FootStepLow = "sfx_footstep-low";
            public const string FootStepHigh = "sfx_footstep-high";
            public const string DamageNormal = "sfx_damage_normal";
            public const string Critical01 = "sfx_critical01";
            public const string Critical02 = "sfx_critical02";
            public const string LevelUp = "sfx_levelup";
            public const string Cancel = "sfx_cancel";
            public const string Popup = "sfx_popup";
            public const string Click = "sfx_click";
            public const string Swing = "sfx_swing";
            public const string Swing2 = "sfx_swing2";
            public const string Swing3 = "sfx_swing3";
        }

        public enum State
        {
            None = -1,
            InInitializing,
            Idle
        }

        public State state { get; private set; }

        protected override bool ShouldRename => true;

        private readonly Dictionary<string, AudioSource> _musicPrefabs = new Dictionary<string, AudioSource>();
        private readonly Dictionary<string, AudioSource> _sfxPrefabs = new Dictionary<string, AudioSource>();

        private readonly Dictionary<string, Stack<AudioSource>> _musicPool =
            new Dictionary<string, Stack<AudioSource>>();

        private readonly Dictionary<string, Stack<AudioSource>> _sfxPool =
            new Dictionary<string, Stack<AudioSource>>();

        private readonly Dictionary<string, List<AudioSource>> _musicPlaylist =
            new Dictionary<string, List<AudioSource>>();

        private readonly Dictionary<string, List<AudioSource>> _sfxPlaylist =
            new Dictionary<string, List<AudioSource>>();

        private readonly Dictionary<string, List<AudioSource>> _shouldRemoveMusic =
            new Dictionary<string, List<AudioSource>>();

        private readonly Dictionary<string, List<AudioSource>> _shouldRemoveSfx =
            new Dictionary<string, List<AudioSource>>();

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            state = State.None;

            // Fix me.
            // 너무 짤랑 거려서 게임을 못하겠어요.
            // RewardGold.RewardGoldMyselfSubject.ObserveOnMainThread().Subscribe(_ => PlaySfx(SfxCode.Cash)).AddTo(this);
        }

        private void Update()
        {
            CheckPlaying(_musicPool, _musicPlaylist, _shouldRemoveMusic);
            CheckPlaying(_sfxPool, _sfxPlaylist, _shouldRemoveSfx);
        }

        private void CheckPlaying<T1, T2>(T1 pool, T2 playList, T2 shouldRemove)
            where T1 : IDictionary<string, Stack<AudioSource>> where T2 : IDictionary<string, List<AudioSource>>
        {
            foreach (var pair in playList)
            {
                foreach (var audioSource in pair.Value)
                {
                    if (audioSource.isPlaying)
                    {
                        continue;
                    }

                    if (!shouldRemove.ContainsKey(pair.Key))
                    {
                        shouldRemove.Add(pair.Key, new List<AudioSource>());
                    }

                    shouldRemove[pair.Key].Add(audioSource);
                }
            }

            foreach (var pair in shouldRemove)
            {
                foreach (var audioSource in pair.Value)
                {
                    playList[pair.Key].Remove(audioSource);
                    Push(pool, pair.Key, audioSource);

                    if (playList[pair.Key].Count == 0)
                    {
                        playList.Remove(pair.Key);
                    }
                }
            }

            shouldRemove.Clear();
        }

        #endregion

        #region Initialize

        public void Initialize()
        {
            if (state != State.None)
            {
                throw new FsmException("Already initialized.");
            }

            state = State.InInitializing;

            InitializeInternal("Audio/Music/Prefabs", _musicPrefabs);
            InitializeInternal("Audio/Sfx/Prefabs", _sfxPrefabs);

            state = State.Idle;
        }

        private void InitializeInternal(string folderPath, IDictionary<string, AudioSource> collection)
        {
            var assets = Resources.LoadAll<GameObject>(folderPath);
            foreach (var asset in assets)
            {
                var audioSource = asset.GetComponent<AudioSource>();
                if (ReferenceEquals(audioSource, null))
                {
                    throw new NotFoundComponentException<AudioSource>();
                }

                collection.Add(asset.name, audioSource);
            }
        }
        
        #endregion

        #region Play

        public void PlayMusic(string audioName, float fadeIn = 3f)
        {
            if (state != State.Idle)
            {
                throw new FsmException("Not initialized.");
            }

            if (string.IsNullOrEmpty(audioName))
            {
                throw new ArgumentNullException();
            }

            StopMusicAll(fadeIn);

            var audioSource = PopFromMusicPool(audioName);
            Push(_musicPlaylist, audioName, audioSource);
            StartCoroutine(CoFadeIn(audioSource, fadeIn));
        }

        public void PlaySfx(string audioName)
        {
            if (state != State.Idle)
            {
                throw new FsmException("Not initialized.");
            }

            if (string.IsNullOrEmpty(audioName))
            {
                throw new ArgumentNullException();
            }

            var audioSource = PopFromSfxPool(audioName);
            Push(_sfxPlaylist, audioName, audioSource);
            audioSource.Play();
        }

        public void StopAll(float musicFadeOut = 1f)
        {
            StopMusicAll(musicFadeOut);
            StopSfxAll();
        }

        public void StopMusicAll(float fadeOut = 1f)
        {
            if (state != State.Idle)
            {
                throw new FsmException("Not initialized.");
            }

            foreach (var pair in _musicPlaylist)
            {
                foreach (var audioSource in pair.Value)
                {
                    StartCoroutine(CoFadeOut(audioSource, fadeOut));
                }
            }
        }

        public void StopSfxAll()
        {
            foreach (var stack in _sfxPlaylist)
            {
                foreach (var audioSource in stack.Value)
                {
                    audioSource.Stop();
                }
            }
        }

        #endregion

        #region Pool

        private AudioSource Instantiate(string audioName, IDictionary<string, AudioSource> collection)
        {
            if (!collection.ContainsKey(audioName))
            {
                throw new KeyNotFoundException($"Not found AudioSource `{audioName}`.");
            }

            return Instantiate(collection[audioName], transform);
        }

        private AudioSource PopFromMusicPool(string audioName)
        {
            if (!_musicPool.ContainsKey(audioName))
            {
                return Instantiate(audioName, _musicPrefabs);
            }

            var stack = _musicPool[audioName];

            return stack.Count > 0 ? stack.Pop() : Instantiate(audioName, _musicPrefabs);
        }

        private AudioSource PopFromSfxPool(string audioName)
        {
            if (!_sfxPool.ContainsKey(audioName))
            {
                return Instantiate(audioName, _sfxPrefabs);
            }

            var stack = _sfxPool[audioName];

            return stack.Count > 0 ? stack.Pop() : Instantiate(audioName, _sfxPrefabs);
        }

        private static void Push(IDictionary<string, List<AudioSource>> collection, string audioName, AudioSource audioSource)
        {
            if (collection.ContainsKey(audioName))
            {
                collection[audioName].Add(audioSource);
            }
            else
            {
                var list = new List<AudioSource> {audioSource};
                collection.Add(audioName, list);
            }
        }

        private static void Push(IDictionary<string, Stack<AudioSource>> collection, string audioName, AudioSource audioSource)
        {
            if (collection.ContainsKey(audioName))
            {
                collection[audioName].Push(audioSource);
            }
            else
            {
                var stack = new Stack<AudioSource>();
                stack.Push(audioSource);
                collection.Add(audioName, stack);
            }
        }

        #endregion

        #region Fade

        private static IEnumerator CoFadeIn(AudioSource audioSource, float duration)
        {
            var volume = audioSource.volume;
            audioSource.volume = 0f;
            audioSource.Play();

            var deltaTime = 0f;
            while (deltaTime < duration)
            {
                deltaTime += Time.deltaTime;
                audioSource.volume += volume * Time.deltaTime / duration;

                yield return null;
            }

            audioSource.volume = volume;
        }

        private static IEnumerator CoFadeOut(AudioSource audioSource, float duration)
        {
            var volume = audioSource.volume;

            var deltaTime = 0f;
            while (deltaTime < duration)
            {
                deltaTime += Time.deltaTime;
                audioSource.volume -= volume * Time.deltaTime / duration;

                yield return null;
            }

            audioSource.Stop();
            audioSource.volume = volume;
        }

        #endregion

        #region Shortcut

        public static void PlayClick()
        {
            instance.PlaySfx(SfxCode.Click);
        }

        public static void PlaySelect()
        {
            instance.PlaySfx(SfxCode.Select);
        }

        public static void PlayCancel()
        {
            instance.PlaySfx(SfxCode.Cancel);
        }

        public static void PlayPopup()
        {
            instance.PlaySfx(SfxCode.Popup);
        }

        public static void PlaySwing()
        {
            var random = Random.value;
            if (random < 0.33f)
            {
                instance.PlaySfx(SfxCode.Swing);
            }
            else if (random < 0.66f)
            {
                instance.PlaySfx(SfxCode.Swing2);
            }
            else
            {
                instance.PlaySfx(SfxCode.Swing3);
            }
        }

        public static void PlayFootStep()
        {
            var random = Random.value;
            instance.PlaySfx(random < 0.5f ? SfxCode.FootStepLow : SfxCode.FootStepHigh);
        }
        
        public static void PlayDamaged()
        {
            var random = Random.value;
            instance.PlaySfx(SfxCode.DamageNormal);
        }
        
        public static void PlayDamagedCritical()
        {
            var random = Random.value;
            instance.PlaySfx(random < 0.5f ? SfxCode.Critical01 : SfxCode.Critical02);
        }

        #endregion
    }
}
