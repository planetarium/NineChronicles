using UnityEngine;
#if UNITY_ANDROID || !UNITY_EDITOR
namespace OneStore.Common
{
    public static class JniHelper
    {
         /// <summary>
        /// Returns the Android activity context of the Unity app.
        /// </summary>
        public static AndroidJavaObject GetUnityAndroidActivity()
        {
            return new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>(
                "currentActivity");
        }

        /// <summary>
        /// Returns the Android application context of the Unity app.
        /// </summary>
        public static AndroidJavaObject GetApplicationContext()
        {
            return GetUnityAndroidActivity().Call<AndroidJavaObject>("getApplicationContext");
        }

        /// <summary>
        /// Create a Java ArrayList of strings.
        /// </summary>
        public static AndroidJavaObject CreateJavaArrayList(params string[] inputs)
        {
            var javaList = new AndroidJavaObject("java.util.ArrayList");
            foreach (var input in inputs)
            {
                javaList.Call<bool>("add", input);
            }

            return javaList;
        }
    }
}
#endif
