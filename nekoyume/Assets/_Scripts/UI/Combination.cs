using Nekoyume.Game;
using UnityEngine;

namespace Nekoyume.UI
{
    public class Combination : Widget
    {
        private Stage _stage;
        private Game.Character.Player _player;

        private void Awake()
        {
            _stage = GameObject.Find("Stage").GetComponent<Stage>();
        }

        public override void Show()
        {
            _player = FindObjectOfType<Game.Character.Player>();
            if (_player != null)
            {
                _player.gameObject.SetActive(false);
            }

            _stage.LoadBackground("combination");
            GetComponentInChildren<Inventory>()?.Show();
            base.Show();
        }

        public override void Close()
        {
            if (_player != null)
            {
                _player.gameObject.SetActive(true);
            }

            Find<Status>()?.Show();
            Find<Menu>()?.Show();
            _stage.LoadBackground("room");
            base.Close();
        }
    }
}
