using System;
using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;
using Unity.Notifications.Android;
#endif

#if UNITY_IOS
using Unity.Notifications.iOS;
using NotificationServices = UnityEngine.iOS.NotificationServices;
using NotificationType = UnityEngine.iOS.NotificationType;
using LocalNotification = UnityEngine.iOS.LocalNotification;
using UnityEngine.Android;
#endif

namespace Nekoyume
{
    public class PushNotifier : MonoBehaviour
    {
        public const string ChannelId = "NineChroniclesLocal";

#if UNITY_ANDROID
        private static int androidApiLevel;
#endif

        static PushNotifier()
        {
#if UNITY_ANDROID
            InitializeAndroid();
#elif UNITY_IOS
            
#endif
        }

#if UNITY_ANDROID
        private static void InitializeAndroid()
        {
            var androidInfo = SystemInfo.operatingSystem;
            Debug.Log("Android info : " + androidInfo);
            androidApiLevel = int.Parse(androidInfo.Substring(androidInfo.IndexOf("-") + 1, 2));
            Debug.Log("Android API Level : " + androidApiLevel);

            if (androidApiLevel >= 33 &&
                !Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
            {
                Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
            }

            if (androidApiLevel >= 26)
            {
                var channel = new AndroidNotificationChannel()
                {
                    Id = "NineChroniclesLocal",
                    Name = "9c Local Notification Channel",
                    Importance = Importance.Default,
                    Description = "For local notification.",
                };
                AndroidNotificationCenter.RegisterNotificationChannel(channel);
            }
        }
#endif

        public static void Push(string title, string text, TimeSpan timespan)
        {
            Debug.Log($"FireTime : {DateTime.Now + timespan}");

#if UNITY_ANDROID
            var fireTime = DateTime.Now + timespan;

            var notification = new AndroidNotification()
            {
                Title = title,
                Text = text,
                FireTime = fireTime,
                IntentData = $"Title : {title}, Text : {text}, FireTime : {fireTime}",
            };
            AndroidNotificationCenter.SendNotification(notification, ChannelId);
#elif UNITY_IOS
            var timeTrigger = new iOSNotificationTimeIntervalTrigger()
            {
                TimeInterval = timespan,
                Repeats = false
            };

            var notification = new iOSNotification()
            {
                Identifier = ChannelId,
                Title = title,
                Body = text,
                Subtitle = title,
                ShowInForeground = true,
                ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound | PresentationOption.Badge),
                CategoryIdentifier = "9c_local_push",
                Trigger = timeTrigger,
            };

            iOSNotificationCenter.ScheduleNotification(notification);      
#endif
        }
    }
}
