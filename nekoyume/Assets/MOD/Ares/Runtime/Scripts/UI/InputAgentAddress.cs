using UnityEngine.UIElements;

namespace NineChronicles.MOD.Ares.UI
{
    public class InputAgentAddress : IUI
    {
        private readonly AresContext _aresContext;
        private readonly VisualElement _ui;

        public VisualElement UI => _ui;

        public InputAgentAddress(VisualElement root, AresContext aresContext)
        {
            _aresContext = aresContext;

            _ui = root;
            _ui.Q<Button>("input-agent-address__previous-button")
                .RegisterCallback<ClickEvent>(ev => Hide());
            // _ui.Q<Button>("input-agent-address__next-button")
            //     .RegisterCallback<ClickEvent>(ev => ShowSelectAvatar());
            _ui.Q<TextField>("input-agent-address__input").isReadOnly = true;
        }

        public void Show()
        {
            _ui.Q<TextField>("input-agent-address__input").value =
                _aresContext.AgentAddress?.ToString() ?? string.Empty;

            _ui.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _ui.style.display = DisplayStyle.None;
        }
    }
}
