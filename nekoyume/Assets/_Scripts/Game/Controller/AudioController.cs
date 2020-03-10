using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Nekoyume.EnumType;
using Nekoyume.Model.Elemental;
using Nekoyume.Pattern;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nekoyume.Game.Controller
{
    public class AudioController : MonoSingleton<AudioController>
    {
        public struct AudioInfo
        {
            public readonly AudioSource source;
            public readonly float volume;

            public AudioInfo(AudioSource source)
            {
                this.source = source;
                volume = source.volume;
            }
        }
        
        public class MusicCode
        {
            public const string Title = "bgm_title";
            public const string Prologue = "bgm_prologue";
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

        public class SfxCode
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
            public const string DamageFire = "sfx_damage_fire";
            public const string DamageWater = "sfx_damage_water";
            public const string DamageLand = "sfx_damage_land";
            public const string DamageWind = "sfx_damage_wind";
            public const string Critical01 = "sfx_critical01";
            public const string Critical02 = "sfx_critical02";
            public const string Miss = "sfx_miss";
            public const string LevelUp = "sfx_levelup";
            public const string Cancel = "sfx_cancel";
            public const string Popup = "sfx_popup";
            public const string Click = "sfx_click";
            public const string Swing = "sfx_swing";
            public const string Swing2 = "sfx_swing2";
            public const string Swing3 = "sfx_swing3";
            public const string BattleCast = "sfx_battle_cast";
            public const string CastingNormal = "sfx_casting_normal";
            public const string CastingFire = "sfx_casting_fire";
            public const string CastingWater = "sfx_casting_water";
            public const string CastingLand = "sfx_casting_land";
            public const string CastingWind = "sfx_casting_wind";
            public const string RewardItem = "sfx_reward_item";
            public const string BuyItem = "sfx_buy_item";
            public const string Notice = "sfx_notice";
            public const string Typing = "sfx_typing";
            public const string Win = "sfx_win";
        }

        private enum State
        {
            None = -1,
            InInitializing,
            Idle
        }
        
        private const string MusicContainerPath = "Audio/Music/Prefabs";
        private const string SfxContainerPath = "Audio/Sfx/Prefabs";

        private State CurrentState { get; set; }

        protected override bool ShouldRename => true;

        private readonly Dictionary<string, AudioSource> _musicPrefabs = new Dictionary<string, AudioSource>();
        private readonly Dictionary<string, AudioSource> _sfxPrefabs = new Dictionary<string, AudioSource>();

        private readonly Dictionary<string, Stack<AudioInfo>> _musicPool =
            new Dictionary<string, Stack<AudioInfo>>();

        private readonly Dictionary<string, Stack<AudioInfo>> _sfxPool =
            new Dictionary<string, Stack<AudioInfo>>();

        private readonly Dictionary<string, List<AudioInfo>> _musicPlaylist =
            new Dictionary<string, List<AudioInfo>>();

        private readonly Dictionary<string, List<AudioInfo>> _sfxPlaylist =
            new Dictionary<string, List<AudioInfo>>();

        private readonly Dictionary<string, List<AudioInfo>> _shouldRemoveMusic =
            new Dictionary<string, List<AudioInfo>>();

        private readonly Dictionary<string, List<AudioInfo>> _shouldRemoveSfx =
            new Dictionary<string, List<AudioInfo>>();

        private Coroutine _fadeInMusic;
        private readonly List<Coroutine> _fadeOutMusics = new List<Coroutine>();

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            CurrentState = State.None;
            Event.OnRoomEnter.AddListener(b => PlayMusic(AudioController.MusicCode.Main));

            // FixMe. 돈 버는 소리는 언제쯤 켜둘 수 있을까요. 마이너모드에서 소리가 방해된다는 피드백으로 다시 꺼둡니다.
//#if !UNITY_EDITOR
//            ReactiveAgentState.Gold.ObserveOnMainThread().Subscribe(_ => PlaySfx(SfxCode.Cash)).AddTo(this);
//#endif
        }

        private void Update()
        {
            CheckPlaying(_musicPool, _musicPlaylist, _shouldRemoveMusic);
            CheckPlaying(_sfxPool, _sfxPlaylist, _shouldRemoveSfx);
        }

        private void CheckPlaying<T1, T2>(T1 pool, T2 playList, T2 shouldRemove)
            where T1 : IDictionary<string, Stack<AudioInfo>> where T2 : IDictionary<string, List<AudioInfo>>
        {
            foreach (var pair in playList)
            {
                foreach (var audioInfo in pair.Value)
                {
                    if (audioInfo.source.isPlaying)
                    {
                        continue;
                    }

                    if (!shouldRemove.ContainsKey(pair.Key))
                    {
                        shouldRemove.Add(pair.Key, new List<AudioInfo>());
                    }

                    shouldRemove[pair.Key].Add(audioInfo);
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

        #region Initialize & Validate

        public void Initialize()
        {
            AudioListener.volume = Settings.Instance.volumeMaster;

            if (CurrentState != State.None)
            {
                throw new FsmException("Already initialized.");
            }

            CurrentState = State.InInitializing;
            InitializeInternal(MusicContainerPath, typeof(MusicCode), _musicPrefabs, _musicPool);
            InitializeInternal(SfxContainerPath, typeof(SfxCode), _sfxPrefabs, _sfxPool);
            CurrentState = State.Idle;
        }

        private void InitializeInternal(string containerPath, Type codeType, IDictionary<string, AudioSource> prefabs, IDictionary<string, Stack<AudioInfo>> pool)
        {
            var assets = Resources.LoadAll<GameObject>(containerPath);
            foreach (var asset in assets)
            {
                var audioSource = asset.GetComponent<AudioSource>();
                if (!audioSource)
                {
                    Debug.LogError($"There is no AudioSource component: {Path.Combine(containerPath, asset.name)}");
                    continue;
                }

                prefabs.Add(asset.name, audioSource);
                Push(pool, asset.name, new AudioInfo(Instantiate(asset.name, prefabs)));
            }
            
            Validate(containerPath, codeType, prefabs);
        }

        private void Validate(string containerPath, Type codeType, IDictionary<string, AudioSource> prefabs)
        {
            var fields = codeType.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var fieldInfo in fields)
            {
                var code = (string) fieldInfo.GetRawConstantValue();
                if (prefabs.ContainsKey(code))
                    continue;
                
                Debug.LogError($"There is no audio prefab: {Path.Combine(containerPath, code)}");
            }
        }
        
        #endregion

        #region Play

        public void PlayMusic(string audioName, float fadeIn = 0.8f)
        {
            if (CurrentState != State.Idle)
            {
                throw new FsmException("Not initialized.");
            }

            if (string.IsNullOrEmpty(audioName))
            {
                throw new ArgumentNullException();
            }

            StopMusicAll(0.5f);

            var audioInfo = PopFromMusicPool(audioName);
            Push(_musicPlaylist, audioName, audioInfo);
            _fadeInMusic = StartCoroutine(CoFadeIn(audioInfo, fadeIn));
        }

        public void PlaySfx(string audioName, float volume = 1.0f)
        {
            if (CurrentState != State.Idle)
            {
                throw new FsmException("Not initialized.");
            }

            if (string.IsNullOrEmpty(audioName))
            {
                throw new ArgumentNullException();
            }

            var audioInfo = PopFromSfxPool(audioName);
            Push(_sfxPlaylist, audioName, audioInfo);
            audioInfo.source.volume = audioInfo.volume * volume * Settings.Instance.volumeSfx;
            audioInfo.source.Play();
        }

        public void StopAll(float musicFadeOut = 1f)
        {
            StopMusicAll(musicFadeOut);
            StopSfxAll();
        }

        private void StopMusicAll(float fadeOut = 1f)
        {
            if (CurrentState != State.Idle)
            {
                throw new FsmException("Not initialized.");
            }

            if (_fadeInMusic != null)
            {
                StopCoroutine(_fadeInMusic);
            }
            foreach (var fadeOutMusic in _fadeOutMusics)
            {
                if (fadeOutMusic != null)
                {
                    StopCoroutine(fadeOutMusic);   
                }
            }
            _fadeOutMusics.Clear();

            foreach (var pair in _musicPlaylist)
            {
                foreach (var audioSource in pair.Value)
                {
                    _fadeOutMusics.Add(StartCoroutine(CoFadeOut(audioSource, fadeOut)));
                }
            }
        }

        private void StopSfxAll()
        {
            foreach (var stack in _sfxPlaylist)
            {
                foreach (var audioInfo in stack.Value)
                {
                    audioInfo.source.Stop();
                }
            }
        }

        #endregion

        #region Pool

        private AudioSource Instantiate(string audioName, IDictionary<string, AudioSource> prefabs)
        {
            if (!prefabs.ContainsKey(audioName))
            {
                throw new KeyNotFoundException($"Not found AudioSource `{audioName}`.");
            }

            return Instantiate(prefabs[audioName], transform);
        }

        private AudioInfo PopFromMusicPool(string audioName)
        {
            if (!_musicPool.ContainsKey(audioName))
            {
                return new AudioInfo(Instantiate(audioName, _musicPrefabs));
            }

            var stack = _musicPool[audioName];

            return stack.Count > 0 ? stack.Pop() : new AudioInfo(Instantiate(audioName, _musicPrefabs));
        }

        private AudioInfo PopFromSfxPool(string audioName)
        {
            if (!_sfxPool.ContainsKey(audioName))
            {
                return new AudioInfo(Instantiate(audioName, _sfxPrefabs));
            }

            var stack = _sfxPool[audioName];

            return stack.Count > 0 ? stack.Pop() : new AudioInfo(Instantiate(audioName, _sfxPrefabs));
        }

        private static void Push(IDictionary<string, List<AudioInfo>> pool, string audioName, AudioInfo audioInfo)
        {
            if (pool.ContainsKey(audioName))
            {
                pool[audioName].Add(audioInfo);
            }
            else
            {
                var list = new List<AudioInfo> {audioInfo};
                pool.Add(audioName, list);
            }
        }

        private static void Push(IDictionary<string, Stack<AudioInfo>> pool, string audioName, AudioInfo audioInfo)
        {
            if (pool.ContainsKey(audioName))
            {
                pool[audioName].Push(audioInfo);
            }
            else
            {
                var stack = new Stack<AudioInfo>();
                stack.Push(audioInfo);
                pool.Add(audioName, stack);
            }
        }

        #endregion

        #region Fade

        private static IEnumerator CoFadeIn(AudioInfo audioInfo, float duration)
        {
            audioInfo.source.volume = 0f;
            audioInfo.source.Play();

            var deltaTime = 0f;
            while (deltaTime < duration)
            {
                deltaTime += Time.deltaTime;
                audioInfo.source.volume += (audioInfo.volume * Settings.Instance.volumeMusic) * Time.deltaTime / duration;

                yield return null;
            }

            audioInfo.source.volume = audioInfo.volume * Settings.Instance.volumeMusic;
        }

        private static IEnumerator CoFadeOut(AudioInfo audioInfo, float duration)
        {
            var deltaTime = 0f;
            while (deltaTime < duration)
            {
                deltaTime += Time.deltaTime;
                audioInfo.source.volume -= audioInfo.volume * Time.deltaTime / duration;

                yield return null;
            }

            audioInfo.source.Stop();
            audioInfo.source.volume = audioInfo.volume;
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
        
        public static void PlayDamaged(ElementalType elementalType = ElementalType.Normal)
        {
            switch (elementalType)
            {
                case ElementalType.Normal:
                    instance.PlaySfx(SfxCode.DamageNormal);
                    break;
                case ElementalType.Fire:
                    instance.PlaySfx(SfxCode.DamageFire);
                    break;
                case ElementalType.Water:
                    instance.PlaySfx(SfxCode.DamageWater);
                    break;
                case ElementalType.Land:
                    instance.PlaySfx(SfxCode.DamageLand);
                    break;
                case ElementalType.Wind:
                    instance.PlaySfx(SfxCode.DamageWind);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(elementalType), elementalType, null);
            }
        }
        
        public static void PlayDamagedCritical()
        {
            var random = Random.value;
            instance.PlaySfx(random < 0.5f ? SfxCode.Critical01 : SfxCode.Critical02);
        }


        public static string GetElementalCastingSFX(ElementalType type)
        {
            switch (type)
            {
                case ElementalType.Normal:
                    return SfxCode.CastingNormal;
                case ElementalType.Fire:
                    return SfxCode.CastingFire;
                case ElementalType.Water:
                    return SfxCode.CastingWater;
                case ElementalType.Land:
                    return SfxCode.CastingLand;
                case ElementalType.Wind:
                    return SfxCode.CastingWind;
                default:
                    Debug.LogError("Elemental type is invaild.");
                    return SfxCode.CastingNormal;
            }
        }

        #endregion
    }
}
