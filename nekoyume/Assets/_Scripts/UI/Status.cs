using Nekoyume.Manager;
using Nekoyume.Action;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Status : Widget
    {
        public Text TextLevelName;
        public StageTitle stageTitle;
        public Text TextStage;
        public Text TextHP;
        public Text TextExp;
        public Slider HPBar;
        public Slider ExpBar;
        public Toggle BtnStatus;
        public Toggle BtnInventory;

        private string _avatarName = "";
        private Player _player;

        private StatusDetail _statusDetail;
        private Inventory _inventory;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            Game.Event.OnRoomEnter.AddListener(OnRoomEnter);
            Game.Event.OnUpdateStatus.AddListener(OnUpdateStatus);
        }
        
        #endregion

        public override void Show()
        {
            base.Show();

            _statusDetail = Find<StatusDetail>();
            if (ReferenceEquals(_statusDetail, null))
            {
                throw new NotFoundComponentException<StatusDetail>();
            }
            
            _inventory = Find<Inventory>();
            if (ReferenceEquals(_statusDetail, null))
            {
                throw new NotFoundComponentException<Inventory>();
            }
        }

        public override void Close()
        {
            foreach (var toggle in new[] {BtnStatus, BtnInventory})
            {
                if (toggle.isOn)
                {
                    toggle.isOn = false;
                }
            }
            stageTitle.Close();

            base.Close();
        }

        public void UpdatePlayer(GameObject playerObj)
        {
            Show();

            if (playerObj != null)
            {
                _player = playerObj.GetComponent<Player>();
            }

            UpdateExp();
        }

        private void UpdateExp()
        {
            if (_player != null)
            {
                _avatarName = ActionManager.instance.Avatar.Name;
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
            AudioController.PlayClick();

            _inventory.Toggle();
            
            if (!_inventory.IsActive())
            {
                return;
            }
            
            if (_statusDetail.IsActive())
            {
                _statusDetail.Close();   
            }
                
            AnalyticsManager.instance.OnEvent(Find<Menu>().gameObject.activeSelf
                ? AnalyticsManager.EventName.ClickMainInventory
                : AnalyticsManager.EventName.ClickBattleInventory);
        }

        public void CloseInventory()
        {
            if (!_inventory.IsActive())
            {
                return;
            }
            
            _inventory.Close();

            if (BtnInventory.isOn)
            {
                BtnInventory.isOn = false;
            }
        }

        public void ToggleStatus()
        {
            AudioController.PlayClick();
            
            _statusDetail.Toggle();
            if (!_statusDetail.IsActive())
            {
                return;
            }

            if (_inventory.IsActive())
            {
                _inventory.Close();
            }

            AnalyticsManager.instance.OnEvent(Find<Menu>().gameObject.activeSelf
                ? AnalyticsManager.EventName.ClickMainEquipment
                : AnalyticsManager.EventName.ClickBattleEquipment);
        }

        public void CloseStatusDetail()
        {
            if (!_statusDetail.IsActive())
            {
                return;
            }
            
            _statusDetail.Close();

            if (BtnStatus.isOn)
            {
                BtnStatus.isOn = false;
            }
        }

        public void ShowStage(int stage)
        {
            stageTitle.Show(stage);
        }
        
        private void OnRoomEnter()
        {
            stageTitle.gameObject.SetActive(false);
        }

        private void OnUpdateStatus()
        {
            UpdateExp();
        }
    }
}
