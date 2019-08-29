using System;
using System.Collections.Generic;
using DefaultNamespace;
using Nekoyume.UI.Scroller;
using UniRx;

namespace Nekoyume.UI
{
    /// <summary>
    /// 사용법: 어디서든 `Notification.Push()`함수를 호출하세요.
    /// </summary>
    public class Notification : SystemInfoWidget
    {
        private const float LifeTimeOfEachNotification = 10f;

        private static readonly ReactiveCollection<NotificationCellView.Model> Models =
            new ReactiveCollection<NotificationCellView.Model>();

        public static IReadOnlyCollection<NotificationCellView.Model> SharedModels => Models;

        public NotificationScrollerController scroller;

        public static void Push(string message)
        {
            Push(message, string.Empty, null);
        }

        public static void Push(string message, string submitText, System.Action submitAction)
        {
            Models.Add(new NotificationCellView.Model
            {
                message = message,
                submitText = submitText,
                submitAction = submitAction,
                addedAt = DateTime.Now
            });
        }

        protected override void Awake()
        {
            base.Awake();
            scroller.onRequestToRemoveModelByIndex.Subscribe(SubscribeToRemoveModel).AddTo(gameObject);
            scroller.SetModel(Models);
        }

        private void Update()
        {
            var now = DateTime.Now;
            for (var i = 0; i < Models.Count; i++)
            {
                var model = Models[i];
                var timeSpan = now - model.addedAt;
                if (timeSpan.TotalSeconds < LifeTimeOfEachNotification)
                    continue;

                SubscribeToRemoveModel(i);
                break;
            }
        }

        private void SubscribeToRemoveModel(int index)
        {
            if (index >= Models.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            Models.RemoveAt(index);
        }
    }
}
