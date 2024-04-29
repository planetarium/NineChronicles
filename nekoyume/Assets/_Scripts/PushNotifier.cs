using System;
using UnityEngine;
using Random = UnityEngine.Random;
using Nekoyume.L10n;

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
        public enum PushType
        {
            Reward,
            Workshop,
            Arena,
            Worldboss,
            PatrolReward
        }

        public const string ChannelId = "NineChroniclesLocal";

        public static readonly TimeSpan NightStartTime = new TimeSpan(21, 0, 0);
        public static readonly TimeSpan NightEndTime = new TimeSpan(8, 0, 0);

#if UNITY_ANDROID
        private static int androidApiLevel;
#endif

        static PushNotifier()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            InitializeAndroid();
#elif !UNITY_EDITOR && UNITY_IOS

#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
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
        /// <summary>
        /// Reserves push notification. (Works in Android/iOS)
        /// <returns>
        /// Notification identifier.
        /// This will be parsed as int in Android.
        /// </returns>
        public static string Push(string text, TimeSpan timespan, PushType pushType)
        {
            if (!Settings.Instance.isPushEnabled || timespan.Ticks <= 0)
            {
                return string.Empty;
            }

            // night-time push (21:00-08:00)
            var fireTime = DateTime.Now + timespan;
            var timeOfDay = fireTime.TimeOfDay;
            if (!Settings.Instance.isNightTimePushEnabled &&
                (timeOfDay >= NightStartTime || timeOfDay <= NightEndTime))
            {
                return string.Empty;
            }

            // filter by push type
            switch (pushType)
            {
                case PushType.Reward:
                    if (!Settings.Instance.isRewardPushEnabled)
                    {
                        return string.Empty;
                    }

                    break;
                case PushType.Workshop:
                    if (!Settings.Instance.isWorkshopPushEnabled)
                    {
                        return string.Empty;
                    }

                    break;
                case PushType.Arena:
                    if (!Settings.Instance.isArenaPushEnabled)
                    {
                        return string.Empty;
                    }

                    break;
                case PushType.Worldboss:
                    if (!Settings.Instance.isWorldbossPushEnabled)
                    {
                        return string.Empty;
                    }

                    break;
                case PushType.PatrolReward:
                    if (!Settings.Instance.isPatrolRewardPushEnabled)
                    {
                        return string.Empty;
                    }
                    break;
            }

            NcDebug.Log($"FireTime : {fireTime}");
            var title = L10nManager.Localize("TITLE");
            var iconName = pushType.ToString().ToLower();

#if UNITY_ANDROID
            var notification = new AndroidNotification()
            {
                Title = title,
                Text = text,
                FireTime = fireTime,
                IntentData = $"Title : {title}, Text : {text}, FireTime : {fireTime}",
                LargeIcon = iconName,
            };

            var identifier = AndroidNotificationCenter.SendNotification(notification, ChannelId);
            return identifier.ToString();
#elif UNITY_IOS
            // NOTE : This is not tested.

            var timeTrigger = new iOSNotificationTimeIntervalTrigger()
            {
                TimeInterval = timespan,
                Repeats = false
            };

            var identifier = Random.Range(int.MinValue, int.MaxValue).ToString();
            var notification = new iOSNotification()
            {
                Identifier = identifier,
                Title = title,
                Body = text,
                ShowInForeground = true,
                ForegroundPresentationOption =
                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             (PresentationOption.Alert | PresentationOption.Sound | PresentationOption.Badge),
                CategoryIdentifier = "9c_local_push",
                Trigger = timeTrigger,
            };

            iOSNotificationCenter.ScheduleNotification(notification);
            return identifier;
#else
            return default;
#endif
        }

        public static void CancelReservation(string identifier)
        {
#if UNITY_ANDROID
            if (int.TryParse(identifier, out var outIdentifier))
            {
                AndroidNotificationCenter.CancelNotification(outIdentifier);
            }
#elif UNITY_IOS
            iOSNotificationCenter.RemoveScheduledNotification(identifier);
#else

#endif
        }
    }
}
