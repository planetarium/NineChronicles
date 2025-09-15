using Nekoyume.Game.LiveAsset;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module.Lobby
{
    public class NcuMenu : MainMenu
    {
        [SerializeField]
        private GameObject notification;

        private void OnEnable()
        {
            LiveAssetManager.instance.ObservableHasUnreadNcu.Subscribe(SetNoti).AddTo(gameObject);
        }

        private void SetNoti(bool b)
        {
            notification.SetActive(b);
        }
    }
}
