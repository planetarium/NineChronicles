using Cysharp.Threading.Tasks;
using Nekoyume.UI.Model;
using UnityEngine.UIElements;

namespace NineChronicles.MOD.Ares.UI.VisualTreeAssets
{
    public class ArenaScoreBoard_AvatarCell
    {
        private readonly AresContext _aresContext;
        private readonly int _index;
        private readonly VisualElement _ui;
        private readonly Button _button;
        private ArenaParticipantModel _participant;

        public ArenaScoreBoard_AvatarCell(
            AresContext aresContext,
            VisualElement root,
            int index)
        {
            _aresContext = aresContext;
            _index = index;
            _ui = root;
            _button = _ui.Q<Button>("arena-score-board__avatar-cell__button");
            _button.RegisterCallback<ClickEvent>(OnClick);
        }

        public void Show(ArenaParticipantModel participant)
        {
            _participant = participant;

            _ui.style.display = DisplayStyle.Flex;
            _ui.Q<VisualElement>("arena-score-board__avatar-cell__portrait").style.backgroundImage =
                new StyleBackground(_aresContext.GetItemIcon(_participant.PortraitId));
            _ui.Q<Label>("arena-score-board__avatar-cell__label-0").text =
                $"{_participant.NameWithHash} | Lv: {_participant.Level} | CP: {_participant.Cp}";
            _ui.Q<Label>("arena-score-board__avatar-cell__label-1").text =
                $"Rank: {_participant.Rank} | Score: {_participant.Score}";
            _ui.Q<Button>("arena-score-board__avatar-cell__button")
                .SetEnabled(true);
            UpdateWinRate();
            _ui.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _ui.style.display = DisplayStyle.None;
        }

        private void OnClick(ClickEvent ev)
        {
            _aresContext.Track("9c_unity_mod_ares__click__arena_score_board__avatar_cell__button");
            if (_aresContext.WinRates.ContainsKey(_participant.AvatarAddr))
            {

                return;
            }

            var address = _participant.AvatarAddr;
            _button.SetEnabled(false);
            _ui.Q<Label>("arena-score-board__avatar-cell__label-2").text = "Wait...";
            UniTask.RunOnThreadPool(UniTask.Action(async () =>
            {
                var winRateTuple = await _aresContext.GetWinRateAsync(address);
                await UniTask.SwitchToMainThread();
                if (address != _participant.AvatarAddr)
                {
                    return;
                }

                SetWinRate(winRateTuple);
                _button.SetEnabled(true);
            })).Forget();
        }

        private void UpdateWinRate()
        {
            if (_aresContext.WinRates.TryGetValue(_participant.AvatarAddr, out var winRateTuple))
            {
                SetWinRate(winRateTuple);
            }
            else
            {
                SetWinRate(null);
            }
        }

        private void SetWinRate((bool inProgress, float winRate)? winRateTuple)
        {
            if (winRateTuple.HasValue)
            {
                if (winRateTuple.Value.inProgress)
                {
                    _ui.Q<Label>("arena-score-board__avatar-cell__label-2").text = "Wait...";
                }
                else
                {
                    _ui.Q<Label>("arena-score-board__avatar-cell__label-2").text =
                        $"WinScore: {_participant.WinScore} | WinRate: {winRateTuple.Value.winRate:P2}";
                }
            }
            else
            {
                _ui.Q<Label>("arena-score-board__avatar-cell__label-2").text =
                    $"WinScore: {_participant.WinScore} | WinRate: ??";
            }
        }
    }
}
