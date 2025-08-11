using System;

namespace Nekoyume.UI.Model
{
    [Serializable]
    public class EventBannerData
    {
        public int Priority { get; set; }
        public string BannerImageName { get; set; }
        public string PopupImageName { get; set; }
        public bool UseDateTime { get; set; }
        public string BeginDateTime { get; set; }
        public string EndDateTime { get; set; }
        public string? Url { get; set; }
        public bool UseAgentAddress { get; set; }
        public string Description { get; set; }
        public string[] EnableKeys { get; set; }
        public bool WithSign { get; set; }
        public bool IsMainnet { get; set; }
        public string ButtonType { get; set; }
        public string? SummonType { get; set; }
        public string? OpenWithProbabilityPopup { get; set; }
    }
}
