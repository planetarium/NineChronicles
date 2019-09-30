using System.Collections.Generic;
using System.Linq;
using Nekoyume.Manager;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.TableData;
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
        public BuffLayout buffLayout;

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
                var level = _player.Level;

                _avatarName = States.Instance.currentAvatarState.Value.name;
                TextLevelName.text = $"LV. {level} {_avatarName}";
                TextHP.text = $"{_player.HP}/{_player.HPMax}";
                TextExp.text = $"{_player.EXPMax - _player.EXP}";

                float hpValue = _player.HP / (float) _player.HPMax;
                HPBar.fillRect.gameObject.SetActive(hpValue > 0.0f);
                hpValue = Mathf.Min(Mathf.Max(hpValue, 0.1f), 1.0f);
                HPBar.value = hpValue;

                var expNeed = _player.Model.expNeed;
                var levelExp = _player.EXPMax - expNeed;
                var expValue = (float) (_player.EXP - levelExp) / expNeed;
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
        }

        public void ShowBattleStatus()
        {
            HPBar.gameObject.SetActive(true);
        }

        public void ToggleQuest()
        {
            Toggle(_quest);
        }

        public void UpdateBuff(Dictionary<int, Buff> modelBuffs)
        {
            var buffs = modelBuffs.Values.OrderBy(r => r.Data.Id);
            buffLayout.UpdateBuff(buffs);
        }

        private void Toggle(Widget selected)
        {
            AudioController.PlayClick();

            selected.Toggle();
            if (!selected.IsActive())
            {
                return;
            }
            foreach (var widget in new Widget[] { _inventory, _statusDetail, _quest })
            {
                if (selected != widget)
                    widget.Close();
            }
        }
        
        private void OnRoomEnter()
        {
            Find<Menu>()?.ShowRoom();
        }

        private void OnUpdateStatus()
        {
            UpdateExp();
        }
    }
}
