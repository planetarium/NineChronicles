using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    public class Settings
    {
        public static Settings Instance => _instance ??= new Settings();
        private static Settings _instance;

        private const string VolumeMasterKey = "SETTINGS_VOLUME_MASTER";
        private const string VolumeMasterIsMutedKey = "SETTINGS_VOLUME_MASTER_ISMUTED";
        private const string VolumeMusicKey = "SETTINGS_VOLUME_MUSIC";
        private const string VolumeMusicIsMutedKey = "SETTINGS_VOLUME_MUSIC_ISMUTED";
        private const string VolumeSfxKey = "SETTINGS_VOLUME_SFX";
        private const string VolumeSfxIsMutedKey = "SETTINGS_VOLUME_SFX_ISMUTED";
        private const string ResolutionIndexKey = "SETTINGS_RESOLUTION_INDEX";
        private const string ResolutionWindowedKey = "SETTINGS_WINDOWED";

        private const string PushEnabledKey = "SETTINGS_PUSH_ENABLED";
        private const string PushNightTimeEnabledKey = "SETTINGS_PUSH_NIGHTTIME_ENABLED";
        private const string PushDailyRewardEnabledKey = "SETTINGS_PUSH_DAILYREWARD_ENABLED";
        private const string PushWorkshopEnabledKey = "SETTINGS_PUSH_WORKSHOP_ENABLED";
        private const string PushArenaEnabledKey = "SETTINGS_PUSH_ARENA_ENABLED";
        private const string PushWorldbossEnabledKey = "SETTINGS_PUSH_WORLDBOSS_ENABLED";
        private const string PushPatrolRewardEnabledKey = "SETTINGS_PUSH_PATROLREWARD_ENABLED";
        private const string PushAdventureBossEnabledKey = "SETTINGS_PUSH_ADVENTUREBOSS_ENABLED";

        public float volumeMaster;
        public float volumeMusic;
        public float volumeSfx;
        public int resolutionIndex = 0;

        public bool isVolumeMasterMuted;
        public bool isVolumeMusicMuted;
        public bool isVolumeSfxMuted;
        public bool isWindowed = true;

        public bool isPushEnabled = true;
        public bool isNightTimePushEnabled = true;
        public bool isRewardPushEnabled = true;
        public bool isWorkshopPushEnabled = true;
        public bool isArenaPushEnabled = true;
        public bool isWorldbossPushEnabled = true;
        public bool isPatrolRewardPushEnabled = true;
        public bool isAdventureBossPushEnabled = true;

        public float MasterVolume => isVolumeMasterMuted ? 0 : volumeMaster;

        public class Resolution
        {
            public int Width { get; }
            public int Height { get; }

            public Resolution(int width, int height)
            {
                Width = width;
                Height = height;
            }
        }

        public readonly List<Resolution> Resolutions = new()
        {
            { new(1176, 664) },
            { new(1280, 720) },
            { new(1366, 768) },
            { new(1600, 900) },
            { new(1920, 1080) }
        };

        /// <summary>
        /// 무조건 메인 스레드에서 동작해야 함.
        /// </summary>
        public Settings()
        {
            ReloadSettings();
        }

        public void ReloadSettings()
        {
            volumeMaster = PlayerPrefs.GetFloat(VolumeMasterKey, 1f);
            volumeMusic = PlayerPrefs.GetFloat(VolumeMusicKey, 1f);
            volumeSfx = PlayerPrefs.GetFloat(VolumeSfxKey, 1f);

            isVolumeMasterMuted = PlayerPrefs.GetInt(VolumeMasterIsMutedKey, 0) != 0;
            isVolumeMusicMuted = PlayerPrefs.GetInt(VolumeMusicIsMutedKey, 0) != 0;
            isVolumeSfxMuted = PlayerPrefs.GetInt(VolumeSfxIsMutedKey, 0) != 0;

            resolutionIndex = PlayerPrefs.GetInt(ResolutionIndexKey, 0);
            isWindowed = PlayerPrefs.GetInt(ResolutionWindowedKey, 1) == 1 ? true : false;

            isPushEnabled = PlayerPrefs.GetInt(PushEnabledKey, 1) != 0;
            isNightTimePushEnabled = PlayerPrefs.GetInt(PushNightTimeEnabledKey, 1) != 0;
            isRewardPushEnabled = PlayerPrefs.GetInt(PushDailyRewardEnabledKey, 1) != 0;
            isWorkshopPushEnabled = PlayerPrefs.GetInt(PushWorkshopEnabledKey, 1) != 0;
            isArenaPushEnabled = PlayerPrefs.GetInt(PushArenaEnabledKey, 1) != 0;
            isWorldbossPushEnabled = PlayerPrefs.GetInt(PushWorldbossEnabledKey, 1) != 0;
            isPatrolRewardPushEnabled = PlayerPrefs.GetInt(PushPatrolRewardEnabledKey, 1) != 0;
            isAdventureBossPushEnabled = PlayerPrefs.GetInt(PushAdventureBossEnabledKey, 1) != 0;

            SetResolution();
        }

        public void ApplyCurrentSettings()
        {
            PlayerPrefs.SetFloat(VolumeMasterKey, volumeMaster);
            PlayerPrefs.SetFloat(VolumeMusicKey, volumeMusic);
            PlayerPrefs.SetFloat(VolumeSfxKey, volumeSfx);

            PlayerPrefs.SetInt(VolumeMasterIsMutedKey, isVolumeMasterMuted ? 1 : 0);
            PlayerPrefs.SetInt(VolumeMusicIsMutedKey, isVolumeMusicMuted ? 1 : 0);
            PlayerPrefs.SetInt(VolumeSfxIsMutedKey, isVolumeSfxMuted ? 1 : 0);

            PlayerPrefs.SetInt(PushEnabledKey, isPushEnabled ? 1 : 0);
            PlayerPrefs.SetInt(PushNightTimeEnabledKey, isNightTimePushEnabled ? 1 : 0);
            PlayerPrefs.SetInt(PushDailyRewardEnabledKey, isRewardPushEnabled ? 1 : 0);
            PlayerPrefs.SetInt(PushWorkshopEnabledKey, isWorkshopPushEnabled ? 1 : 0);
            PlayerPrefs.SetInt(PushArenaEnabledKey, isArenaPushEnabled ? 1 : 0);
            PlayerPrefs.SetInt(PushWorldbossEnabledKey, isWorldbossPushEnabled ? 1 : 0);
            PlayerPrefs.SetInt(PushPatrolRewardEnabledKey, isPatrolRewardPushEnabled ? 1 : 0);
            PlayerPrefs.SetInt(PushAdventureBossEnabledKey, isAdventureBossPushEnabled ? 1 : 0);
        }

        public void ApplyCurrentResolution()
        {
            PlayerPrefs.SetInt(ResolutionIndexKey, resolutionIndex);
            PlayerPrefs.SetInt(ResolutionWindowedKey, isWindowed ? 1 : 0);
            SetResolution();
        }

        private void SetResolution()
        {
#if !(UNITY_ANDROID || UNITY_IOS)
            Screen.SetResolution(Resolutions[resolutionIndex].Width, Resolutions[resolutionIndex].Height, !isWindowed);
#endif
        }
    }
}
