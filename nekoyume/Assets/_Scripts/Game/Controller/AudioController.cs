using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UniRx;
using UniRx.Triggers;

namespace Nekoyume.Game.Controller
{
    public class AudioController : MonoSingleton<AudioController>
    {
        public struct MusicCode
        {
            public const string SelectCharacter = "bgm_selectcharacter";
            public const string Main = "bgm_main";
            public const string Shop = "bgm_shop";
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
            public const string ChainMail1 = "chainmail1";
            public const string Equipment = "sfx_equipmount";
            public const string FootStepLow = "sfx_footstep-low";
            public const string FootStepHigh = "sfx_footstep-high";
            public const string DamageNormal = "sfx_damage_normal";
            public const string Critical01 = "sfx_critical01";
            public const string Critical02 = "sfx_critical02";
            public const string LevelUp = "sfx_levelup";
            public const string Cancel = "sfx_cancel";
            public const string Popup = "sfx_pupop";
            public const string Click = "sfx_click";
        }

        public enum State
        {
            None = -1,
            InInitializing,
            Idle
        }

        public State state { get; private set; }

        private readonly Dictionary<string, AudioSource> _musicPrefabs = new Dictionary<string, AudioSource>();
        private readonly Dictionary<string, AudioSource> _sfxPrefabs = new Dictionary<string, AudioSource>();

        private readonly Dictionary<string, Stack<AudioSource>> _musicPool =
            new Dictionary<string, Stack<AudioSource>>();
        private readonly Dictionary<string, Stack<AudioSource>> _sfxPool =
            new Dictionary<string, Stack<AudioSource>>();

        private readonly Dictionary<string, AudioSource> _musicPlaylist = new Dictionary<string, AudioSource>();

        private readonly Dictionary<string, List<AudioSource>> _sfxPlaylist =
            new Dictionary<string, List<AudioSource>>();

        private readonly Dictionary<string, List<AudioSource>> _shouldRemoveSfx = new Dictionary<string, List<AudioSource>>();
        
        #region Mono

        protected override void Awake()
        {
            base.Awake();

            this.ComponentFieldsNotNullTest();
        }

        private void Update()
        {   
            foreach (var pair in _sfxPlaylist)
            {
                foreach (var sfx in pair.Value)
                {
                    if (sfx.isPlaying)
                    {
                        continue;
                    }

                    if (!_shouldRemoveSfx.ContainsKey(pair.Key))
                    {
                        _shouldRemoveSfx.Add(pair.Key, new List<AudioSource>());
                    }
                    
                    _shouldRemoveSfx[pair.Key].Add(sfx);
                }
            }

            foreach (var pair in _shouldRemoveSfx)
            {
                foreach (var sfx in pair.Value)
                {
                    _sfxPlaylist[pair.Key].Remove(sfx);
                    Push(_sfxPool, pair.Key, sfx);

                    if (_sfxPlaylist[pair.Key].Count == 0)
                    {
                        _sfxPlaylist.Remove(pair.Key);
                    }
                }
            }
            
            _shouldRemoveSfx.Clear();
        }

        #endregion

        public IEnumerator CoInitialize()
        {
            if (state != State.None)
            {
                throw new FiniteStateMachineException("Already initialized.");
            }

            state = State.InInitializing;

            yield return StartCoroutine(CoInitializeInternal("Assets/Resources/Audio/Music/Prefabs", _musicPrefabs));
            yield return StartCoroutine(CoInitializeInternal("Assets/Resources/Audio/Sfx/Prefabs", _sfxPrefabs));

            state = State.Idle;
        }

        private IEnumerator CoInitializeInternal(string folderPath, IDictionary<string, AudioSource> collection)
        {
            var assets = AssetDatabase.FindAssets("t: GameObject", new[] {folderPath});

            foreach (var asset in assets)
            {
                var path = AssetDatabase.GUIDToAssetPath(asset).Replace("Assets/Resources/", "");
                path = path.Substring(0, path.Length - 7);
                var loadOperator = Resources.LoadAsync<GameObject>(path);
                while (!loadOperator.isDone)
                {
                    yield return null;
                }

                try
                {
                    var assetName = Path.GetFileNameWithoutExtension(path);
                    var go = loadOperator.asset as GameObject;
                    if (ReferenceEquals(go, null))
                    {
                        throw new InvalidCastException();
                    }
                    var audioSource = go.GetComponent<AudioSource>();
                    if (ReferenceEquals(audioSource, null))
                    {
                        throw new NotFoundComponentException<AudioSource>();
                    }

                    collection.Add(assetName, audioSource);
                }
                catch
                {
                    throw new FailedToLoadResourceException<AudioSource>(path);
                }
            }
        }

        #region Play

        public void PlayMusic(string audioName, float fadeIn = 3f)
        {
            if (state != State.Idle)
            {
                throw new FiniteStateMachineException("Not initialized.");
            }

            if (string.IsNullOrEmpty(audioName))
            {
                throw new ArgumentNullException();
            }

            StopMusic(fadeIn);

            var audioSource = PopFromMusicPool(audioName);
            Push(_musicPlaylist, audioName, audioSource);
            StartCoroutine(CoFadeIn(audioSource, fadeIn));
        }
        
        public void PlaySfx(string audioName)
        {
            if (state != State.Idle)
            {
                throw new FiniteStateMachineException("Not initialized.");
            }

            if (string.IsNullOrEmpty(audioName))
            {
                throw new ArgumentNullException();
            }

            var audioSource = PopFromSfxPool(audioName);
            Push(_sfxPlaylist, audioName, audioSource);
            audioSource.Play();
        }

        public void StopMusic(float fadeOut = 1f)
        {
            if (state != State.Idle)
            {
                throw new FiniteStateMachineException("Not initialized.");
            }

            if (_musicPlaylist.Count <= 0)
            {
                return;
            }

            StartCoroutine(CoStopMusic(fadeOut));
        }

        public void StopSfx()
        {
            foreach (var stack in _sfxPlaylist)
            {
                foreach (var audioSource in stack.Value)
                {
                    audioSource.Stop();
                    Push(_sfxPool, stack.Key, audioSource);        
                }
                
                stack.Value.Clear();
            }
            
            _sfxPlaylist.Clear();
        }

        private IEnumerator CoStopMusic(float fadeOut)
        {
            var music = _musicPlaylist.First();
            _musicPlaylist.Clear();
            yield return StartCoroutine(CoFadeOut(music.Value, fadeOut));
            Push(_musicPool, music.Key, music.Value);
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

        private void Push(IDictionary<string, AudioSource> collection, string audioName, AudioSource audioSource)
        {
            if (collection.ContainsKey(audioName))
            {
                collection[audioName] = audioSource;
            }
            else
            {
                collection.Add(audioName, audioSource);
            }
        }
        
        private void Push(IDictionary<string, List<AudioSource>> collection, string audioName, AudioSource audioSource)
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

        private void Push(IDictionary<string, Stack<AudioSource>> collection, string audioName, AudioSource audioSource)
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

        private IEnumerator CoFadeIn(AudioSource audioSource, float duration)
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

        private IEnumerator CoFadeOut(AudioSource audioSource, float duration)
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

        #endregion
    }
}
