using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI.Extensions.EasingCore;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.UI
{
    using UniRx;

    public class OneLineSystem : SystemWidget
    {
        private enum State
        {
            None,
            Idle,
            Add,
            Remove
        }

        private class ReservationModel
        {
            public MailType mailType;
            public string message;
            public long requiredBlockIndex;
            public Guid itemId;
        }

        private const float InternalTimeToAddOrRemoveCell = 1f;
        private const float LifeTimeOfEachNotification = 1.5f;

        private static State _state = State.None;

        private static readonly int CellMaxCount = Mathf.CeilToInt(LifeTimeOfEachNotification);

        private static readonly List<NotificationCell.ViewModel> SharedModel =
            new List<NotificationCell.ViewModel>();

        private static readonly ConcurrentQueue<NotificationCell.ViewModel> AddQueue =
            new ConcurrentQueue<NotificationCell.ViewModel>();

        private static readonly List<ReservationModel> ReservationList =
            new List<ReservationModel>();

        [SerializeField] private NotificationScroll scroll = null;

        private float _lastTimeToAddOrRemoveCell;

        #region Control

        public static void Push(
            MailType mailType,
            string message,
            NotificationCell.NotificationType notificationType)
        {
            if (AddQueue.Any() &&
                AddQueue.FirstOrDefault(x => x.message == message) != null)
            {
                return;
            }

            AddQueue.Enqueue(new NotificationCell.ViewModel
            {
                mailType = mailType,
                message = message,
                notificationType = notificationType,
            });
        }

        public static void Reserve(MailType mailType,
            string message,
            long requiredBlockIndex,
            Guid itemId)
        {
            ReservationList.Add(new ReservationModel
            {
                mailType = mailType,
                message = message,
                requiredBlockIndex = requiredBlockIndex,
                itemId = itemId
            });
        }

        public static void CancelReserve(Guid itemUsableItemId)
        {
            if (!ReservationList.Any())
            {
                return;
            }

            var message = ReservationList.FirstOrDefault(m => m.itemId == itemUsableItemId);
            if (message is null)
            {
                return;
            }

            ReservationList.Remove(message);
        }

        #endregion

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            scroll.UpdateData(SharedModel, true);
            scroll.OnCompleteOfAddAnimation.Merge(scroll.OnCompleteOfRemoveAnimation)
                .Where(_ => SharedModel.All(item =>
                    item.animationState == NotificationCell.AnimationState.Idle))
                .Subscribe(_ => _state = State.Idle).AddTo(gameObject);

            CloseWidget = null;
            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(SubscribeBlockIndex).AddTo(gameObject);

            _state = State.Idle;
            StartCoroutine(CoUpdate());
        }

        private IEnumerator CoUpdate()
        {
            while (true)
            {
                var deltaTime = Time.time - _lastTimeToAddOrRemoveCell;
                if (deltaTime < InternalTimeToAddOrRemoveCell)
                {
                    yield return new WaitForSeconds(InternalTimeToAddOrRemoveCell - deltaTime);
                }

                while (_state != State.Idle)
                {
                    yield return null;
                }

                if (TryAddCell())
                {
                    _state = State.Add;
                    _lastTimeToAddOrRemoveCell = Time.time;
                }
                else if (TryRemoveCell())
                {
                    _state = State.Remove;
                    _lastTimeToAddOrRemoveCell = Time.time;
                }

                yield return null;
            }
        }

        #endregion

        private bool TryAddCell()
        {
            if (AddQueue.Count == 0 || SharedModel.Count >= CellMaxCount ||
                !AddQueue.TryDequeue(out var viewModel))
            {
                return false;
            }

            viewModel.addedTime = Time.time;
            SharedModel.Add(viewModel);
            scroll.UpdateData(SharedModel, true);
            return true;
        }

        private bool TryRemoveCell()
        {
            if (SharedModel.Count == 0)
            {
                return false;
            }

            var target = SharedModel[0];
            var deltaTime = Time.time - target.addedTime;
            if (deltaTime < LifeTimeOfEachNotification)
            {
                return false;
            }

            scroll.PlayCellRemoveAnimation(0).First().Subscribe(_ =>
            {
                SharedModel.RemoveAt(0);
                scroll.UpdateData(SharedModel, true);
            });

            scroll.ScrollTo(1, .5f, Ease.InCirc);
            return true;
        }

        #region Subscribe

        private static void SubscribeBlockIndex(long blockIndex)
        {
            foreach (var reservationModel in ReservationList
                .Where(i => i.requiredBlockIndex <= blockIndex).ToList())
            {
                Push(reservationModel.mailType,
                    reservationModel.message,
                    NotificationCell.NotificationType.Notification);
                ReservationList.Remove(reservationModel);
            }
        }

        #endregion
    }
}
