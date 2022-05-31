using System;
using System.Globalization;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Game;
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

        private void Awake()
        {
            _button.onClick.AddListener(() =>
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

                var inventory = States.Instance.CurrentAvatarState.inventory;
                ActionManager.Instance.JoinArena(
                        inventory.Costumes
                            .Where(e => e.Equipped)
                            .Select(e => e.NonFungibleId)
                            .ToList(),
                        inventory.Equipments
                            .Where(e => e.Equipped)
                            .Select(e => e.NonFungibleId)
                            .ToList(),
                        _championshipId,
                        _round)
                    .DoOnSubscribe(() => Widget.Find<LoadingScreen>().Show())
                    .DoOnError(e =>
                    {
                        Widget.Find<LoadingScreen>().Close();
                        NotificationSystem.Push(
                            MailType.System,
                            "Failed to early register to next round.",
                            NotificationCell.NotificationType.Alert);
                    })
                    .DoOnCompleted(() =>
                    {
                        Hide();
                        Widget.Find<LoadingScreen>().Close();
                    })
                    .Subscribe();
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
                    var seasonNumber = TableSheets.Instance.ArenaSheet.TryGetSeasonNumber(
                        Game.Game.instance.Agent.BlockIndex,
                        _round,
                        out var outSeasonNumber)
                        ? outSeasonNumber
                        : throw new Exception($"Failed to get season number: {_championshipId}, {round}");
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
    }
}
