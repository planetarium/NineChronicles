using Nekoyume.Game;
using Nekoyume.State;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ArenaTicketPopup : PopupWidget
    {
        [SerializeField]
        private SweepSlider ticketSlider = null;

        [SerializeField]
        private TextMeshProUGUI haveTicketText = null;

        [SerializeField]
        private TextMeshProUGUI ticketCountToUseText = null;

        [SerializeField]
        private ConditionalButton startButton = null;

        [SerializeField]
        private Button closeButton = null;

        private System.Action<int> _arenaBattleAction;
        private readonly ReactiveProperty<int> _ticketCountToUse = new();

        protected override void Awake()
        {
            base.Awake();

            _ticketCountToUse
                .Subscribe(count =>
                {
                    ticketCountToUseText.text = count.ToString();
                    startButton.Interactable = count > 0;
                })
                .AddTo(gameObject);

            startButton.OnSubmitSubject.Subscribe(_ =>
            {
                _arenaBattleAction?.Invoke(_ticketCountToUse.Value);
                Close();
            }).AddTo(gameObject);

            closeButton.onClick.AddListener(() => Close());
        }

        public void Show(System.Action<int> arenaBattleAction)
        {
            _arenaBattleAction = arenaBattleAction;

            var blockIndex = Game.Game.instance.Agent.BlockIndex;
            var currentRound = TableSheets.Instance.ArenaSheet.GetRoundByBlockIndex(blockIndex);
            var ticketCount = RxProps.PlayersArenaParticipant.HasValue
                ? RxProps.PlayersArenaParticipant.Value.CurrentArenaInfo.GetTicketCount(
                    blockIndex,
                    currentRound.StartBlockIndex,
                    States.Instance.GameConfigState.DailyArenaInterval)
                : 0;
            haveTicketText.text = ticketCount.ToString();

            ticketSlider.Set(0, ticketCount, ticketCount, 8, 1, x => _ticketCountToUse.Value = x);

            base.Show();
        }
    }
}
