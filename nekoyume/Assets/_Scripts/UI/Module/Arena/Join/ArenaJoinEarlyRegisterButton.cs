using System;
using System.Globalization;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module.Arena.Emblems;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Arena.Join
{
    using UniRx;

    public class ArenaJoinEarlyRegisterButton : MonoBehaviour
    {
        [SerializeField]
        private SeasonArenaEmblem _seasonArenaEmblem;

        [SerializeField]
        private ChampionshipArenaEmblem _championshipArenaEmblem;

        [SerializeField]
        private GameObject _paymentObject;

        [SerializeField]
        private TextMeshProUGUI _costText;

        [SerializeField]
        private GameObject _completedObject;

        [SerializeField]
        private Button _button;

        private int _championshipId;
        private int _round;
        private long _cost;

        private readonly Subject<Unit> _onGoToGrinding = new Subject<Unit>();
        public IObservable<Unit> OnGoToGrinding => _onGoToGrinding;


        private readonly Subject<Unit> _onJoinArenaAction = new Subject<Unit>();
        public IObservable<Unit> OnJoinArenaAction => _onJoinArenaAction;

        private void Awake()
        {
            _button.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                var balance = States.Instance.CrystalBalance;
                var enoughMessageFormat =
                    L10nManager.Localize("UI_ARENA_EARLY_REGISTRATION_Q");
                var notEnoughMessage =
                    L10nManager.Localize("UI_NOT_ENOUGH_CRYSTAL");
                Widget.Find<PaymentPopup>().Show(
                    CostType.Crystal,
                    balance.MajorUnit,
                    _cost,
                    string.Format(enoughMessageFormat, _cost),
                    notEnoughMessage,
                    JoinArenaAction,
                    () => _onGoToGrinding.OnNext(Unit.Default));
            });
        }

        public void Show(
            ArenaType arenaType,
            int championshipId,
            int round,
            bool isRegistered,
            long cost = 0)
        {
            _championshipId = championshipId;
            _round = round;
            _cost = cost;

            switch (arenaType)
            {
                case ArenaType.OffSeason:
                    Hide();
                    return;
                case ArenaType.Season:
                    var seasonNumber = TableSheets.Instance.ArenaSheet
                        .GetSeasonNumber(
                            Game.Game.instance.Agent.BlockIndex,
                            _round);
                    _seasonArenaEmblem.Show(seasonNumber, !isRegistered);
                    _championshipArenaEmblem.Hide();
                    break;
                case ArenaType.Championship:
                    _seasonArenaEmblem.Hide();
                    _championshipArenaEmblem.Show(_championshipId, !isRegistered);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(arenaType));
            }

            if (isRegistered)
            {
                _paymentObject.SetActive(false);
                _completedObject.SetActive(true);
                _button.interactable = false;
                return;
            }

            _costText.text = _cost.ToString("N0", CultureInfo.CurrentCulture);
            _paymentObject.SetActive(true);
            _completedObject.SetActive(false);
            _button.interactable = true;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void JoinArenaAction()
        {
            var costFav =
                _cost * States.Instance.CrystalBalance.Currency;
            if (States.Instance.CrystalBalance < costFav)
            {
                NotificationSystem.Push(
                    MailType.System,
                    "Not enough crystal.",
                    NotificationCell.NotificationType.Information);
                return;
            }

            var itemSlotState = States.Instance.CurrentItemSlotStates[BattleType.Arena];
            var runeInfos = States.Instance.CurrentRuneSlotStates[BattleType.Arena]
                .GetEquippedRuneSlotInfos();
            ActionManager.Instance
                .JoinArena(
                    itemSlotState.Costumes,
                    itemSlotState.Equipments,
                    runeInfos,
                    _championshipId,
                    _round)
                .Subscribe();
            _onJoinArenaAction.OnNext(Unit.Default);
        }
    }
}
