using Nekoyume.Manager;
using Nekoyume.BlockChain;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Item;
using Nekoyume.Game.VFX;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class Status : Widget
    {
        public BottomMenu bottomMenu;
        public Text TextLevelName;
        public StageTitle stageTitle;
        public Text TextStage;
        public Text TextHP;
        public Text TextExp;
        public Slider HPBar;
        public Slider ExpBar;
        public Toggle BtnStatus;
        public Toggle BtnInventory;
        public Toggle BtnQuest;
        public DropItemInventoryVFX InventoryVfx;

        private string _avatarName = "";
        private Player _player;

        private StatusDetail _statusDetail;
        private Inventory _inventory;
        private Quest _quest;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            Game.Event.OnRoomEnter.AddListener(OnRoomEnter);
            Game.Event.OnUpdateStatus.AddListener(OnUpdateStatus);
            Game.Event.OnGetItem.AddListener(OnGetItem);
            InventoryVfx.Stop();
        }

        private void OnGetItem(DropItem dropItem)
        {
            InventoryVfx.Play();
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
            if (ReferenceEquals(_inventory, null))
            {
                throw new NotFoundComponentException<Inventory>();
            }

            _quest = Find<Quest>();
            if (ReferenceEquals(_quest, null))
            {
                throw new NotFoundComponentException<Quest>();
            }

            HPBar.gameObject.SetActive(false);
        }

        public override void Close()
        {
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
                _avatarName = States.Instance.currentAvatarState.Value.name;
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
            Toggle(_inventory);
                
            AnalyticsManager.Instance.OnEvent(Find<Menu>().gameObject.activeSelf
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
            Toggle(_statusDetail);

            AnalyticsManager.Instance.OnEvent(Find<Menu>().gameObject.activeSelf
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
            bottomMenu.Show(
                BottomMenu.ButtonHideFlag.Main |
                BottomMenu.ButtonHideFlag.Dictionary);
            bottomMenu.FadeInAlpha(0.5f);
            HPBar.gameObject.SetActive(true);
            stageTitle.Show(stage);
        }

        public void ToggleQuest()
        {
            Toggle(_quest);
        }

        private void Toggle(Widget selected)
        {
            AudioController.PlayClick();

            selected.Toggle();
            if (!selected.IsActive())
            {
                return;
            }
            foreach (var widget in new Widget[] {_inventory, _statusDetail, _quest})
            {
                if (selected != widget)
                    widget.Close();
            }
        }
        
        private void OnRoomEnter()
        {
            stageTitle.gameObject.SetActive(false);
            Find<Menu>()?.ShowRoom();
        }

        private void OnUpdateStatus()
        {
            UpdateExp();
        }
    }
}
