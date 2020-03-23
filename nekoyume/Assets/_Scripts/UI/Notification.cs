using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Scroller;
using UniRx;

namespace Nekoyume.UI
{
    /// <summary>
    /// Usage: Just to call the `Notification.Push()` method anywhere.
    /// </summary>
    public class Notification : SystemInfoWidget
    {
        private const float LifeTimeOfEachNotification = 10f;

        private static readonly ReactiveCollection<NotificationCellView.Model> Models =
            new ReactiveCollection<NotificationCellView.Model>();

        private static readonly List<Type> WidgetTypesForUX = new List<Type>();

        private static int _widgetEnableCount;

        private static readonly List<(MailType mailType, string message, long requiredBlockIndex, Guid itemId)> ReservationList =
            new List<(MailType mailType, string message, long requiredBlockIndex, Guid itemId)>();

        public static IReadOnlyCollection<NotificationCellView.Model> SharedModels => Models;

        public NotificationScrollerController scroller;

        public static void Push(MailType mailType, string message)
        {
            Push(mailType, message, string.Empty, null);
        }

        public static void Push(MailType mailType, string message, string submitText, System.Action submitAction)
        {
            if (_widgetEnableCount > 0)
            {
                return;
            }

            Models.Add(new NotificationCellView.Model
            {
                mailType = mailType,
                message = message,
                submitText = submitText,
                submitAction = submitAction,
                addedAt = DateTime.Now
            });
        }

        /// <summary>
        /// This class consider if there is any widget raise `OnEnableSubject` subject in the `WidgetTypesForUX` property.
        /// Widget type can registered once and cannot unregistered.
        /// </summary>
        public static void RegisterWidgetTypeForUX<T>() where T : Widget
        {
            var type = typeof(T);
            if (WidgetTypesForUX.Contains(type))
            {
                return;
            }

            WidgetTypesForUX.Add(type);
        }

        public static void Reserve(MailType mailType, string message, long requiredBlockIndex, Guid itemId)
        {
            ReservationList.Add((mailType, message, requiredBlockIndex, itemId));
        }

        #region Mono

        protected override void Awake()
        {
            base.Awake();
            OnEnableStaticSubject.Subscribe(SubscribeOnEnable).AddTo(gameObject);
            OnDisableStaticSubject.Subscribe(SubscribeOnDisable).AddTo(gameObject);
            scroller.onRequestToRemoveModelByIndex.Subscribe(SubscribeToRemoveModel).AddTo(gameObject);
            scroller.SetModel(Models);

            CloseWidget = null;
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread().Subscribe(SubscribeBlockIndex)
                .AddTo(gameObject);
        }

        protected override void Update()
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

        #endregion

        private static void SubscribeOnEnable(Widget widget)
        {
            var type = widget.GetType();
            if (!WidgetTypesForUX.Contains(type))
            {
                return;
            }

            _widgetEnableCount++;

            if (_widgetEnableCount == 1)
            {
                Models.Clear();
            }
        }

        private static void SubscribeOnDisable(Widget widget)
        {
            var type = widget.GetType();
            if (!WidgetTypesForUX.Contains(type))
            {
                return;
            }

            if (_widgetEnableCount == 0)
            {
                return;
            }

            _widgetEnableCount--;
        }

        private void SubscribeToRemoveModel(int index)
        {
            if (index >= Models.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            Models.RemoveAt(index);
        }

        private static void SubscribeBlockIndex(long blockIndex)
        {
            var list = ReservationList
                .Where(i => i.requiredBlockIndex <= blockIndex).ToList();
            foreach (var valueTuple in list)
            {
                Push(valueTuple.mailType, valueTuple.message);
                ReservationList.Remove(valueTuple);
            }
        }

        public static void Remove(Guid itemUsableItemId)
        {
            if (ReservationList.Any())
            {
                var message =
                    ReservationList.FirstOrDefault(m => m.itemId == itemUsableItemId);
                if (message != default)
                {
                    ReservationList.Remove(message);
                }
            }
        }
    }
}
