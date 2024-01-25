using NineChronicles.MOD.Ares.UI.VisualTreeAssets;
using UnityEngine.UIElements;

namespace NineChronicles.MOD.Ares.UI
{
    public class ArenaScoreBoard : IUI
    {
        private readonly VisualElement _ui;
        private readonly ArenaScoreBoard_AvatarCell[] _avatarCells;
        private readonly AresContext _aresContext;

        public VisualElement UI => _ui;

        public ArenaScoreBoard(VisualElement root, AresContext aresContext)
        {
            _aresContext = aresContext;

            _ui = root;
            _ui.Q<Button>("arena-score-board__previous-button")
                .RegisterCallback<ClickEvent>(ev => Hide());
            _ui.Q<Button>("arena-score-board__previous-page-button")
                .RegisterCallback<ClickEvent>(ev =>
                    {
                        _aresContext.ArenaScoreBoardPage--;
                        Show();
                    });
            _ui.Q<Button>("arena-score-board__next-page-button")
                .RegisterCallback<ClickEvent>(ev =>
                    {
                        _aresContext.ArenaScoreBoardPage++;
                        Show();
                    });
            _ui.Q<Button>("arena-score-board__load-button")
                .SetEnabled(false);
            const int cellCountPerPage = 14;
            _avatarCells = new ArenaScoreBoard_AvatarCell[cellCountPerPage];
            for (var i = 0; i < cellCountPerPage; i++)
            {
                var avatarCell = _ui.Q<VisualElement>($"arena-score-board__avatar-cell-{i:00}")
                                .Q<VisualElement>("arena-score-board__avatar-cell__container");
                _avatarCells[i] = new ArenaScoreBoard_AvatarCell(_aresContext, avatarCell, i);
            }
        }

        public void Show()
        {
            _ui.Q<Label>("arena-score-board__agent-address").text =
                _aresContext.AgentAddress?.ToString()[..10] ?? "Empty";
            _ui.Q<Label>("arena-score-board__avatar-nickname").text =
                _aresContext.SelectedAvatarAddress?.ToString()[..10] ?? "Empty";

            const int cellCountPerPage = 14;
            var startIndex = (_aresContext.ArenaScoreBoardPage - 1) * cellCountPerPage;
            var participants = _aresContext.GetArenaParticipants(
                startIndex,
                cellCountPerPage);
            for (var i = 0; i < cellCountPerPage; i++)
            {
                if (participants.Length <= i)
                {
                    _avatarCells[i].Hide();
                    continue;
                }

                var participant = participants[i];
                _avatarCells[i].Show(participant);
            }

            var prevPageButton = _ui.Q<Button>("arena-score-board__previous-page-button");
            prevPageButton.SetEnabled(_aresContext.ArenaScoreBoardPage > 1);
            var nextPageButton = _ui.Q<Button>("arena-score-board__next-page-button");
            nextPageButton.SetEnabled(participants.Length == cellCountPerPage);

            _ui.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _ui.style.display = DisplayStyle.None;
        }
    }
}
