using System;
using Nekoyume.Action;
using UnityEngine;
using UnityEngine.UI;


namespace Nekoyume.UI
{
    public class Status : Widget
    {
        public Text TextLevelName;
        public Text TextStage;
        public Text TextExp;
        public Slider ExpBar;

        private string _avatarName = "";
        private Game.Character.Player _player = null;

        private void Awake()
        {
            Game.Event.OnUpdateStatus.AddListener(OnUpdateStatus);
        }

        private void OnUpdateStatus()
        {
            UpdateExp();
        }

        public void UpdatePlayer(GameObject playerObj)
        {
            Show();

            Model.Avatar avatar = ActionManager.Instance.Avatar;
            if (avatar != null)
            {
                _avatarName = avatar.Name;
                TextStage.text = $"STAGE {avatar.WorldStage}";
            }
            if (playerObj != null)
            {
                _player = playerObj.GetComponent<Game.Character.Player>();
            }

            UpdateExp();
        }

        private void UpdateExp()
        {
            if (_player != null)
            {
                TextLevelName.text = $"LV. {_player.Level} {_avatarName}";  
                TextExp.text = $"{_player.EXP} / {_player.EXPMax}";

                float value = (float)_player.EXP / (float)_player.EXPMax;
                if (value <= 0.0f)
                    ExpBar.fillRect.gameObject.SetActive(false);
                else
                    ExpBar.fillRect.gameObject.SetActive(true);

                if (value > 1.0f)
                    value = 1.0f;
                if (value < 0.1f)
                    value = 0.1f;

                ExpBar.value = value;
            }
        }

        public void ToggleInventory()
        {
            Widget.Find<Inventory>().Toggle();
        }
    }
}
