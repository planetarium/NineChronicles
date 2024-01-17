using System;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    [Serializable]
    public class EventNoticeData
    {
        public int Priority { get; set; }
        public Sprite BannerImage { get; set; }
        public Sprite PopupImage { get; set; }
        public bool UseDateTime { get; set; }
        public string BeginDateTime { get; set; }
        public string EndDateTime { get; set; }
        public string Url { get; set; }
        public bool UseAgentAddress { get; set; }
        public string Description { get; set; }
        public string[] EnableKeys { get; set; }
    }
}
