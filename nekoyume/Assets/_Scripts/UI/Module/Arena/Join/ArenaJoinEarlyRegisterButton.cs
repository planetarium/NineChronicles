using System;
using System.Globalization;
using Nekoyume.Model.EnumType;
using Nekoyume.UI.Module.Arena.Emblems;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Arena.Join
{
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

        public event UnityAction onClickPaymentButton;

        private void Awake()
        {
            _button.onClick.AddListener(onClickPaymentButton);
        }

        public void Show(
            ArenaType arenaType,
            int seasonNumberOrChampionshipId,
            bool isRegistered,
            int cost = 0)
        {
            switch (arenaType)
            {
                case ArenaType.OffSeason:
                    Hide();
                    return;
                case ArenaType.Season:
                    _seasonArenaEmblem.Show(seasonNumberOrChampionshipId, !isRegistered);
                    _championshipArenaEmblem.Hide();
                    break;
                case ArenaType.Championship:
                    _seasonArenaEmblem.Hide();
                    _championshipArenaEmblem.Show(seasonNumberOrChampionshipId, !isRegistered);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(arenaType));
            }

            if (isRegistered)
            {
                _paymentObject.SetActive(false);
                _completedObject.SetActive(true);
                return;
            }

            _costText.text = cost.ToString("N0", CultureInfo.CurrentCulture);
            _paymentObject.SetActive(true);
            _completedObject.SetActive(false);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
