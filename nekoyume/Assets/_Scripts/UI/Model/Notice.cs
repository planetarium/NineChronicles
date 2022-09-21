using System;

namespace Nekoyume.UI.Model
{
    [Serializable]
    public class Notice
    {
        public string ImageName { get; set; }
        public string BeginDateTime { get; set; }
        public string EndDateTime { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
    }
}
