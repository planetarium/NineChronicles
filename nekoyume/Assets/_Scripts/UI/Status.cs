using Nekoyume.Game.Character;
using Nekoyume.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Timer;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;

namespace Nekoyume.UI
{
    public class Status : Widget
    {
        [SerializeField]
        private FramedCharacterView characterView = null;

        [SerializeField]
        private TextMeshProUGUI textLvName = null;

        [SerializeField]
        private TextMeshProUGUI textHp = null;

        [SerializeField]
        private TextMeshProUGUI textExp = null;

        [SerializeField]
        private Image hpBar = null;

        [SerializeField]
        private Image expBar = null;

        [SerializeField]
        private BuffLayout buffLayout = null;

        [SerializeField]
        private BuffTooltip buffTooltip = null;

        [SerializeField]
        private BattleTimerView battleTimerView = null;

        private string _avatarName = "";
        private Player _player;

        #region Mono

        protected override void Awake()
        {
            base.Awake();

            Game.Event.OnRoomEnter.AddListener(b => Show());
            Game.Event.OnUpdatePlayerEquip
                .Subscribe(characterView.SetByPlayer)
                .AddTo(gameObject);
            Game.Event.OnUpdatePlayerStatus.Subscribe(SubscribeOnUpdatePlayerStatus)
                .AddTo(gameObject);

            CloseWidget = null;
        }

        #endregion

        public override void Show(bool ignoreStartAnimation = false)
        {
            base.Show(ignoreStartAnimation);
            battleTimerView.Close();
            hpBar.transform.parent.gameObject.SetActive(false);
            buffLayout.SetBuff(null);
        }

        public void ShowBattleStatus()
        {
            hpBar.transform.parent.gameObject.SetActive(true);
        }

        public void ShowBattleTimer(int timeLimit)
        {
            battleTimerView.Show(timeLimit);
        }

        // NOTE: call from Hierarchy
        public void ShowBuffTooltip(GameObject sender)
        {
            var icon = sender.GetComponent<BuffIcon>();
            var iconRectTransform = icon.image.rectTransform;

            buffTooltip.gameObject.SetActive(true);
            buffTooltip.UpdateText(icon.Data);
            buffTooltip.RectTransform.anchoredPosition =
                iconRectTransform.anchoredPosition + Vector2.down * iconRectTransform.sizeDelta.y;
        }

        // NOTE: call from Hierarchy
        public void HideBuffTooltip()
        {
            buffTooltip.gameObject.SetActive(false);
        }

        public void UpdatePlayer(Player player)
        {
            characterView.SetByPlayer(player);
            Show();

            if (player)
            {
                _player = player;
            }

            UpdateExp();
        }

        private void SubscribeOnUpdatePlayerStatus(Player player)
        {
            if (player is null ||
                player is EnemyPlayer ||
                player.Model is null)
            {
                return;
            }

            UpdateExp();
            buffLayout.SetBuff(player.Model.Buffs);
        }

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
    }
}
