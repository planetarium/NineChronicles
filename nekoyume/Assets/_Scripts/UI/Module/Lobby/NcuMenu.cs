using System;
using Nekoyume.Game.LiveAsset;
using UniRx;
using UnityEngine;
using ObservableExtensions = UniRx.ObservableExtensions;

namespace Nekoyume.UI.Module.Lobby
{
    public class NcuMenu : MainMenu
    {
        [SerializeField]
        private GameObject notification;

        protected override void Awake()
        {
            base.Awake();
            Game.Lobby.OnLobbyEnterEvent += OnLobbyEnter;
        }

        private void OnDestroy()
        {
            Game.Lobby.OnLobbyEnterEvent -= OnLobbyEnter;
        }

        private void OnLobbyEnter()
        {
            ObservableExtensions.Subscribe(LiveAssetManager.instance.ObservableHasUnreadNcu, SetNoti).AddTo(gameObject);
        }

        public void SetNoti(bool b)
        {
            Debug.Log($"[NcuMenu] SetNoti: {b}");
            notification.SetActive(b);
        }
    }
}
