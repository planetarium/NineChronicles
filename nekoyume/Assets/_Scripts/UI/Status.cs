using System.Collections.Generic;
using DG.Tweening;
using Nekoyume.Manager;
using Nekoyume.Game.Character;
using Nekoyume.Game.Controller;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Timer;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using Nekoyume.Model.Buff;

namespace Nekoyume.UI
{
    public class Status : Widget
    {
        public TextMeshProUGUI textLvName;
        public TextMeshProUGUI textHp;
        public TextMeshProUGUI textExp;
        public Image hpBar;
        public Image expBar;
        public BuffLayout buffLayout;
        public BuffTooltip buffTooltip;
        public BattleTimerView battleTimerView;

        private string _avatarName = "";
        private Player _player;

        private StatusDetail _statusDetail;
        private Inventory _inventory;
        private Quest _quest;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            Game.Event.OnRoomEnter.AddListener(b => Show());
            Game.Event.OnUpdatePlayerStatus.Subscribe(SubscribeOnUpdatePlayerStatus)
                .AddTo(gameObject);

            CloseWidget = null;
        }

        #endregion

        public override void Show(bool ignoreStartAnimation = false)
        {
            base.Show(ignoreStartAnimation);
            battleTimerView.Close();

            _statusDetail = Find<StatusDetail>();
            if (_statusDetail is null)
            {
                throw new NotFoundComponentException<StatusDetail>();
            }

            _inventory = Find<Inventory>();
            if (_inventory is null)
            {
                throw new NotFoundComponentException<Inventory>();
            }

            _quest = Find<Quest>();
            if (_quest is null)
            {
                throw new NotFoundComponentException<Quest>();
            }

            hpBar.transform.parent.gameObject.SetActive(false);
            buffLayout.SetBuff(null);
        }

        private void SubscribeOnUpdatePlayerStatus(Player player)
        {
            if (!player ||
                player is EnemyPlayer ||
                player.Model is null)
            {
                return;
            }

            UpdateExp();
            SetBuffs(player.Model.Buffs);
        }

        public void UpdatePlayer(Player player)
        {
            Show();

            if (player)
            {
                _player = player;
            }

            UpdateExp();
        }

        #region Buff

        public void SetBuffs(IReadOnlyDictionary<int, Buff> value)
        {
            buffLayout.SetBuff(value);
        }

        public void ShowBuffTooltip(GameObject sender)
        {
            var icon = sender.GetComponent<BuffIcon>();
            var iconRectTransform = icon.image.rectTransform;

            buffTooltip.gameObject.SetActive(true);
            buffTooltip.UpdateText(icon.Data);
            buffTooltip.RectTransform.anchoredPosition =
                iconRectTransform.anchoredPosition + Vector2.down * iconRectTransform.sizeDelta.y;
        }

        public void HideBuffTooltip()
        {
            buffTooltip.gameObject.SetActive(false);
        }

        #endregion

        private void UpdateExp()
        {
            if (!_player)
            {
                return;
            }

            var level = _player.Level;

            _avatarName = States.Instance.CurrentAvatarState.NameWithHash;
            textLvName.text = $"<color=#B38271>LV. {level}</color> {_avatarName}";
            var displayHp = _player.CurrentHP;
            textHp.text = $"{displayHp} / {_player.HP}";
            textExp.text =
                $"{_player.Model.Exp.Need - _player.EXPMax + _player.EXP} / {_player.Model.Exp.Need}";

            var hpValue = _player.CurrentHP / (float) _player.HP;
            hpBar.gameObject.SetActive(hpValue > 0.0f);
            hpValue = Mathf.Min(Mathf.Max(hpValue, 0.1f), 1.0f);
            hpBar.fillAmount = hpValue;

            var expNeed = _player.Model.Exp.Need;
            var levelExp = _player.EXPMax - expNeed;
            var expValue = (float) (_player.EXP - levelExp) / expNeed;
            expBar.gameObject.SetActive(expValue > 0.0f);
            expValue = Mathf.Min(Mathf.Max(expValue, 0.1f), 1.0f);
            expBar.fillAmount = expValue;
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
            hpBar.transform.parent.gameObject.SetActive(true);
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
    }
}
