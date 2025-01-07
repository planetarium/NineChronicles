using System;
using System.Collections.Generic;
using System.Xml;
using Newtonsoft.Json;

namespace Nekoyume.UI.Model
{
    public class PolicyModel
    {
        public int MinimumLevel { get; set; }
        public int? MaxLevel { get; set; }

        public List<PatrolRewardModel> Rewards { get; set; }

        public long RequiredBlockInterval { get; set; }
    }

    public class PatrolRewardModel
    {
        // FungibleAssetValueRewardModel
        public string Currency { get; set; }

        // FungibleItemRewardModel
        public string FungibleId { get; set; }
        public int? ItemId { get; set; }

        public int PerInterval { get; set; }
    }
}
