using System;
using System.Collections.Generic;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.State.Subjects;
using UnityEngine;

namespace Nekoyume.UI.Module.Lobby
{
    using UniRx;

    public class DccMenu : MainMenu
    {
        [SerializeField]
        private GameObject notification;

        private readonly List<IDisposable> _disposables = new();

        private void OnEnable()
        {
            void SetNotification()
            {
                var hasNotification = false;
                if (Game.Game.instance.IsInitialized)
                {
                    foreach (var row in TableSheets.Instance.PetSheet)
                    {
                        hasNotification |= PetFrontHelper.HasNotification(row.Id);
                    }
                }

                notification.SetActive(hasNotification);
            }

            SetNotification();
            AgentStateSubject.Gold.Subscribe(_ => { SetNotification(); }).AddTo(_disposables);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }
    }
}
