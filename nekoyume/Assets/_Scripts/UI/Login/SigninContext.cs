using UnityEngine;

namespace Nekoyume.UI
{
    public class SigninContext
    {
        public enum SocialType
        {
            Google,
            Apple,
            Twitter,
            Discord,
        }

        private const string PlayerPrefsIntKeyLatestSignedInSocialType = "LatestSignedInSocialType";

        public static bool HasLatestSignedInSocialType =>
            PlayerPrefs.HasKey(PlayerPrefsIntKeyLatestSignedInSocialType);

        public static SocialType? LatestSignedInSocialType =>
            PlayerPrefs.HasKey(PlayerPrefsIntKeyLatestSignedInSocialType)
                ? (SocialType)PlayerPrefs.GetInt(PlayerPrefsIntKeyLatestSignedInSocialType)
                : null;

        public static void SetLatestSignedInSocialType(SocialType socialType)
        {
            PlayerPrefs.SetInt(PlayerPrefsIntKeyLatestSignedInSocialType, (int) socialType);
        }


        private const string PlayerPrefsBoolKeyHasSignedWithKeyImport = "HasSignedWithKeyImport";

        public static bool HasSignedWithKeyImport =>
            PlayerPrefs.GetInt(PlayerPrefsBoolKeyHasSignedWithKeyImport, 0) == 1;

        public static void SetHasSignedWithKeyImport(bool keyImport)
        {
            PlayerPrefs.SetInt(PlayerPrefsBoolKeyHasSignedWithKeyImport, keyImport ? 1 : 0);
        }
    }
}
