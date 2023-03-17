using System;
using System.Collections.Generic;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.State.Subjects;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class DccMain : Widget
    {
        [SerializeField]
        private Button dccConnectButton;

        [SerializeField]
        private Button collectionButton;

        [SerializeField]
        private Button backButton;

        [SerializeField]
        private GameObject notification;

        [SerializeField]
        private GameObject connectedObject;

        private readonly List<IDisposable> _disposables = new();

        protected override void OnEnable()
        {
            base.OnEnable();

            void SetNotification()
            {
                var hasNotification = false;
                foreach (var row in TableSheets.Instance.PetSheet)
                {
                    hasNotification |= PetFrontHelper.HasNotification(row.Id);
                }

                notification.SetActive(hasNotification);
            }

            SetNotification();
            AgentStateSubject.Gold.Subscribe(_ =>
            {
                SetNotification();
            }).AddTo(_disposables);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _disposables.DisposeAllAndClear();
        }

        protected override void Awake()
        {
            base.Awake();
            dccConnectButton.onClick.AddListener(() =>
            {
                Find<DccSettingPopup>().ConnectDcc();
            });
            collectionButton.onClick.AddListener(() =>
            {
                Find<DccCollection>().Show(true);
                Close(true);
            });
            backButton.onClick.AddListener(() =>
            {
                CloseWidget.Invoke();
            });
            CloseWidget = () =>
            {
                Close(true);
                Game.Event.OnRoomEnter.Invoke(true);
            };
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            Find<HeaderMenuStatic>().UpdateAssets(HeaderMenuStatic.AssetVisibleState.Mileage);
            connectedObject.SetActive(Dcc.instance.IsConnected.GetValueOrDefault());
            base.Show(ignoreShowAnimation);
        }
    }
}
