using NineChronicles.MOD.Ares.UI;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace NineChronicles.MOD.Ares
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private UIDocument _document;

        private InputAgentAddress _inputAgentAddress;
        private SelectAvatar _selectAvatar;
        private ArenaScoreBoard _arenaScoreboard;

        private AresContext _aresContext;

        public void Initialize(AresContext aresContext)
        {
            _aresContext = aresContext;
            InitUI();
        }

        public void Show()
        {
            ShowArenaScoreBoard(1);
        }

        private void InitUI()
        {
            if (_inputAgentAddress is not null)
            {
                return;
            }

            var root = _document.GetComponent<UIDocument>().rootVisualElement;
            _inputAgentAddress = new InputAgentAddress(
                root.Q<VisualElement>("input-agent-address__container"),
                _aresContext);
            _selectAvatar = new SelectAvatar(
                root.Q<VisualElement>("select-avatar__container"),
                _aresContext);
            _arenaScoreboard = new ArenaScoreBoard(
                root.Q<VisualElement>("arena-score-board__container"),
                _aresContext);

            HideAll();
        }

        private void HideAll()
        {
            _inputAgentAddress.Hide();
            _selectAvatar.Hide();
            _arenaScoreboard.Hide();

            if (_aresContext is not null)
            {
                _aresContext.CurrentUI = null;
            }
        }

        private void ShowInputAgentAddress()
        {
            Show(_inputAgentAddress);
        }

        private void ShowSelectAvatar()
        {
            if (!_aresContext.AgentAddress.HasValue)
            {
                Debug.LogError("agentAddress is null");
                return;
            }

            Show(_selectAvatar);
        }

        private void ShowArenaScoreBoard(int page)
        {
            _aresContext.ArenaScoreBoardPage = page;
            Show(_arenaScoreboard);
        }

        private void Show(IUI ui)
        {
            _aresContext.CurrentUI?.Hide();
            _aresContext.CurrentUI = ui;
            _aresContext.CurrentUI.Show();
        }

        private void Update()
        {
            if (_aresContext.CurrentUI == _arenaScoreboard)
            {
                _arenaScoreboard.UI.Q<Label>("arena-score-board__current-block-index").text =
                    $"Current: {_aresContext.BlockIndex}";
            }
        }
    }
}
