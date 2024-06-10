using System.Collections.Generic;
using Nekoyume.Game;
using Nekoyume.Game.Character;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Timer;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Nekoyume.UI
{
    using UniRx;

    public class Status : Widget
    {
        [SerializeField]
        private FramedCharacterView characterView = null;

        [SerializeField]
        private TextMeshProUGUI textName = null;

        [SerializeField]
        private TextMeshProUGUI textLevel = null;

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

        private Player _player;

        private int? _activatedDccId;
        #region Mono

        protected override void Awake()
        {
            base.Awake();

            Game.Event.OnRoomEnter.AddListener(b => Show());
            Game.Event.OnUpdatePlayerEquip.Where(_ => _activatedDccId != null)
                .Subscribe(characterView.SetByPlayer).AddTo(gameObject);
            Game.Event.OnUpdatePlayerStatus.Subscribe(SubscribeOnUpdatePlayerStatus)
                .AddTo(gameObject);

            characterView.OnClickCharacterIcon
                .Subscribe(_ =>
                {
#if UNITY_ANDROID || UNITY_IOS
                    Find<Alert>().Show("UI_ALERT_NOT_IMPLEMENTED_TITLE",
                        "UI_ALERT_NOT_IMPLEMENTED_CONTENT");
#else
                    Find<ProfileSelectPopup>().Show();
#endif
                })
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

#if UNITY_ANDROID || UNITY_IOS
            this.transform.SetSiblingIndex(Widget.Find<Menu>().transform.GetSiblingIndex()+1);
#endif
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

        public void UpdateOnlyPlayer(Player player)
        {
            if (_activatedDccId == null)
            {
                characterView.SetByPlayer(player);
            }

            if (player)
            {
                _player = player;
            }

            UpdateExp();
        }

        public void UpdatePlayer(Player player)
        {
            if (_activatedDccId == null)
            {
                characterView.SetByPlayer(player);
            }

            Show();

            if (player)
            {
                _player = player;
            }

            UpdateExp();
        }

        private void SubscribeOnUpdatePlayerStatus(Player player)
        {
            if (player is null || player is EnemyPlayer || player.Model is null)
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
            textLevel.text = level.ToString();
            var displayHp = _player.CurrentHp;
            textHp.text = $"{displayHp} / {_player.Hp}";
            textExp.text =
                $"{_player.Model.Exp.Need - _player.EXPMax + _player.EXP} / {_player.Model.Exp.Need}";

            var hpValue = _player.CurrentHp / (float) _player.Hp;
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

        public void UpdateForLobby(
            AvatarState avatarState,
            List<Equipment> equipments,
            List<Costume> costumes
        )
        {
            // portrait
            if (Dcc.instance.Avatars.TryGetValue(avatarState.address.ToString(), out var dccId))
            {
                _activatedDccId = dccId;
                characterView.SetByDccId(dccId);
            }
            else
            {
                _activatedDccId = null;
                var portraitId = Util.GetPortraitId(equipments, costumes);
                characterView.SetByFullCostumeOrArmorId(portraitId);
            }

            // level& name
            textLevel.text = avatarState.level.ToString();
            textName.text = avatarState.NameWithHash;

            // exp
            var levelSheet = Game.Game.instance.TableSheets.CharacterLevelSheet;
            if (levelSheet.TryGetValue(avatarState.level, out var levelRow))
            {
                var currentExp = avatarState.exp - levelRow.Exp;
                textExp.text = $"{currentExp} / {levelRow.ExpNeed}";
                expBar.fillAmount = (float)currentExp / levelRow.ExpNeed;
            }
        }
    }
}
