using System;
using System.Collections.Generic;
using Nekoyume.BlockChain;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Util;
using Nekoyume.State;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using mixpanel;
using Nekoyume.UI.Module;

namespace Nekoyume.UI
{
    public class Login : Widget
    {
        [SerializeField]
        private GameObject[] slots = null;

        public bool ready;
        public List<Player> players;

        private ObjectPool _objectPool;

        protected override void Awake()
        {
            base.Awake();

            if (slots.Length != GameConfig.SlotCount)
            {
                throw new Exception("Login widget's slots.Length is not equals GameConfig.SlotCount.");
            }
            _objectPool = Game.Game.instance.Stage.objectPool;

            Game.Event.OnNestEnter.AddListener(ClearPlayers);
            Game.Event.OnRoomEnter.AddListener(b => ClearPlayers());
            Game.Event.OnRoomEnter.AddListener(b => ReactiveShopState.InitSellDigests());
            CloseWidget = null;
        }

        public void SlotClick(int index)
        {
            if (!ready)
                return;

            Game.Event.OnLoginDetail.Invoke(index);
            gameObject.SetActive(false);
            AudioController.PlayClick();
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            base.Show(ignoreShowAnimation);
            Mixpanel.Track("Unity/LoginImpression");

            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                var playerSlot = slot.GetComponent<LoginPlayerSlot>();

                if (States.Instance.AvatarStates.TryGetValue(i, out var avatarState))
                {
                    playerSlot.LabelLevel.text = $"LV.{avatarState.level}";
                    playerSlot.LabelName.text = avatarState.NameWithHash;
                    playerSlot.CreateView.SetActive(false);
                    playerSlot.NameView.SetActive(true);
                }
                else
                {
                    playerSlot.CreateView.SetActive(true);
                    playerSlot.NameView.SetActive(false);
                }
            }

            AudioController.instance.PlayMusic(AudioController.MusicCode.SelectCharacter);
        }

        private void ClearPlayers()
        {
            foreach (var player in players)
            {
                player.DisableHUD();
                _objectPool.Remove<Player>(player.gameObject);
            }
            _objectPool.ReleaseAll();
            players.Clear();
        }
    }
}
