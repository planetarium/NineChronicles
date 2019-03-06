using Nekoyume.Game.Character;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Status : Widget
    {
        public Text TextLevelName;
        public Text TextStage;
        public Text TextHP;
        public Text TextExp;
        public Slider HPBar;
        public Slider ExpBar;
        public Toggle BtnStatus;
        public Toggle BtnInventory;

        private string _avatarName = "";
        private Player _player = null;

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

            if (playerObj != null)
            {
                _player = playerObj.GetComponent<Player>();
                TextStage.text = $"STAGE {_player.model.stage}";
            }

            UpdateExp();
        }

        private void UpdateExp()
        {
            if (_player != null)
            {
                TextLevelName.text = $"LV. {_player.Level} {_avatarName}";  
                TextHP.text = $"{_player.HP}/{_player.HPMax}";
                TextExp.text = $"{_player.EXP}/{_player.EXPMax}";

                float hpValue = _player.HP / (float) _player.HPMax;
                HPBar.fillRect.gameObject.SetActive(hpValue > 0.0f);
                hpValue = Mathf.Min(Mathf.Max(hpValue, 0.1f), 1.0f);
                HPBar.value = hpValue;

                float expValue = _player.EXP / (float) _player.EXPMax;
                ExpBar.fillRect.gameObject.SetActive(expValue > 0.0f);
                expValue = Mathf.Min(Mathf.Max(expValue, 0.1f), 1.0f);
                ExpBar.value = expValue;
            }
        }

        public void ToggleInventory()
        {
            Find<StatusDetail>().Close();
            Find<Inventory>().Toggle();
        }

        public void ToggleStatus()
        {
            Find<Inventory>().Close();
            Find<StatusDetail>().Toggle();
        }
    }
}
