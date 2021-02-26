using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume
{
    public class Settings
    {
        public static Settings Instance => (_instance is null) ? _instance = new Settings() : _instance;
        private static Settings _instance;

        private const string VolumeMasterKey = "SETTINGS_VOLUME_MASTER";
        private const string VolumeMasterIsMutedKey = "SETTINGS_VOLUME_MASTER_ISMUTED";
        private const string VolumeMusicKey = "SETTINGS_VOLUME_MUSIC";
        private const string VolumeMusicIsMutedKey = "SETTINGS_VOLUME_MUSIC_ISMUTED";
        private const string VolumeSfxKey = "SETTINGS_VOLUME_SFX";
        private const string VolumeSfxIsMutedKey = "SETTINGS_VOLUME_SFX_ISMUTED";
        private const string ResolutionIndexKey = "SETTINGS_RESOLUTION_INDEX";
        private const string ResolutionWindowedKey = "SETTINGS_WINDOWED";

        public float volumeMaster;
        public float volumeMusic;
        public float volumeSfx;
        public int resolutionIndex = 0;

        public bool isVolumeMasterMuted;
        public bool isVolumeMusicMuted;
        public bool isVolumeSfxMuted;
        public bool isWindowed = true;

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

        public readonly List< Resolution> Resolutions = new List< Resolution>()
        {
            {new Resolution(1176, 664)},
            {new Resolution(1280, 720)},
            {new Resolution(1366, 768)},
            {new Resolution(1600, 900)},
            {new Resolution(1920, 1080)},
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

            isVolumeMasterMuted = PlayerPrefs.GetInt(VolumeMasterIsMutedKey, 0) == 0 ? false : true;
            isVolumeMusicMuted = PlayerPrefs.GetInt(VolumeMusicIsMutedKey, 0) == 0 ? false : true;
            isVolumeSfxMuted = PlayerPrefs.GetInt(VolumeSfxIsMutedKey, 0) == 0 ? false : true;

            resolutionIndex = PlayerPrefs.GetInt(ResolutionIndexKey, 0);
            isWindowed = PlayerPrefs.GetInt(ResolutionWindowedKey, 1) == 1 ? true : false;
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
        }

        public void ApplyCurrentResolution()
        {
            PlayerPrefs.SetInt(ResolutionIndexKey, resolutionIndex);
            PlayerPrefs.SetInt(ResolutionWindowedKey, isWindowed ? 1 : 0);
            SetResolution();
        }

        private void SetResolution()
        {
            Screen.SetResolution(Resolutions[resolutionIndex].Width, Resolutions[resolutionIndex].Height, !isWindowed);
        }
    }
}
