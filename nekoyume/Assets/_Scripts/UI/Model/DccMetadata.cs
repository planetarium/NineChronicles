using System;
using System.Collections.Generic;

namespace Nekoyume.UI.Model
{
    [Serializable]
    public class DccMetadata
    {
        public string name { get; set; }
        public string description { get; set; }
        public string image { get; set; }
        public List<Attribute> attributes { get; set; }
        public List<int> traits { get; set; }
        public string traitBits { get; set; }
    }

    [Serializable]
    public class Attribute
    {
        public string trait_type { get; set; }
        public string value { get; set; }
    }
}
