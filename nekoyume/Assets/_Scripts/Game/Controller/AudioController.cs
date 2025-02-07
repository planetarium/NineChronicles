using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Nekoyume.Model.Elemental;
using Nekoyume.Pattern;
using Nekoyume.State;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Nekoyume.Game.Controller
{
    using UniRx;

    public class AudioController : MonoSingleton<AudioController>
    {
        private readonly struct AudioInfo
        {
            public readonly AudioSource source;
            public readonly float volume;

            public AudioInfo(AudioSource source)
            {
                this.source = source;
                volume = source ? source.volume : 0f;
            }
        }

        public class MusicCode
        {
            public const string Title = "bgm_title";
            public const string Prologue = "bgm_prologue";
            public const string PrologueBattle = "bgm_prologue_battle";
            public const string SelectCharacter = "bgm_selectcharacter";
            public const string Main = "bgm_main";
            public const string Shop = "bgm_shop";
            public const string Ranking = "bgm_ranking";
            public const string Workshop = "bgm_workshop";
            public const string Boss1 = "bgm_boss1";
            public const string Win = "bgm_win";
            public const string Lose = "bgm_lose";
            public const string PVPBattle = "bgm_pvp_battle";
            public const string PVPWin = "bgm_pvp_win";
            public const string PVPLose = "bgm_pvp_lose";
            public const string Dcc = "bgm_dcc";
            public const string BattleLoading = "bgm_battle_loading";
            public const string AdventureBoss01 = "bgm_adventure_boss_01";
            public const string AdventureBossLobby = "bgm_adventure_boss_lobby";
            public const string CustomCraft = "bgm_custom_craft";

#region WorldBoss

            [UsedImplicitly] // Used in WorldBoss SO
            public const string WorldBossBattle01 = "bgm_worldboss_battle_01";

            [UsedImplicitly] // Used in WorldBoss SO
            public const string WorldBossBattle02 = "bgm_worldboss_battle_02";

            public const string WorldBossBattleResult = "bgm_worldboss_battle_result";

            [UsedImplicitly] // Used in WorldBoss SO
            public const string WorldBossTitle = "bgm_worldboss_title";

#endregion WorldBoss

#region Stage

            [UsedImplicitly] // Used in Stage Sheet
            public const string Alfheim01 = "bgm_alfheim_01";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Alfheim02 = "bgm_alfheim_02";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Alfheim03 = "bgm_alfheim_03";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Asgard01 = "bgm_asgard_01";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Asgard02 = "bgm_asgard_02";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Asgard03 = "bgm_asgard_03";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Hard01 = "bgm_hard1";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Hel01 = "bgm_hel_01";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Hel02 = "bgm_hel_02";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Hel03 = "bgm_hel_03";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Jotunheim01 = "bgm_jotunheim_01";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Jotunheim02 = "bgm_jotunheim_02";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Jotunheim03 = "bgm_jotunheim_03";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Muspelheim01 = "bgm_muspelheim_01";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Muspelheim02 = "bgm_muspelheim_02";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Muspelheim03 = "bgm_muspelheim_03";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Niflheim01 = "bgm_niflheim_01";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Niflheim02 = "bgm_niflheim_02";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Niflheim03 = "bgm_niflheim_03";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Svartalfheim01 = "bgm_svartalfheim_01";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Svartalfheim02 = "bgm_svartalfheim_02";

            [UsedImplicitly] // Used in Stage Sheet
            public const string Svartalfheim03 = "bgm_svartalfheim_03";

#endregion Stage

#region Event

            [UsedImplicitly] // Used in Event Data SO
            public const string Christmas = "bgm_christmas";

            [UsedImplicitly] // Used in Event Data SO
            public const string Event22Summer01 = "bgm_event_22summer_01";

            [UsedImplicitly] // Used in Event Data SO
            public const string Event22Summer02 = "bgm_event_22summer_02";

            [UsedImplicitly] // Used in Event Data SO
            public const string Event22Summer03 = "bgm_event_22summer_03";

            [UsedImplicitly] // Used in Event Data SO
            public const string Event22SummerTitle = "bgm_event_22summer_title";

#endregion Event

            // [Obsolete("Use `bgm_workshop` instead. bgm_combination has not prefab.")]
            // public const string Combination = "bgm_combination";
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
            public const string UnlockRecipe = "sfx_unlock_recipe";
            public const string CombinationSmash = "sfx_combination_smash";
            public const string FenrirGrowlCasting = "sfx_fenrir_growl_casting";
            public const string FenrirGrowlSkill = "sfx_fenrir_growl_skill";
            public const string FenrirGrowlCastingAttack = "sfx_fenrir_growl_casting_attack";
            public const string FenrirGrowlSummon = "sfx_fenrir_growl_summon";
            public const string Heal = "sfx_heal";
            public const string NPC_Common = "sfx_npc_common";
            public const string NPC_Congrat = "sfx_npc_congrat";
            public const string NPC_Question = "sfx_npc_question";
            public const string GuideArrow = "sfx_guide_arrow";
            public const string BgmFailed = "sfx_bgm_failed";
            public const string BgmGreatSuccess = "sfx_bgm_great_success";
            public const string BgmSuccess = "sfx_bgm_success";
            public const string FailedEffect = "sfx_failed_effect";
            public const string GreatSuccessDrum = "sfx_great_success_drum";
            public const string OptionNormal = "sfx_option_normal";
            public const string OptionSpecial = "sfx_option_special";
            public const string SuccessEffectFadeIn = "sfx_successeffect_fadein";
            public const string SuccessEffectSlot = "sfx_successeffect_slot";
            public const string UpgradeNumber = "sfx_upgrade_number";
            public const string Rewards = "sfx_rewards";
            public const string Star = "sfx_star";
            public const string ArenaBattleLoading = "sfx_arenabattleloading";
            public const string AdventureBossCoin = "sfx_adventure_boss_coin";
            public const string AdventureBossMonCollision = "sfx_adventure_boss_mon_collision";
            public const string AdventureBossPenetration = "sfx_adventure_boss_penetration";
            public const string AdventureBossPopUp = "sfx_adventure_boss_pop_up";
            public const string CustomCraftJudge1 = "sfx_custom_craft_judge_01";
            public const string CustomCraftJudge2 = "sfx_custom_craft_judge_02";
            public const string CustomCraftJudge3 = "sfx_custom_craft_judge_03";
        }

        private enum State
        {
            None = -1,
            InInitializing,
            Idle
        }

        private State CurrentState { get; set; }

        protected override bool ShouldRename => true;

        private readonly Dictionary<string, AudioSource> _musicPrefabs = new();
        private readonly Dictionary<string, AudioSource> _sfxPrefabs = new();

        private readonly Dictionary<string, Stack<AudioInfo>> _musicPool = new();
        private readonly Dictionary<string, Stack<AudioInfo>> _sfxPool = new();

        private readonly Dictionary<string, List<AudioInfo>> _musicPlaylist = new();
        private readonly Dictionary<string, List<AudioInfo>> _sfxPlaylist = new();
        private readonly Dictionary<string, List<AudioInfo>> _shouldRemoveMusic = new();
        private readonly Dictionary<string, List<AudioInfo>> _shouldRemoveSfx = new();

        private Coroutine _fadeInMusic;
        private readonly List<Coroutine> _fadeOutMusics = new();

        public string CurrentPlayingMusicName { get; private set; }

#region Mono

        protected override void Awake()
        {
            base.Awake();

            CurrentState = State.None;
            Lobby.OnLobbyEnterEvent += OnLobbyEnter;
        }

        private void Update()
        {
            CheckPlaying(_musicPool, _musicPlaylist, _shouldRemoveMusic);
            CheckPlaying(_sfxPool, _sfxPlaylist, _shouldRemoveSfx);
        }

        private static void CheckPlaying<T1, T2>(T1 pool, T2 playList, T2 shouldRemove)
            where T1 : IDictionary<string, Stack<AudioInfo>>
            where T2 : IDictionary<string, List<AudioInfo>>
        {
            foreach (var pair in playList)
            {
                foreach (var audioInfo in pair.Value)
                {
                    if (audioInfo.source == null || audioInfo.source.isPlaying)
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

        public async UniTask InitializeAsync()
        {
            AudioListener.volume = Settings.Instance.MasterVolume;

            if (CurrentState != State.None)
            {
                NcDebug.LogError("Already initialized.");
                return;
            }

            CurrentState = State.InInitializing;
            await UniTask.WhenAll(
                InitializeInternal(ResourceManager.MusicAudioLabel, typeof(MusicCode), _musicPrefabs, _musicPool),
                InitializeInternal(ResourceManager.SfxAudioLabel, typeof(SfxCode), _sfxPrefabs, _sfxPool)
            );
            CurrentState = State.Idle;
        }

        private async UniTask InitializeInternal(
            string label,
            Type codeType,
            IDictionary<string, AudioSource> prefabs,
            IDictionary<string, Stack<AudioInfo>> pool)
        {
            var assets = new List<GameObject>();
            await ResourceManager.Instance.LoadAllAsync<GameObject>(label, true, assetAddress =>
            {
                var prefab = ResourceManager.Instance.Load<GameObject>(assetAddress);
                if (prefab == null)
                {
                    NcDebug.LogError($"Failed to load {assetAddress}");
                    return;
                }
                assets.Add(prefab);
            });

            foreach (var asset in assets)
            {
                var audioSource = asset.GetComponent<AudioSource>();
                if (!audioSource)
                {
                    NcDebug.LogError($"There is no AudioSource component: {asset.name}");
                    continue;
                }

                prefabs.Add(asset.name, audioSource);
                Push(pool, asset.name, new AudioInfo(Instantiate(asset.name, prefabs)));
            }

            Validate(codeType, prefabs);
        }

        private static void Validate(
            Type codeType,
            IDictionary<string, AudioSource> prefabs)
        {
            var fields = codeType.GetFields(BindingFlags.Public | BindingFlags.Static);
            foreach (var fieldInfo in fields)
            {
                var code = (string)fieldInfo.GetRawConstantValue();
                if (prefabs.ContainsKey(code))
                {
                    continue;
                }

                NcDebug.LogError($"There is no audio prefab: {code}");
            }
        }

#endregion

#region Play

        public void PlayMusic(string audioName, float fadeIn = 0.8f)
        {
            if (CurrentState != State.Idle)
            {
                NcDebug.LogError("Not initialized.");
                return;
            }

            if (string.IsNullOrEmpty(audioName))
            {
                NcDebug.LogError($"{nameof(audioName)} is null or empty");
                return;
            }

            StopMusicAll(0.5f);

            var audioInfo = PopFromMusicPool(audioName);
            Push(_musicPlaylist, audioName, audioInfo);
            _fadeInMusic = StartCoroutine(CoFadeIn(audioInfo, fadeIn));
            CurrentPlayingMusicName = audioName;
        }

        public void PlaySfx(string audioName, float volume = 1f, float delay = 0f)
        {
            if (delay > 0f)
            {
                Observable.Timer(TimeSpan.FromSeconds(delay))
                    .First()
                    .Subscribe(_ => PlaySfx(audioName, volume));
                return;
            }

            if (CurrentState != State.Idle)
            {
                NcDebug.LogWarning("Not initialized.");
                return;
            }

            if (string.IsNullOrEmpty(audioName))
            {
                NcDebug.LogError($"{nameof(audioName)} is null or empty");
                return;
            }

            var audioInfo = PopFromSfxPool(audioName);
            if (audioInfo.source == null)
            {
                NcDebug.LogError($"Failed to load AudioSource `{audioName}`.");
                return;
            }
            Push(_sfxPlaylist, audioName, audioInfo);
            audioInfo.source.volume = audioInfo.volume * volume * Settings.Instance.volumeSfx;
            audioInfo.source.Play();
        }

#endregion

#region Stop

        public void StopAll(float musicFadeOut = 1f)
        {
            StopMusicAll(musicFadeOut);
            StopSfxAll();
        }

        public void StopMusicAll(float fadeOut = 1f)
        {
            if (CurrentState != State.Idle)
            {
                NcDebug.LogError("Not initialized.");
                return;
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
            foreach (var audioInfo in _sfxPlaylist.SelectMany(pair => pair.Value))
            {
                audioInfo.source.Stop();
            }
        }

        public void StopSfx(string audioName)
        {
            if (CurrentState != State.Idle)
            {
                NcDebug.LogError("Not initialized.");
                return;
            }

            if (string.IsNullOrEmpty(audioName))
            {
                NcDebug.LogError($"{nameof(audioName)} is null or empty");
                return;
            }

            foreach (var audioInfo in _sfxPlaylist
                .Where(pair => pair.Key.Equals(audioName))
                .SelectMany(pair => pair.Value))
            {
                audioInfo.source.Stop();
            }
        }

#endregion

#region Pool

        private AudioSource Instantiate(string audioName, IDictionary<string, AudioSource> prefabs)
        {
            if (!prefabs.ContainsKey(audioName))
            {
                NcDebug.LogError($"Not found AudioSource `{audioName}`.");
                return null;
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

            return stack.Count > 0
                ? stack.Pop()
                : new AudioInfo(Instantiate(audioName, _musicPrefabs));
        }

        private AudioInfo PopFromSfxPool(string audioName)
        {
            if (!_sfxPool.ContainsKey(audioName))
            {
                return new AudioInfo(Instantiate(audioName, _sfxPrefabs));
            }

            var stack = _sfxPool[audioName];

            return stack.Count > 0
                ? stack.Pop()
                : new AudioInfo(Instantiate(audioName, _sfxPrefabs));
        }

        private static void Push(IDictionary<string, List<AudioInfo>> pool, string audioName,
            AudioInfo audioInfo)
        {
            if (pool.ContainsKey(audioName))
            {
                pool[audioName].Add(audioInfo);
            }
            else
            {
                var list = new List<AudioInfo> { audioInfo };
                pool.Add(audioName, list);
            }
        }

        private static void Push(IDictionary<string, Stack<AudioInfo>> pool, string audioName,
            AudioInfo audioInfo)
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
                audioInfo.source.volume += audioInfo.volume
                    * Settings.Instance.volumeMusic
                    * Time.deltaTime
                    / duration;

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
                    throw new ArgumentOutOfRangeException(
                        nameof(elementalType),
                        elementalType,
                        null);
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
                    NcDebug.LogError("Elemental type is invaild.");
                    return SfxCode.CastingNormal;
            }
        }

#endregion

        private void OnLobbyEnter()
        {
            PlayMusic(EventManager.GetEventInfo().MainBGM.name);
        }
    }
}
