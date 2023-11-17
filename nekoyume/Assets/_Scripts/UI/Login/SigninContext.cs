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
                ? (SocialType) PlayerPrefs.GetInt(PlayerPrefsIntKeyLatestSignedInSocialType)
                : null;

        public static void SetLatestSignedInSocialType(SocialType socialType)
        {
            PlayerPrefs.SetInt(PlayerPrefsIntKeyLatestSignedInSocialType, (int) socialType);
        }
    }
}
