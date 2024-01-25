using Cysharp.Threading.Tasks;
using Libplanet.Crypto;
using Nekoyume;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace NineChronicles.MOD.Ares.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private VisualElement _inputAgentAddressUI;
        private VisualElement _selectAvatarUI;
        private VisualElement _arenaScoreboardUI;

        private AresContext _aresContext;

        public void Show(AresContext aresContext)
        {
            _aresContext = aresContext;
            if (_aresContext is null)
            {
                ShowInputAgentAddress();
            }
            else if (_aresContext.AgentAddress is null)
            {
                ShowInputAgentAddress();
            }
            else if (_aresContext.AvatarAddresses is null ||
                     _aresContext.AvatarAddresses.Length == 0 ||
                     _aresContext.SelectedAvatarIndex is null)
            {
                ShowSelectAvatar();
            }
            else
            {
                ShowArenaScoreBoard(1);
            }
        }

        private void Awake()
        {
            if (!_document)
            {
                Debug.LogError("SerializeField _document is null");
                return;
            }

            InitUI();
        }

        private void InitUI()
        {
            var root = _document.GetComponent<UIDocument>().rootVisualElement;
            _inputAgentAddressUI = root.Q<VisualElement>("input-agent-address__container");
            _inputAgentAddressUI
                .Q<Button>("input-agent-address__previous-button")
                .RegisterCallback<ClickEvent>(ev => HideAll());
            _inputAgentAddressUI
                .Q<Button>("input-agent-address__next-button")
                .RegisterCallback<ClickEvent>(ev => ShowSelectAvatar());
            _inputAgentAddressUI.Q<TextField>("input-agent-address__input").isReadOnly = true;

            _selectAvatarUI = root.Q<VisualElement>("select-avatar__container");
            _selectAvatarUI
                .Q<Button>("select-avatar__previous-button")
                // .RegisterCallback<ClickEvent>(ev => ShowInputAgentAddress());
                .RegisterCallback<ClickEvent>(ev => HideAll());
            _selectAvatarUI
                .Q<Button>("select-avatar__next-button")
                .RegisterCallback<ClickEvent>(ev => ShowArenaScoreBoard(1));
            _selectAvatarUI
                .Q<VisualElement>("select-avatar__avatar-button-0")
                .Q<RadioButton>("select-avatar__avatar-button__container")
                .SetEnabled(false);
            _selectAvatarUI
                .Q<VisualElement>("select-avatar__avatar-button-1")
                .Q<RadioButton>("select-avatar__avatar-button__container")
                .SetEnabled(false);
            _selectAvatarUI
                .Q<VisualElement>("select-avatar__avatar-button-2")
                .Q<RadioButton>("select-avatar__avatar-button__container")
                .SetEnabled(false);

            _arenaScoreboardUI = root.Q<VisualElement>("arena-score-board__container");
            _arenaScoreboardUI
                .Q<Button>("arena-score-board__previous-button")
                // .RegisterCallback<ClickEvent>(ev => ShowSelectAvatar());
                .RegisterCallback<ClickEvent>(ev => HideAll());
            _arenaScoreboardUI.Q<Button>("arena-score-board__previous-page-button")
            .RegisterCallback<ClickEvent>(ev =>
                ShowArenaScoreBoard(_aresContext.ArenaScoreBoardPage - 1));
            _arenaScoreboardUI.Q<Button>("arena-score-board__next-page-button")
                .RegisterCallback<ClickEvent>(ev =>
                    ShowArenaScoreBoard(_aresContext.ArenaScoreBoardPage + 1));
            _arenaScoreboardUI
                .Q<Button>("arena-score-board__load-button")
                .SetEnabled(false);
            const int cellCountPerPage = 14;
            for (var i = 0; i < cellCountPerPage; i++)
            {
                var avatarCell = _arenaScoreboardUI
                    .Q<VisualElement>($"arena-score-board__avatar-cell-{i:00}")
                    .Q<VisualElement>("arena-score-board__avatar-cell__container");
                var button = avatarCell.Q<Button>("arena-score-board__avatar-cell__button");
                button.RegisterCallback<ClickEvent>(ev =>
                {
                    _aresContext.Track("9c_unity_mod_ares__click__arena_score_board__avatar_cell__button");
                    button.SetEnabled(false);
                    var currentPage = _aresContext.ArenaScoreBoardPage;
                    var participantIndex = currentPage * cellCountPerPage + i;
                    var participant = _aresContext.GetArenaParticipants(participantIndex, 1)[0];
                    avatarCell.Q<Label>("arena-score-board__avatar-cell__label-2").text = "Wait...";
                    UniTask.RunOnThreadPool(UniTask.Action(async () =>
                    {
                        var wr = await _aresContext.GetWinRateAsync(
                            participant.AvatarAddr);
                        if (currentPage != _aresContext.ArenaScoreBoardPage)
                        {
                            return;
                        }

                        await UniTask.SwitchToMainThread();
                        avatarCell.Q<Label>("arena-score-board__avatar-cell__label-2").text =
                            $"WinScore: {participant.WinScore} | WinRate: {wr:P2}";
                        button.SetEnabled(true);
                    })).Forget();
                });
            }

            HideAll();
        }

        private void HideAll()
        {
            _inputAgentAddressUI.style.display = DisplayStyle.None;
            _selectAvatarUI.style.display = DisplayStyle.None;
            _arenaScoreboardUI.style.display = DisplayStyle.None;

            if (_aresContext is not null)
            {
                _aresContext.CurrentUI = null;
            }
        }

        private void ShowInputAgentAddress()
        {
            _inputAgentAddressUI.Q<TextField>("input-agent-address__input").value =
                _aresContext.AgentAddress?.ToString() ?? string.Empty;
            Show(_inputAgentAddressUI);
        }

        private void ShowSelectAvatar()
        {
            if (!_aresContext.AgentAddress.HasValue)
            {
                Debug.LogError("agentAddress is null");
                return;
            }

            for (var i = 0; i < GameConfig.SlotCount; i++)
            {
                var avatarAddress = _aresContext.AvatarAddresses.Length > i
                    ? _aresContext.AvatarAddresses[i]
                    : (Address?)null;
                var avatarButtonContainer = _selectAvatarUI
                    .Q<VisualElement>($"select-avatar__avatar-button-{i}")
                    .Q<RadioButton>("select-avatar__avatar-button__container");
                avatarButtonContainer.label = avatarAddress.HasValue
                    ? $"AvatarName(Level)\n{avatarAddress}"
                    : "Empty";
                avatarButtonContainer.value = i == _aresContext.SelectedAvatarIndex;
            }

            Show(_selectAvatarUI);
        }

        private void ShowArenaScoreBoard(int page)
        {
            _aresContext.ArenaScoreBoardPage = page;

            _arenaScoreboardUI.Q<Label>("arena-score-board__agent-address").text =
                _aresContext.AgentAddress?.ToString()[..10] ?? "Empty";
            _arenaScoreboardUI.Q<Label>("arena-score-board__avatar-nickname").text =
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
                    _arenaScoreboardUI
                        .Q<VisualElement>($"arena-score-board__avatar-cell-{i:00}")
                        .style.display = DisplayStyle.None;
                    continue;
                }

                var participant = participants[i];
                var winRate = _aresContext.WinRates.ContainsKey(participant.AvatarAddr)
                    ? _aresContext.WinRates[participant.AvatarAddr].ToString("P2")
                    : "-";
                var avatarCell = _arenaScoreboardUI
                    .Q<VisualElement>($"arena-score-board__avatar-cell-{i:00}")
                    .Q<VisualElement>("arena-score-board__avatar-cell__container");
                avatarCell.Q<Label>("arena-score-board__avatar-cell__label-0").text =
                    $"{participant.NameWithHash} | Lv: {participant.Level} | CP: {participant.Cp}";
                avatarCell.Q<Label>("arena-score-board__avatar-cell__label-1").text =
                    $"Rank: {participant.Rank} | Score: {participant.Score}";
                avatarCell.Q<Label>("arena-score-board__avatar-cell__label-2").text =
                    $"WinScore: {participant.WinScore} | WinRate: {winRate}";
                // avatarCell.Q<Button>("arena-score-board__avatar-cell__button")
                //     .RegisterCallback<ClickEvent>(ev =>
                //     {
                //         avatarCell.Q<Label>("arena-score-board__avatar-cell__label-2").text = "Wait...";
                //         UniTask.RunOnThreadPool(UniTask.Action(async () =>
                //         {
                //             var wr = await _aresContext.GetWinRateAsync(
                //                 participant.AvatarAddr);
                //             await UniTask.SwitchToMainThread();
                //             avatarCell.Q<Label>("arena-score-board__avatar-cell__label-2").text =
                //                 $"WinScore: {participant.WinScore} | WinRate: {wr:P2}";
                //         })).Forget();
                //     });
            }

            var prevPageButton = _arenaScoreboardUI.Q<Button>("arena-score-board__previous-page-button");
            prevPageButton.SetEnabled(_aresContext.ArenaScoreBoardPage > 1);
            var nextPageButton = _arenaScoreboardUI.Q<Button>("arena-score-board__next-page-button");
            nextPageButton.SetEnabled(participants.Length == cellCountPerPage);

            Show(_arenaScoreboardUI);
        }

        private void Show(VisualElement ui)
        {
            if (_aresContext.CurrentUI == ui)
            {
                return;
            }

            if (_aresContext.CurrentUI is not null)
            {
                _aresContext.CurrentUI.style.display = DisplayStyle.None;
            }

            _aresContext.CurrentUI = ui;
            _aresContext.CurrentUI.style.display = DisplayStyle.Flex;
        }

        private void Update()
        {
            if (_aresContext.CurrentUI == _arenaScoreboardUI)
            {
                _arenaScoreboardUI.Q<Label>("arena-score-board__current-block-index").text =
                    $"Current: {_aresContext.BlockIndex}";
            }
        }
    }
}
