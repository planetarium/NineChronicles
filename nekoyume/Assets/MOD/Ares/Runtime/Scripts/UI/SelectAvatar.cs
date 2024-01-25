using Libplanet.Crypto;
using Nekoyume;
using UnityEngine.UIElements;

namespace NineChronicles.MOD.Ares.UI
{
    public class SelectAvatar : IUI
    {
        private readonly AresContext _aresContext;
        private readonly VisualElement _ui;

        public VisualElement UI => _ui;

        public SelectAvatar(VisualElement root, AresContext aresContext)
        {
            _aresContext = aresContext;

            _ui = root;
            _ui.Q<Button>("select-avatar__previous-button")
                // .RegisterCallback<ClickEvent>(ev => ShowInputAgentAddress());
                .RegisterCallback<ClickEvent>(ev => Hide());
            // _ui.Q<Button>("select-avatar__next-button")
            //     .RegisterCallback<ClickEvent>(ev => ShowArenaScoreBoard(1));
            _ui.Q<VisualElement>("select-avatar__avatar-button-0")
                .Q<RadioButton>("select-avatar__avatar-button__container")
                .SetEnabled(false);
            _ui.Q<VisualElement>("select-avatar__avatar-button-1")
                .Q<RadioButton>("select-avatar__avatar-button__container")
                .SetEnabled(false);
            _ui.Q<VisualElement>("select-avatar__avatar-button-2")
                .Q<RadioButton>("select-avatar__avatar-button__container")
                .SetEnabled(false);
        }

        public void Show()
        {
            for (var i = 0; i < GameConfig.SlotCount; i++)
            {
                var avatarAddress = _aresContext.AvatarAddresses.Length > i
                    ? _aresContext.AvatarAddresses[i]
                    : (Address?)null;
                var avatarButtonContainer = _ui
                    .Q<VisualElement>($"select-avatar__avatar-button-{i}")
                    .Q<RadioButton>("select-avatar__avatar-button__container");
                avatarButtonContainer.label = avatarAddress.HasValue
                    ? $"AvatarName(Level)\n{avatarAddress}"
                    : "Empty";
                avatarButtonContainer.value = i == _aresContext.SelectedAvatarIndex;
            }

            _ui.style.display = DisplayStyle.Flex;
        }

        public void Hide()
        {
            _ui.style.display = DisplayStyle.None;
        }
    }
}
