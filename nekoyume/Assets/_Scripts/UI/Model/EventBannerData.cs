using System;

namespace Nekoyume.UI.Model
{
    [Serializable]
    public class EventBannerData
    {
        public int Priority { get; set; }
        public string ImageName { get; set; }
        public bool UseDateTime { get; set; }
        public string BeginDateTime { get; set; }
        public string EndDateTime { get; set; }
        public string Url { get; set; }
        public bool UseAgentAddress { get; set; }
        public string Description { get; set; }
    }
}
