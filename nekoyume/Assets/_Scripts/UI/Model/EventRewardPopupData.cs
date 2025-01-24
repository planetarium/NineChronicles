using System;
using Nekoyume.Helper;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    [Serializable]
    public class EventRewardPopupData
    {
        [Serializable]
        public class EventReward
        {
            public string BeginDateTime { get; set; }
            public string EndDateTime { get; set; }
            public string ToggleL10NKey { get; set; }
            public string DescriptionL10NKey { get; set; }

            // if `ContentPresetType` is not None and `Content` is null, it means the UI is controlled by the client.
            public ContentPresetType ContentPresetType { get; set; }
            public Content Content { get; set; }
        }

        [Serializable]
        public enum ContentPresetType
        {
            None,
            ClaimGift,
            PatrolReward,
            ThorChain,
        }

        [Serializable]
        public class Content
        {
            public Sprite Image { get; set; }
            public string ImageName { get; set; }
            public ShortcutHelper.PlaceType[] ShortcutTypes { get; set; }
        }

        public bool EnableEventRewardPopup { get; set; }
        public string TitleL10NKey { get; set; }
        public EventReward[] EventRewards { get; set; }

        public Content EnabledThorChainContent { get; set; }
        public Content DisabledThorChainContent { get; set; }

        public bool HasEvent => EnableEventRewardPopup && (EventRewards?.Length ?? 0) > 0;
    }
}
